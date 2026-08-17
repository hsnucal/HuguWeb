import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate } from 'react-router'
import { useAuthSession } from './AuthContext'
import { selectLanguageLocal } from '../i18n/preference'
import { BrandMark } from '../ui/BrandMark'
import { Button } from '../ui/Button'
import { LanguageSelect } from '../ui/LanguageSelect'
import { SessionNotice } from '../ui/SessionNotice'
import { Surface } from '../ui/Surface'
import { TextField } from '../ui/TextField'
import styles from './LoginPage.module.css'

export function LoginPage() {
  const { t } = useTranslation()
  const { status, signIn } = useAuthSession()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [failed, setFailed] = useState(false)

  if (status === 'checking') {
    return <SessionNotice>{t('auth.checkingSession')}</SessionNotice>
  }

  if (status === 'authenticated') {
    return <Navigate to="/app" replace />
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting) {
      return
    }

    setFailed(false)
    setSubmitting(true)

    try {
      await signIn(email, password)
    } catch {
      setFailed(true)
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
            <p className={styles.statement}>{t('auth.hotelOperations')}</p>
          </div>
        </section>

        <section className={styles.auth}>
          <Surface className={styles.card} raised>
            <div className={styles.cardHeader}>
              <div>
                <h1 className={styles.welcome}>{t('auth.welcomeBack')}</h1>
                <p className={styles.lead}>{t('auth.signInToContinue')}</p>
              </div>
              <LanguageSelect
                id="login-language"
                className={styles.language}
                disabled={submitting}
                onChange={selectLanguageLocal}
              />
            </div>

            <form
              className={styles.form}
              onSubmit={onSubmit}
              aria-busy={submitting}
              aria-describedby={failed ? 'login-error' : undefined}
            >
              <TextField
                id="email"
                label={t('auth.email')}
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
                label={t('auth.password')}
                name="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={setPassword}
                required
                disabled={submitting}
              />

              {failed ? (
                <p className={styles.error} id="login-error" role="alert">
                  {t('auth.signInFailed')}
                </p>
              ) : null}

              <Button type="submit" disabled={submitting}>
                {submitting ? t('auth.signingIn') : t('auth.signIn')}
              </Button>
            </form>
          </Surface>
        </section>
      </div>
    </div>
  )
}
