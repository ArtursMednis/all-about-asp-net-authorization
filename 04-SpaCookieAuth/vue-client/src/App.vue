<script setup lang="ts">
import { useRouter } from 'vue-router'
import { api } from './api'
import { user } from './auth'

const router = useRouter()

async function logout() {
  await api('POST', '/api/account/logout')
  user.value = null
  router.push('/')
}
</script>

<template>
  <nav>
    <RouterLink to="/">Home</RouterLink> | <RouterLink to="/secret">Secret</RouterLink> |
    <RouterLink to="/admin">Admin</RouterLink> |
    <template v-if="user">
      Logged in as <b>{{ user.name }}</b> <button @click="logout">Logout</button>
    </template>
    <RouterLink v-else to="/login">Login</RouterLink>
  </nav>
  <hr />

  <RouterView />
</template>
