import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate } from 'react-router'
import { selectLanguageLocal } from '../i18n/preference'
import { BrandMark } from '../ui/BrandMark'
import { Button } from '../ui/Button'
import { LanguageSelect } from '../ui/LanguageSelect'
import { Notice } from '../ui/Notice'
import { SessionNotice } from '../ui/SessionNotice'
import { Surface } from '../ui/Surface'
import { TextField } from '../ui/TextField'
import { AmbientBrandMark } from './AmbientBrandMark'
import { useAuthSession } from './AuthContext'
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
      <div className={styles.atmosphere} aria-hidden="true">
        <span className={styles.grid} />
        <span className={styles.wash} />
        <span className={styles.mullion} />
        <span className={styles.lintel} />
        <span className={styles.orb} />
        <span className={styles.orbSoft} />
        <span className={styles.arc} />
        <span className={styles.arcInner} />
        <span className={styles.plane} />
        <AmbientBrandMark />
      </div>

      <div className={styles.top}>
        <LanguageSelect
          id="login-language"
          className={styles.language}
          disabled={submitting}
          onChange={selectLanguageLocal}
        />
      </div>

      <div className={styles.stage}>
        <section className={styles.identity} aria-label="HuGu">
          <BrandMark size="login" label="HuGuWeb" />
          <p className={styles.wordmark}>HuGu</p>
          <p className={styles.statement}>{t('auth.hotelOperations')}</p>
        </section>

        <Surface className={styles.card} raised>
          <h1 className={styles.welcome}>{t('auth.welcomeBack')}</h1>
          <p className={styles.lead}>{t('auth.signInToContinue')}</p>

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
              <Notice tone="danger" className={styles.formNotice}>
                <span id="login-error">{t('auth.signInFailed')}</span>
              </Notice>
            ) : null}

            <Button type="submit" loading={submitting}>
              {submitting ? t('auth.signingIn') : t('auth.signIn')}
            </Button>
          </form>
        </Surface>
      </div>
    </div>
  )
}
