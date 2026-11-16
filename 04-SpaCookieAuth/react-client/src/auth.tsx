import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { api, type User } from './api'

type Auth = {
  user: User | null
  loading: boolean
  setUser: (user: User | null) => void
}

const AuthContext = createContext<Auth>(null!)

export const useAuth = () => useContext(AuthContext)

// Holds the current user for the whole app.
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  // The cookie survives page reloads, but JavaScript can't read it - so ask the API who we are.
  useEffect(() => {
    api('GET', '/api/account/me').then(r => {
      setUser(r.status === 200 ? r.data : null)
      setLoading(false)
    })
  }, [])

  return <AuthContext.Provider value={{ user, loading, setUser }}>{children}</AuthContext.Provider>
}

// Route guard - the SPA version of [Authorize]: anonymous users go to the login page,
// with a return URL to come back to. This is for the user's convenience only:
// the API checks the cookie on every request anyway.
export function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) return <p>Loading...</p>
  if (!user) return <Navigate to={`/login?returnUrl=${encodeURIComponent(location.pathname)}`} replace />

  return children
}

// Only return to paths inside this app, like Url.IsLocalUrl in tutorial 01.
export function safeReturnUrl(returnUrl: string | null) {
  return returnUrl?.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/'
}
