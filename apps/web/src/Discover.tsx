import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { request, type Claim, type Episode, type Session, type SessionSummary } from './experience-api'

export function Discover() {
  const { sessionId } = useParams()
  const navigate = useNavigate()
  const [sessions, setSessions] = useState<SessionSummary[]>([])
  const [session, setSession] = useState<Session | null>(null)
  const [title, setTitle] = useState('')
  const [answer, setAnswer] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  // Preserve operation IDs on retry; changing text starts a new operation.
  const createOp = useRef({ id: crypto.randomUUID(), text: '' })
  const answerOp = useRef({ id: crypto.randomUUID(), text: '' })
  const load = useCallback(async () => {
    try {
      const list = await request<SessionSummary[]>('/sessions'); setSessions(list); setError('')
      setSession(sessionId ? await request<Session>(`/sessions/${sessionId}`) : null)
    } catch (e) { setError((e as Error).message) }
  }, [sessionId])
  useEffect(() => {
    let active = true
    void Promise.all([request<SessionSummary[]>('/sessions'), sessionId ? request<Session>(`/sessions/${sessionId}`) : Promise.resolve(null)])
      .then(([list, record]) => { if (active) { setSessions(list); setSession(record); setError('') } })
      .catch((e: Error) => { if (active) setError(e.message) })
    return () => { active = false }
  }, [sessionId])
  async function act(work: () => Promise<void>) {
    if (busy) return
    setBusy(true); setError('')
    try { await work() } catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  return <><p className="eyebrow">DISCOVER · MANUAL CAPTURE · NO AI</p><h1>Start with a real experience.</h1><p className="intro">Save your own account, identify factual statements, and review them against the original words. Nothing is sent to an AI provider.</p>
    {error && <p className="notice" role="alert">{error} <button disabled={busy} onClick={() => void load()}>Reload records</button></p>}
    {!sessionId ? <div className="grid"><section className="card"><h2>A new experience</h2><form onSubmit={(e) => { e.preventDefault(); void act(async () => {
      if (createOp.current.text !== title) createOp.current = { id: crypto.randomUUID(), text: title }
      const result = await request<{ id: string }>('/sessions', { id: createOp.current.id, title })
      setTitle(''); navigate(`/app/discover/${result.id}`)
    }) }}><label htmlFor="title">Give this experience a short title</label><input id="title" maxLength={160} required value={title} onChange={(e) => setTitle(e.target.value)} placeholder="For example, solving a build failure" /><button className="button" disabled={busy || !title.trim()}>Create session</button></form></section><section className="card"><h2>Continue a session</h2>{sessions.length ? <ul>{sessions.map((s) => <li key={s.id}><Link to={`/app/discover/${s.id}`}>{s.title}</Link> <span className="badge">{s.saved ? 'In Experience Bank' : 'In progress'}</span></li>)}</ul> : <p>No sessions yet.</p>}</section></div> : session ? <>
      <p><Link to="/app/discover">All sessions</Link> · <Link to="/app/experiences">Experience Bank</Link></p><h2>{session.title}</h2><div className="grid"><section className="card"><h2>Your original account</h2><p className="small">Describe the situation, what you personally did, and what happened. This is a manual capture step, not an AI interview.</p>{session.turns.map((turn) => <article className="source" key={turn.id}><span className="badge">Original answer · {new Date(turn.createdAt).toLocaleString()}</span><p className="verbatim">{turn.text}</p></article>)}
      <form onSubmit={(e) => { e.preventDefault(); void act(async () => {
        if (answerOp.current.text !== answer) answerOp.current = { id: crypto.randomUUID(), text: answer }
        setSession(await request<Session>(`/sessions/${session.id}/turns`, { id: answerOp.current.id, text: answer, expectedRevision: session.revision }))
        setAnswer(''); answerOp.current = { id: crypto.randomUUID(), text: '' }
      }) }}><label htmlFor="answer">Add an answer or more context</label><textarea id="answer" required maxLength={20000} value={answer} onChange={(e) => setAnswer(e.target.value)} /><button className="button" disabled={busy || !answer.trim()}>Save original answer</button></form><p className="small">Saved answers remain unchanged. Add a new answer to clarify an earlier account. Unsaved text stays only on this page; save before leaving.</p></section>
      <section className="card"><h2>Facts for review</h2><p className="small">Your confirmation records your account. It does not externally verify it or establish a skill.</p>{session.claims.map((claim) => <ClaimCard key={`${claim.id}:${claim.revisions.length}`} claim={claim} source={session.turns.find((t) => t.id === claim.sourceTurnId)?.text ?? 'Source unavailable'} busy={busy} onReview={(statement, reviewStatus) => act(async () => setSession(await request<Session>(`/sessions/${session.id}/claims/${claim.id}`, { statement, reviewStatus, expectedRevision: session.revision }, 'PUT')))} />)}
      {session.turns.length > 0 && <NewClaim key={session.id} session={session} busy={busy} onAdd={(id, sourceTurnId, statement) => act(async () => setSession(await request<Session>(`/sessions/${session.id}/claims`, { id, sourceTurnId, statement, expectedRevision: session.revision })))} />}
      {!session.episodeId ? <button className="button" disabled={busy || !session.claims.some((c) => c.reviewStatus === 'Accepted')} onClick={() => void act(async () => setSession(await request<Session>(`/sessions/${session.id}/episode`, { expectedRevision: session.revision })))}>Save to Experience Bank</button> : <p className="notice">Saved to Experience Bank. Later fact reviews update this record; the original answers and review history remain available.</p>}</section></div></> : <p role="status">{error ? 'Session could not be loaded.' : 'Loading session…'}</p>}</>
}
function NewClaim({ session, busy, onAdd }: { session: Session; busy: boolean; onAdd: (id: string, source: string, text: string) => Promise<void> }) {
  const [source, setSource] = useState(session.turns[0].id)
  const [text, setText] = useState('')
  const op = useRef({ id: crypto.randomUUID(), payload: '' })
  return <form className="source" onSubmit={(e) => { e.preventDefault(); const payload = JSON.stringify([source, text]); if (op.current.payload !== payload) op.current = { id: crypto.randomUUID(), payload }; void onAdd(op.current.id, source, text) }}><h3>Record one factual statement</h3><label htmlFor="source">Supporting original answer</label><select id="source" value={source} onChange={(e) => setSource(e.target.value)}>{session.turns.map((turn, i) => <option value={turn.id} key={turn.id}>Answer {i + 1}: {turn.text.slice(0, 60)}</option>)}</select><details><summary>Read selected source</summary><p className="verbatim">{session.turns.find((t) => t.id === source)?.text}</p></details><label htmlFor="fact">What does this answer say you personally did?</label><textarea id="fact" maxLength={4000} required value={text} onChange={(e) => setText(e.target.value)} /><button disabled={busy || !text.trim()}>Add pending fact</button><p className="small">User-authored statement. Review separately below; no automatic verification is performed.</p></form>
}
function ClaimCard({ claim, source, busy, onReview }: { claim: Claim; source: string; busy: boolean; onReview: (text: string, status: string) => Promise<void> }) {
  const [text, setText] = useState(claim.statement)
  return <article className="source"><span className="badge">{claim.reviewStatus} · User-authored</span><label htmlFor={`fact-${claim.id}`}>Factual statement</label><textarea id={`fact-${claim.id}`} maxLength={4000} value={text} onChange={(e) => setText(e.target.value)} /><details><summary>Original source</summary><p className="verbatim">{source}</p></details><div className="actions">{['Accepted', 'Disputed', 'Pending'].map((status) => <button key={status} disabled={busy || !text.trim()} onClick={() => void onReview(text, status)}>{status === 'Accepted' ? 'Accept fact' : status === 'Disputed' ? 'Dispute' : 'Keep pending'}</button>)}</div><details><summary>Review history ({claim.revisions.length})</summary><ul>{claim.revisions.map((r) => <li key={r.sessionRevision}><span className="badge">{r.reviewStatus} · revision {r.sessionRevision}</span><p className="verbatim">{r.statement}</p></li>)}</ul></details></article>
}
export function ExperienceBank() {
  const [episodes, setEpisodes] = useState<Episode[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  useEffect(() => { request<Episode[]>('/experiences').then(setEpisodes).catch((e: Error) => setError(e.message)).finally(() => setLoading(false)) }, [])
  return <><p className="eyebrow">EXPERIENCE BANK</p><h1>The work behind your story.</h1><p className="intro">Saved experiences with reviewed facts and their original sources. Skill interpretations are not generated yet.</p>{error && <p role="alert" className="notice">{error}</p>}{loading && <p role="status">Loading experiences…</p>}{!loading && !error && !episodes.length && <section className="card"><h2>Your bank starts with one experience.</h2><Link className="button" to="/app/discover">Start a session</Link></section>}{episodes.map((ep) => <section className="card" key={ep.id}><span className="badge">{ep.acceptedFacts} accepted facts</span><h2>{ep.title}</h2><p className="small">Saved {new Date(ep.savedAt).toLocaleString()}</p><Link to={`/app/discover/${ep.sessionId}`}>Open facts and original sources</Link></section>)}</>
}
