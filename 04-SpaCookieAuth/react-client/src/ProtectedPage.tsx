import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { api, type ApiResponse } from './api'
import { useAuth } from './auth'

// Shows data from a protected API endpoint. The API decides who may see it:
//   200 -> show the data, 401 -> not logged in (anymore), 403 -> logged in but not allowed
export default function ProtectedPage({ title, url }: { title: string; url: string }) {
  const [result, setResult] = useState<ApiResponse | null>(null)
  const { setUser } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  useEffect(() => {
    setResult(null)
    api('GET', url).then(r => {
      if (r.status === 401) {
        // The cookie expired or was removed - the guard thought we were logged in, the API knows better.
        setUser(null)
        navigate(`/login?returnUrl=${encodeURIComponent(location.pathname)}`)
      } else {
        setResult(r)
      }
    })
  }, [url])

  if (!result) return <p>Loading...</p>
  if (result.status === 403) return <p>Access denied: you are logged in, but not allowed to see this.</p>

  return (
    <>
      <h1>{title}</h1>
      <p>{result.data.message}</p>

      {result.data.claims && (
        <table border={1}>
          <tbody>
            {result.data.claims.map((c: { type: string; value: string }) => (
              <tr key={c.type}>
                <td>{c.type}</td>
                <td>{c.value}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  )
}
