import { useState, type FormEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import { api } from './api'
import { safeReturnUrl, useAuth } from './auth'

export default function LoginPage() {
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const { setUser } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  async function submit(e: FormEvent) {
    e.preventDefault()

    // On success the response sets the cookie - we only get the user's name back.
    const r = await api('POST', '/api/account/login', { userName, password })
    if (r.status !== 200) {
      setError(r.data?.error)
      return
    }

    setUser(r.data)
    navigate(safeReturnUrl(searchParams.get('returnUrl')))
  }

  return (
    <>
      <h1>Login</h1>
      <form onSubmit={submit}>
        <input placeholder="User name" value={userName} onChange={e => setUserName(e.target.value)} />
        <input placeholder="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} />
        <button type="submit">Login</button>
      </form>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      <p>Try <b>user</b> / <b>user</b> or <b>admin</b> / <b>admin</b>.</p>
    </>
  )
}
