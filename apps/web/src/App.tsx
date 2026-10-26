import { useEffect, useState } from 'react'
import { BrowserRouter, Link, Navigate, NavLink, Route, Routes } from 'react-router-dom'
import './App.css'
import { Discover, ExperienceBank } from './Discover'

type Status = 'checking' | 'connected' | 'unavailable'
function Home() {
  const [attempt, retry] = useState(0)
  const [api, setApi] = useState<Status>('checking')
  const [database, setDatabase] = useState<Status>('checking')
  useEffect(() => {
    const controller = new AbortController()
    let active = true
    const timer = setTimeout(() => controller.abort(), 5000)
    async function check(path: string, update: (value: Status) => void) {
      update('checking')
      try {
        const response = await fetch(path, { signal: controller.signal })
        const data: { status?: string } = await response.json()
        if (active) update(response.ok && ['healthy', 'ready'].includes(data.status ?? '') ? 'connected' : 'unavailable')
      } catch { if (active) update('unavailable') }
    }
    void check('/api/health/live', setApi)
    void check('/api/health/ready', setDatabase)
    return () => { active = false; clearTimeout(timer); controller.abort() }
  }, [attempt])
  return <><p className="eyebrow">YOUR WORKSPACE</p><h1>A little experience.<br />A clearer picture.</h1><p className="intro">Start with something you worked on. Uncover the actions, choices, and capabilities within it.</p><div className="grid"><section className="card feature"><span className="symbol" aria-hidden="true">↗</span><h2>Every story starts somewhere.</h2><p>Discover will help you explore one experience at a time, with room to pause, clarify, and change your mind.</p><Link className="button" to="/app/discover">Explore Discover</Link></section><section className="card"><p className="eyebrow">LOCAL SERVICES</p><h2>A foundation to build on.</h2><div aria-live="polite"><Service label="Application API" value={api} /><Service label="PostgreSQL" value={database} /></div><button onClick={() => retry((n) => n + 1)}>Check connection again</button><p className="small">See the startup guide if a service is unavailable.</p></section></div><section className="card"><h2>Built around what you actually did.</h2><p>Experiences, sources, and interpretations will stay connected. Original accounts and reviewed facts can now be saved. AI summaries and follow-up questions are available after server configuration. Capability discovery is still under development.</p></section></>
}
function Service({ label, value }: { label: string; value: Status }) { return <p className="status"><span>{label}</span><span className={`badge ${value}`}>{value}</span></p> }
const pages = {
  discover: { title: 'Discover your experience.', intro: 'A focused conversation, grounded in your own story.', items: ['Explore a concrete event and your personal contribution.', 'Review facts separately from proposed capabilities.', 'Pause, skip, and resume.'] },
  experiences: { title: 'Your Experience Bank.', intro: 'A home for the work behind your professional story.', items: ['Browse projects and experience episodes.', 'Inspect actions, outcomes, sources, and open questions.', 'Correct records and review affected interpretations.'] },
  capabilities: { title: 'Understand what it demonstrates.', intro: 'Capabilities with context, evidence, and clear boundaries.', items: ['See which actions support a proposed capability.', 'Inspect your contribution and what remains unknown.', 'Explore relationships without reducing yourself to a score.'] },
  settings: { title: 'Your data. Your choices.', intro: 'Privacy and control belong in the foundation.', items: ['Local development identity only; hosted authentication is not configured.', 'AI processing is opt-in per session; uploads are not enabled.', 'Export and deletion controls will arrive with persistence.'] },
}
function Planned({ page }: { page: typeof pages[keyof typeof pages] }) { return <><p className="eyebrow">PHASE 1 · NOT CONNECTED YET</p><h1>{page.title}</h1><p className="intro">{page.intro}</p><section className="card"><h2>What belongs here</h2><ul>{page.items.map((item) => <li key={item}>{item}</li>)}</ul><p className="notice">This module is not connected yet. Manual experience capture is available in Discover; live AI, uploads, and exports remain unavailable.</p><Link to="/app">Back to Home</Link></section></> }
function App() {
  return <BrowserRouter><a className="skip" href="#main">Skip to content</a><div className="shell"><aside><Link className="brand" to="/app">⌁ twiggle.</Link><p className="eyebrow workspace-label">LOCAL WORKSPACE</p><nav aria-label="Main navigation">{[['/app', 'Home'], ['/app/discover', 'Discover'], ['/app/experiences', 'Experience Bank'], ['/app/capabilities', 'Capabilities'], ['/app/settings', 'Settings']].map(([path, name]) => <NavLink key={path} to={path} end>{name}</NavLink>)}</nav><p className="sidebar-note">One twig at a time.<br /><span>Room to grow.</span></p></aside><div className="body"><header><span>Personal workspace</span><span className="badge">Local workspace</span></header><main id="main"><Routes><Route path="/" element={<Navigate to="/app" replace />} /><Route path="/app" element={<Home />} /><Route path="/app/discover" element={<Discover />} /><Route path="/app/discover/:sessionId" element={<Discover />} /><Route path="/app/experiences" element={<ExperienceBank />} />{Object.entries(pages).filter(([key]) => !['discover', 'experiences'].includes(key)).map(([key, page]) => <Route key={key} path={`/app/${key}`} element={<Planned page={page} />} />)}<Route path="*" element={<section className="card"><h1>Page not found.</h1><Link to="/app">Return to Home</Link></section>} /></Routes></main></div></div></BrowserRouter>
}
export default App
