/* Temporary bootstrap UI. Not final visual design. */
import { useState, type FormEvent } from 'react'
import { Navigate } from 'react-router'
import { useAuthSession } from './AuthContext'

export function LoginPage() {
  const { status, signIn } = useAuthSession()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (status === 'checking') {
    return <p>Checking session…</p>
  }

  if (status === 'authenticated') {
    return <Navigate to="/app" replace />
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)

    try {
      await signIn(email, password)
    } catch {
      setError('Sign-in failed. Check your details and try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main>
      <h1>HuGuWeb</h1>
      <p>Temporary bootstrap sign-in. Not final product UI.</p>
      <form onSubmit={onSubmit}>
        <label htmlFor="email">Email</label>
        <input
          id="email"
          name="email"
          type="email"
          autoComplete="username"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
        />

        <label htmlFor="password">Password</label>
        <input
          id="password"
          name="password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
        />

        <button type="submit" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
      {error ? <p className="error" role="alert">{error}</p> : null}
    </main>
  )
}
