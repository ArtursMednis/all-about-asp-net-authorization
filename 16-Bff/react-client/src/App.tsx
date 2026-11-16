import { BrowserRouter, Link, Route, Routes } from 'react-router'
import { login } from './api'
import { AuthProvider, RequireAuth, useAuth } from './auth'
import ProtectedPage from './ProtectedPage'

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Nav />
        <hr />

        <Routes>
          <Route path="/" element={<p>React + BFF. Try <b>Secret</b>: the data comes from a separate API, called by the BFF with your access token.</p>} />
          <Route path="/secret" element={<RequireAuth><ProtectedPage title="Secret" url="/api/secret" /></RequireAuth>} />
          <Route path="/admin" element={<RequireAuth><ProtectedPage title="Admin" url="/api/admin" /></RequireAuth>} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

function Nav() {
  const { user } = useAuth()

  return (
    <nav>
      <Link to="/">Home</Link> | <Link to="/secret">Secret</Link> | <Link to="/admin">Admin</Link> |{' '}
      {user
        ? <>Logged in as <b>{user.name}</b> <a href={user.logoutUrl}>Logout</a></>
        : <button onClick={() => login()}>Login</button>}
    </nav>
  )
}
