import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import { App } from './app/App'
import { AuthSessionProvider } from './auth/AuthSession'
import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <AuthSessionProvider>
        <App />
      </AuthSessionProvider>
    </BrowserRouter>
  </StrictMode>,
)
