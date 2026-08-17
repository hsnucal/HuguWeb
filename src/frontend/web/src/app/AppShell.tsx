/* Temporary bootstrap shell. Not a product dashboard. */
import { useAuthSession } from '../auth/AuthContext'

export function AppShell() {
  const { user, signOut } = useAuthSession()

  async function onLogout() {
    await signOut()
  }

  return (
    <main>
      <h1>HuGuWeb</h1>
      <p>Authenticated bootstrap shell. No hotel domain functionality is implemented.</p>
      <p>Signed in as {user?.email ?? user?.id ?? 'unknown user'}.</p>
      <p className="status">Technical status: application foundation is running.</p>
      <button type="button" onClick={() => void onLogout()}>
        Sign out
      </button>
    </main>
  )
}
