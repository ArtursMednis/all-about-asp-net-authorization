// Every request goes to the BFF (same origin). The browser sends the BFF's session cookie,
// the BFF adds the access token. There are no tokens in JavaScript at all.

export type User = { name: string; isAdmin: boolean; logoutUrl: string }

export type ApiResponse = { status: number; data: any }

export async function api(url: string): Promise<ApiResponse> {
  const response = await fetch(url, {
    // Required by the BFF: proves the request came from this app, not from another site (CSRF).
    headers: { 'X-CSRF': '1' },
  })

  const text = await response.text()
  return { status: response.status, data: text ? JSON.parse(text) : null }
}

// Logging in is a full page navigation: BFF -> auth server -> back to returnUrl.
export function login(returnUrl = '/') {
  window.location.href = `/bff/login?returnUrl=${encodeURIComponent(returnUrl)}`
}
