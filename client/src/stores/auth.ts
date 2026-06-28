import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { login as apiLogin, getUserInfo } from '@/api/game'
import type { LoginRequest } from '@/types/game'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string>(localStorage.getItem('token') || '')
  const userId = ref<number>(Number(localStorage.getItem('userId')) || 0)
  const username = ref<string>(localStorage.getItem('username') || '')

  const isAuthenticated = computed(() => !!token.value)

  async function login(data: LoginRequest) {
    const res = await apiLogin(data)
    token.value = res.accessToken
    localStorage.setItem('token', res.accessToken)

    // 登录成功后通过接口获取用户信息，而非解析 JWT
    const info = await getUserInfo()
    userId.value = info.id
    username.value = info.account

    localStorage.setItem('userId', String(info.id))
    localStorage.setItem('username', info.account)
  }

  function logout() {
    token.value = ''
    userId.value = 0
    username.value = ''
    localStorage.removeItem('token')
    localStorage.removeItem('userId')
    localStorage.removeItem('username')
  }

  return {
    token,
    userId,
    username,
    isAuthenticated,
    login,
    logout,
  }
})
