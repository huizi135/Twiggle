# First Saved Experience

This release connects manual capture and factual review to PostgreSQL. It does not provide adaptive AI questions, skill discovery, or a capability graph yet. No external AI service receives your text.

## Update and restart

1. Stop Vite and the API with Ctrl+C in their own terminals.
2. From the repository root, run `git pull --ff-only`.
3. Keep Docker Desktop running, then run `docker compose up -d --wait`.
4. Restart the API using `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-api.ps1`.
5. In `apps/web`, run `npm.cmd ci`, then `npm.cmd run dev`.
6. Open `http://127.0.0.1:5173/app/discover`.

The API script now applies the initial EF Core migration on startup. It creates Sessions, Turns, Claims, ClaimRevision, and Episodes tables without erasing existing database data. Do not delete the Docker volume to update the application. No separate EF CLI installation is required for normal startup.

## Try the complete flow

Use this synthetic example for an initial check:

- Create a session named **Investigating a failed customer build**.
- Save an original answer: **I added a configuration validation step to an existing build workflow. A teammate had designed the original workflow.**
- Under **Record one factual statement**, select that answer and write: **I implemented a configuration validation step in an existing workflow.**
- Select **Add pending fact**. The statement is saved but remains Pending.
- Open **Original source** to compare the wording with the original answer.
- Select **Accept fact**, then **Save to Experience Bank**.
- Open **Experience Bank** and reopen the experience.
- Refresh the browser. The saved answer, claim status, and episode should still exist.
- Edit the fact to make ownership more precise, accept it again, and inspect **Review history**. The original answer must remain unchanged.

You can add clarifying answers later, and leave facts Pending or Disputed. Saving an episode requires at least one accepted fact. Confirmation is your endorsement of wording, not external verification or proof of a skill. A saved episode is a live record; its current accepted facts can change, while claim revisions remain visible.

## Recovery and current limits

- Failed requests keep the draft on the current page. A reload, navigation, or closed tab can discard unsaved text; save before leaving.
- On a version conflict, copy any unsaved text, choose **Reload records**, then review the latest version before retrying. No silent overwrite occurs.
- Repeating an identical answer submission with the same operation ID does not duplicate the turn. Reusing an ID for different content is rejected.
- Original answers are append-only in this slice. To clarify them, add another answer. Full privacy export/deletion controls are still pending; use synthetic or non-sensitive records for initial testing.
- One local development workspace only. No hosted identity, public access, file uploads, generated skills, automatic fact extraction, or AI interviews.
- The fact-entry form retains its text after submission to support safe retries. Change the text or source when adding a different fact.

## Engineering validation

The HTTP integration test in `tests/experience-flow.py` runs against a disposable PostgreSQL database in GitHub Actions. It covers migrations, answer immutability, valid source membership, explicit fact review, retry behavior, stale writes, revision history, Experience Bank visibility, and persistence after API restart. Never point this test at your personal database: it creates synthetic records and deliberately restarts its own API process.
