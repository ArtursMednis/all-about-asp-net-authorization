<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api, type ApiResponse } from './api'
import { user } from './auth'

// Shows data from a protected API endpoint. The API decides who may see it:
//   200 -> show the data, 401 -> not logged in (anymore), 403 -> logged in but not allowed
const props = defineProps<{ title: string; url: string }>()

const result = ref<ApiResponse | null>(null)
const route = useRoute()
const router = useRouter()

// Load again when switching between Secret and Admin (Vue reuses this component).
watch(() => props.url, async url => {
  result.value = null
  const r = await api('GET', url)

  if (r.status === 401) {
    // The cookie expired or was removed - the guard thought we were logged in, the API knows better.
    user.value = null
    router.push({ path: '/login', query: { returnUrl: route.fullPath } })
  } else {
    result.value = r
  }
}, { immediate: true })
</script>

<template>
  <p v-if="!result">Loading...</p>
  <p v-else-if="result.status === 403">Access denied: you are logged in, but not allowed to see this.</p>
  <template v-else>
    <h1>{{ title }}</h1>
    <p>{{ result.data.message }}</p>

    <table v-if="result.data.claims" border="1">
      <tr v-for="claim in result.data.claims" :key="claim.type">
        <td>{{ claim.type }}</td>
        <td>{{ claim.value }}</td>
      </tr>
    </table>
  </template>
</template>
