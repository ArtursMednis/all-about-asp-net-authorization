import { createRouter, createWebHistory } from 'vue-router'
import { user, userLoaded } from './auth'
import HomePage from './HomePage.vue'
import LoginPage from './LoginPage.vue'
import ProtectedPage from './ProtectedPage.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomePage },
    { path: '/login', component: LoginPage },

    // Guarded routes. Admin is guarded only by "logged in" on purpose:
    // whether you may see it is decided by the API (403).
    { path: '/secret', component: ProtectedPage, props: { title: 'Secret', url: '/api/secret' }, meta: { requiresAuth: true } },
    { path: '/admin', component: ProtectedPage, props: { title: 'Admin', url: '/api/admin' }, meta: { requiresAuth: true } },
  ],
})

// Route guard - the SPA version of [Authorize]: anonymous users go to the login page,
// with a return URL to come back to. This is for the user's convenience only:
// the API checks the cookie on every request anyway.
router.beforeEach(async to => {
  await userLoaded

  if (to.meta.requiresAuth && !user.value) {
    return { path: '/login', query: { returnUrl: to.fullPath } }
  }
})
