import { useState, type FormEvent } from 'react'
import { Navigate } from 'react-router'
import { useAuthSession } from './AuthContext'
import { BrandMark } from '../ui/BrandMark'
import { Button } from '../ui/Button'
import { SessionNotice } from '../ui/SessionNotice'
import { Surface } from '../ui/Surface'
import { TextField } from '../ui/TextField'
import styles from './LoginPage.module.css'

export function LoginPage() {
  const { status, signIn } = useAuthSession()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (status === 'checking') {
    return <SessionNotice>Checking session…</SessionNotice>
  }

  if (status === 'authenticated') {
    return <Navigate to="/app" replace />
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting) {
      return
    }

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
    <div className={styles.page}>
      <div className={styles.layout}>
        <section className={styles.brand} aria-label="HuGuWeb">
          <div className={styles.atmosphere} aria-hidden="true">
            <span className={styles.mullion} />
            <span className={styles.linen} />
            <span className={styles.orb} />
            <span className={styles.arc} />
            <span className={styles.arcSoft} />
            <span className={styles.panel} />
            <span className={styles.panelInner} />
            <span className={styles.horizon} />
          </div>
          <div className={styles.brandCopy}>
            <div className={styles.wordmarkRow}>
              <BrandMark size="lg" />
              <p className={styles.wordmark}>HuGuWeb</p>
            </div>
            <div className={styles.rule} />
            <p className={styles.statement}>Hotel operations, in one calm workspace.</p>
          </div>
        </section>

        <section className={styles.auth}>
          <Surface className={styles.card} raised>
            <h1 className={styles.welcome}>Welcome back</h1>
            <p className={styles.lead}>Sign in to continue to HuGuWeb.</p>

            <form
              className={styles.form}
              onSubmit={onSubmit}
              aria-busy={submitting}
              aria-describedby={error ? 'login-error' : undefined}
            >
              <TextField
                id="email"
                label="Email"
                name="email"
                type="email"
                autoComplete="username"
                value={email}
                onChange={setEmail}
                required
                disabled={submitting}
              />
              <TextField
                id="password"
                label="Password"
                name="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={setPassword}
                required
                disabled={submitting}
              />

              {error ? (
                <p className={styles.error} id="login-error" role="alert">
                  {error}
                </p>
              ) : null}

              <Button type="submit" disabled={submitting}>
                {submitting ? 'Signing in…' : 'Sign in'}
              </Button>
            </form>
          </Surface>
        </section>
      </div>
    </div>
  )
}
