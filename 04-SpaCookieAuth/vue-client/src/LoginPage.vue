<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api } from './api'
import { safeReturnUrl, user } from './auth'

const userName = ref('')
const password = ref('')
const error = ref('')
const route = useRoute()
const router = useRouter()

async function submit() {
  // On success the response sets the cookie - we only get the user's name back.
  const r = await api('POST', '/api/account/login', { userName: userName.value, password: password.value })
  if (r.status !== 200) {
    error.value = r.data?.error
    return
  }

  user.value = r.data
  router.push(safeReturnUrl(route.query.returnUrl))
}
</script>

<template>
  <h1>Login</h1>
  <form @submit.prevent="submit">
    <input v-model="userName" placeholder="User name" />
    <input v-model="password" placeholder="Password" type="password" />
    <button type="submit">Login</button>
  </form>
  <p v-if="error" style="color: red">{{ error }}</p>
  <p>Try <b>user</b> / <b>user</b> or <b>admin</b> / <b>admin</b>.</p>
</template>
