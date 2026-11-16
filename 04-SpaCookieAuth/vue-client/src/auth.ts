import { ref } from 'vue'
import { api, type User } from './api'

// The current user, shared by the whole app.
export const user = ref<User | null>(null)

// The cookie survives page reloads, but JavaScript can't read it - so ask the API who we are.
// Started once when the app loads; the route guard waits for it (see router.ts).
export const userLoaded = api('GET', '/api/account/me').then(r => {
  user.value = r.status === 200 ? r.data : null
})

// Only return to paths inside this app, like Url.IsLocalUrl in tutorial 01.
export function safeReturnUrl(returnUrl: unknown) {
  return typeof returnUrl === 'string' && returnUrl.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/'
}
