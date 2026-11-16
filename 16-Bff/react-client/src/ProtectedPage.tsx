import { useEffect, useState } from 'react'
import { useLocation } from 'react-router'
import { api, login, type ApiResponse } from './api'

// Shows data from the resource API, called through the BFF.
//   200 -> show the data, 401 -> session gone, log in again, 403 -> logged in but not allowed
export default function ProtectedPage({ title, url }: { title: string; url: string }) {
  const [result, setResult] = useState<ApiResponse | null>(null)
  const location = useLocation()

  async function load() {
    const r = await api(url)
    if (r.status === 401) login(location.pathname)
    else setResult(r)
  }

  useEffect(() => {
    setResult(null)
    load()
  }, [url])

  if (!result) return <p>Loading...</p>
  if (result.status === 403) return <p>Access denied: you are logged in, but not allowed to see this.</p>

  const expires = result.data.accessTokenExpires && new Date(result.data.accessTokenExpires)

  return (
    <>
      <h1>{title}</h1>
      <p>{result.data.message}</p>

      {expires && (
        <p>
          The access token the BFF used expires at <b>{expires.toLocaleTimeString()}</b>.{' '}
          <button onClick={load}>Call the API again</button>
          <br />
          It only lives 30 seconds. Call again after it expired: the BFF refreshes it, and the time moves forward.
        </p>
      )}

      {result.data.claims && (
        <table border={1}>
          <tbody>
            {result.data.claims.map((c: { type: string; value: string }) => (
              <tr key={c.type + c.value}>
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
