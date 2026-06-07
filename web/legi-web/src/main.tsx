import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './i18n'
import './styles/index.css'
import App from './app/App.tsx'

// Apply saved theme before React mounts to prevent flash of unstyled content.
if (localStorage.getItem('legi.theme') === 'dark') {
  document.documentElement.classList.add('dark')
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
