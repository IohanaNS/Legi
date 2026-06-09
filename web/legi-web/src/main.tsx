import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './i18n'
import './styles/index.css'
import App from './app/App.tsx'

// Apply theme before React mounts to prevent flash of unstyled content.
// Use the saved preference if the user picked one, otherwise follow the OS.
const savedTheme = localStorage.getItem('legi.theme')
const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
if (savedTheme === 'dark' || (!savedTheme && prefersDark)) {
  document.documentElement.classList.add('dark')
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
