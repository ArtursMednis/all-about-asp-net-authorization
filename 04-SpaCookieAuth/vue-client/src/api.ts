// Every request goes to our own origin (/api/...), so the browser sends the auth cookie
// automatically. There is no token to store or attach - JavaScript never sees the cookie.

export type User = { name: string; isAdmin: boolean }

export type ApiResponse = { status: number; data: any }

export async function api(method: 'GET' | 'POST', url: string, body?: unknown): Promise<ApiResponse> {
  const response = await fetch(url, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  })

  const text = await response.text()
  return { status: response.status, data: text ? JSON.parse(text) : null }
}
