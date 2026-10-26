export type Turn = { id: string; text: string; createdAt: string }
export type Claim = { id: string; sourceTurnId: string; statement: string; reviewStatus: string; origin: string; revisions: { statement: string; reviewStatus: string; sessionRevision: number; recordedAt: string }[] }
export type Session = { id: string; title: string; revision: number; episodeId: string | null; turns: Turn[]; claims: Claim[] }
export type SessionSummary = { id: string; title: string; saved: boolean }
export type Episode = { id: string; sessionId: string; title: string; savedAt: string; acceptedFacts: number }
export async function request<T>(path: string, body?: unknown, method = 'POST'): Promise<T> {
  const response = await fetch(`/api${path}`, body === undefined ? undefined : { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
  const text = await response.text()
  let data: { message?: string; title?: string } = {}
  try { data = JSON.parse(text) as typeof data } catch { /* Proxy or server failure may not be JSON. */ }
  if (!response.ok) throw new Error(data.message ?? data.title ?? 'The request failed. Check the API and database. Your text has been kept.')
  return data as T
}
