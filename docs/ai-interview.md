# AI Deep Interview — First Connected Slice

## What changes

Discover now saves your answer first, then uses an explicitly enabled OpenAI connection to return a short summary, one focused follow-up question, and up to five candidate factual statements. Facts are automatically placed in the existing review panel with `AIExtracted` origin and Pending status. You no longer have to extract them yourself.

Original user answers stay unchanged. AI summaries/questions are stored separately and persist across refresh. User acceptance is still required for facts. Candidate skills, an adaptive coverage planner, a durable generation queue, and a full capability graph remain future work.

## One-time server configuration (Windows)

Update the repository first. In PowerShell from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\configure-ai.ps1
```

Enter your **OpenAI API key** at the hidden prompt. Do not send it in chat. Press Enter to use the default `gpt-4.1` model, or provide a Responses-compatible model with Structured Outputs available to your API project. The code does not assume every account has access to every model.

The script uses .NET development User Secrets, outside the repository and browser. User Secrets are local developer configuration, not an encrypted production vault. No key is written to frontend files or `.env`. Do not run `dotnet user-secrets list` in output you plan to share.

Alternatively, the API accepts a server environment variable `OPENAI_API_KEY` or `AI__ApiKey` and an optional `AI__Model`. Hosted use remains disabled until real authentication exists. No server key is provided by the repository; local configuration and a live model call are necessary to validate your account access.

## Update and start

1. Stop the old API and frontend with Ctrl+C in their respective terminals. A running old API can lock DLLs on Windows.
2. Run `git pull --ff-only` in the repository root.
3. Run the one-time configuration command above.
4. Keep Docker Desktop running and run `docker compose up -d --wait`.
5. Restart the API: `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-api.ps1`. The new migration preserves existing sessions and answers while adding AI response storage and source-quote support.
6. In a second terminal, enter `apps/web`, run `npm.cmd ci`, then `npm.cmd run dev`.

## Continue an existing answer

Open your existing Discover session. The **AI interview** panel shows provider/model and explains the exact context sent externally. Select **Enable OpenAI for this session**. This opt-in is deliberately not retained across page reloads.

If an answer is already saved but has no AI reply, click **Continue with AI / Retry follow-up**. Do not retype your existing answer. Twiggle will send the current session context and save its summary/question separately.

For subsequent answers, use **Send answer**. The answer is saved before model execution. Wait for the summary and next question, respond naturally, and repeat. The AI follows the language of your latest answer; UI labels and code stay English. You can disable AI and keep capturing manually.

Candidate facts appear automatically on the right. Inspect the exact supporting quote and original answer, then accept, edit, dispute, or leave pending. AI origin remains visible after acceptance. The optional manual fact form is collapsed by default.

## Data and validation rules

- Destination is fixed to `https://api.openai.com/v1/responses`. Context includes the session title, original answers, fact statements/statuses, and previous AI summaries/questions. No other session or attachment is sent.
- The request uses `store: false`; this is a provider response-storage setting, not a promise of zero retention. Review your provider’s data controls before processing confidential material.
- Strict structured output is requested. Application validation checks required content, candidate count/length, source membership, and exact contiguous source quotes. A real quote validates citation integrity, not whether the model’s interpretation is semantically correct; user review remains essential.
- Planned work, team work, uncertain terminology, and inferred capabilities must not be promoted to completed personal facts by the prompt. Prompt adherence still needs real interview evaluation.
- Identical successful follow-up retries return the stored response rather than calling the provider again. Per-session local locks serialize concurrent generation; session revisions reject stale writes.
- A 90-second provider timeout, authentication failure, quota error, incomplete output, invalid source, or generation conflict leaves the saved answer intact. Use the explicit retry button. Generation is not automatically repeated in the background.
- This prototype does not have a durable job queue. A process crash after provider completion but before database commit may require a second paid call on retry. Large sessions are rejected explicitly rather than silently dropping early context.
- Saved summaries are historical drafts with model, policy version, and revision. They do not update automatically after a user changes a fact. Subsequent model context includes the current reviewed facts.

## Tests and limits

`tests/Twiggle.AiChecks` uses recorded responses through a fake HTTP handler. It checks request format, key isolation, parsing, citation validation, missing configuration, auth/quota errors, and incomplete output. It never makes a live API call. PostgreSQL integration tests cover the existing capture/review flow plus consent rejection and answer preservation with AI unconfigured. No test fixture is represented as real AI in the product.

A live provider call cannot be verified without an API key. Interview quality, meaningful discoveries, repetition, and behavior under real-world ambiguity must be evaluated on actual sessions after configuration. Browser visual review is separate from compilation.

## Official references checked

- [Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs)
- [GPT-4.1 model](https://developers.openai.com/api/docs/models/gpt-4.1)
- [Responses API](https://developers.openai.com/api/reference/resources/responses/methods/create)
