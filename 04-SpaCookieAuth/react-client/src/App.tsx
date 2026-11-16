import { BrowserRouter, Link, Route, Routes, useNavigate } from 'react-router'
import { api } from './api'
import { AuthProvider, RequireAuth, useAuth } from './auth'
import LoginPage from './LoginPage'
import ProtectedPage from './ProtectedPage'

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Nav />
        <hr />

        <Routes>
          <Route path="/" element={<p>React + cookie authentication. Try <b>Secret</b> and <b>Admin</b>.</p>} />
          <Route path="/login" element={<LoginPage />} />

          {/* Guarded routes. Admin is guarded only by "logged in" on purpose:
              whether you may see it is decided by the API (403). */}
          <Route path="/secret" element={<RequireAuth><ProtectedPage title="Secret" url="/api/secret" /></RequireAuth>} />
          <Route path="/admin" element={<RequireAuth><ProtectedPage title="Admin" url="/api/admin" /></RequireAuth>} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

function Nav() {
  const { user, setUser } = useAuth()
  const navigate = useNavigate()

  async function logout() {
    await api('POST', '/api/account/logout')
    setUser(null)
    navigate('/')
  }

  return (
    <nav>
      <Link to="/">Home</Link> | <Link to="/secret">Secret</Link> | <Link to="/admin">Admin</Link> |{' '}
      {user
        ? <>Logged in as <b>{user.name}</b> <button onClick={logout}>Logout</button></>
        : <Link to="/login">Login</Link>}
    </nav>
  )
}
