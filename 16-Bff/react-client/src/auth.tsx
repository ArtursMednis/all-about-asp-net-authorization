import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { useLocation } from 'react-router'
import { api, login, type User } from './api'

type Auth = { user: User | null; loading: boolean }

const AuthContext = createContext<Auth>(null!)

export const useAuth = () => useContext(AuthContext)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  // Ask the BFF who is logged in (it knows from its session cookie).
  useEffect(() => {
    api('/bff/user').then(r => {
      setUser(r.status === 200 ? r.data : null)
      setLoading(false)
    })
  }, [])

  return <AuthContext.Provider value={{ user, loading }}>{children}</AuthContext.Provider>
}

// Route guard, as in tutorial 04 - but the login page lives on the auth server,
// so instead of a route we navigate to the BFF's login endpoint.
export function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth()
  const location = useLocation()

  useEffect(() => {
    if (!loading && !user) login(location.pathname)
  }, [loading, user, location.pathname])

  if (!user) return <p>Loading...</p>
  return children
}
