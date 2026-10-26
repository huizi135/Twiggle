"""HTTP integration checks against a disposable PostgreSQL database only."""
import json
import os
from pathlib import Path
import subprocess
import time
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[1]
connection = os.environ['TWIGGLE_TEST_CONNECTION']
dotnet = os.environ.get('TWIGGLE_TEST_DOTNET', 'dotnet')
base = 'http://127.0.0.1:5080/api'

def call(path, body=None, method=None, expected=200):
    req = urllib.request.Request(base + path, data=json.dumps(body).encode() if body is not None else None,
                                 headers={'Content-Type': 'application/json'}, method=method)
    try:
        response = urllib.request.urlopen(req, timeout=10)
    except urllib.error.HTTPError as error:
        response = error
    data = json.load(response)
    assert response.status == expected, (path, response.status, data)
    return data

def start():
    log = open(ROOT / 'api-test.log', 'a')
    process = subprocess.Popen([dotnet, str(ROOT / 'src/Twiggle.Api/bin/Release/net10.0/Twiggle.Api.dll')],
        cwd=ROOT, stdout=log, stderr=log, env={**os.environ, 'ASPNETCORE_ENVIRONMENT': 'Development',
        'ConnectionStrings__Twiggle': connection, 'Database__ApplyMigrations': 'true'})
    for _ in range(150):
        if process.poll() is not None:
            raise RuntimeError('API exited. Inspect api-test.log.')
        try:
            call('/health/ready')
            return process, log
        except (urllib.error.URLError, AssertionError):
            time.sleep(.2)
    process.terminate()
    raise RuntimeError('API did not become ready')

def stop(process, log):
    process.terminate()
    process.wait(timeout=10)
    log.close()

process, log = start()
try:
    session_id, turn_id, fact_id = [str(uuid.uuid4()) for _ in range(3)]
    title = 'Synthetic integration test — workflow validation'
    call('/sessions', {'id': session_id, 'title': title})
    call('/sessions', {'id': session_id, 'title': title})
    call('/sessions', {'id': session_id, 'title': 'Different title'}, expected=409)
    path = '/sessions/' + session_id
    call(path + '/episode', {'expectedRevision': 0}, expected=400)
    original = 'I added a validation step to an existing build workflow.'
    payload = {'id': turn_id, 'text': original, 'expectedRevision': 0}
    session = call(path + '/turns', payload)
    assert call('/ai/status')['configured'] is False
    call(path + '/follow-up', {'turnId': turn_id, 'expectedRevision': 1, 'allowExternalProcessing': False}, expected=400)
    call(path + '/follow-up', {'turnId': turn_id, 'expectedRevision': 1, 'allowExternalProcessing': True}, expected=503)
    assert call(path)['turns'][0]['text'] == original
    assert session['revision'] == 1 and len(session['turns']) == 1
    assert len(call(path + '/turns', payload)['turns']) == 1
    call(path + '/turns', {**payload, 'text': 'Changed original'}, expected=409)
    call(path + '/turns', {**payload, 'id': str(uuid.uuid4())}, expected=409)
    call(path + '/claims', {'id': fact_id, 'sourceTurnId': str(uuid.uuid4()), 'statement': original, 'expectedRevision': 1}, expected=400)
    claim_payload = {'id': fact_id, 'sourceTurnId': turn_id, 'statement': original, 'expectedRevision': 1}
    session = call(path + '/claims', claim_payload)
    assert session['claims'][0]['reviewStatus'] == 'Pending'
    assert len(call(path + '/claims', claim_payload)['claims']) == 1
    call(path + '/episode', {'expectedRevision': 2}, expected=400)
    review = {'statement': original, 'reviewStatus': 'Accepted', 'expectedRevision': 2}
    session = call(path + '/claims/' + fact_id, review, 'PUT')
    assert session['revision'] == 3
    assert call(path + '/claims/' + fact_id, review, 'PUT')['revision'] == 3
    session = call(path + '/episode', {'expectedRevision': 3})
    assert session['episodeId']
    assert call(path + '/episode', {'expectedRevision': 3})['episodeId'] == session['episodeId']
    corrected = 'I implemented one check; the existing workflow was designed by a teammate.'
    call(path + '/claims/' + fact_id, {'statement': corrected, 'reviewStatus': 'Accepted', 'expectedRevision': 2}, 'PUT', expected=409)
    session = call(path + '/claims/' + fact_id, {'statement': corrected, 'reviewStatus': 'Accepted', 'expectedRevision': 4}, 'PUT')
    assert session['turns'][0]['text'] == original
    assert len(session['claims'][0]['revisions']) == 3
    assert session['claims'][0]['statement'] == corrected
    assert any(ep['sessionId'] == session_id and ep['acceptedFacts'] == 1 for ep in call('/experiences'))
    other = str(uuid.uuid4())
    call('/sessions', {'id': other, 'title': 'Synthetic second session'})
    call('/sessions/' + other + '/claims', {**claim_payload, 'id': str(uuid.uuid4()), 'expectedRevision': 0}, expected=400)
finally:
    stop(process, log)
process, log = start()
try:
    restored = call(path)
    assert restored['turns'][0]['text'] == original
    assert restored['claims'][0]['statement'] == corrected
    assert restored['episodeId'] == session['episodeId']
finally:
    stop(process, log)
print('PASS: migrations, immutable answers, source validation, explicit review, stale-write rejection, retries, history, bank, and restart persistence')
