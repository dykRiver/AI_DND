<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const username = ref('')
const password = ref('')
const isLoading = ref(false)
const errorMsg = ref('')

async function handleLogin() {
  if (!username.value || !password.value) {
    errorMsg.value = '请输入用户名和密码'
    return
  }
  isLoading.value = true
  errorMsg.value = ''
  try {
    await authStore.login({ account: username.value, password: password.value })
    router.push('/')
  } catch (err: any) {
    errorMsg.value = err?.response?.data?.message || '登录失败，请重试'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex flex-col items-center justify-center px-6 bg-slate-900">
    <!-- 标题 -->
    <div class="text-center mb-10">
      <h1 class="text-3xl font-bold text-gray-100 tracking-tight">无限流</h1>
      <p class="text-gray-500 text-sm mt-2">AI驱动的文字冒险RPG</p>
    </div>

    <!-- 登录表单 -->
    <div class="w-full max-w-sm space-y-5">
      <div>
        <input
          v-model="username"
          type="text"
          placeholder="用户名"
          class="w-full px-4 py-3 bg-slate-800 border border-gray-700/50 rounded-xl text-gray-100 placeholder-gray-500 focus:outline-none focus:border-indigo-500/70 focus:ring-1 focus:ring-indigo-500/30"
          @keydown.enter="handleLogin"
        />
      </div>
      <div>
        <input
          v-model="password"
          type="password"
          placeholder="密码"
          class="w-full px-4 py-3 bg-slate-800 border border-gray-700/50 rounded-xl text-gray-100 placeholder-gray-500 focus:outline-none focus:border-indigo-500/70 focus:ring-1 focus:ring-indigo-500/30"
          @keydown.enter="handleLogin"
        />
      </div>

      <!-- 错误提示 -->
      <p v-if="errorMsg" class="text-rose-400 text-sm text-center">{{ errorMsg }}</p>

      <!-- 登录按钮 -->
      <button
        @click="handleLogin"
        :disabled="isLoading"
        class="w-full py-3 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {{ isLoading ? '登录中...' : '进入游戏' }}
      </button>
    </div>

    <!-- 底部装饰 -->
    <div class="mt-16 text-gray-600 text-xs">
      "每一次选择，都通向未知的命运"
    </div>
  </div>
</template>
