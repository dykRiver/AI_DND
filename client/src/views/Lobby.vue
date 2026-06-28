<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useMetaStore } from '@/stores/meta'
import { useAuthStore } from '@/stores/auth'
import { useGameStore } from '@/stores/game'
import { useSignalR } from '@/composables/useSignalR'
import { checkActiveSession } from '@/api/game'
import type { ActiveSessionResult } from '@/types/game'

const router = useRouter()
const metaStore = useMetaStore()
const authStore = useAuthStore()
const gameStore = useGameStore()
const signalR = useSignalR()

// 断线续玩
const activeSession = ref<ActiveSessionResult | null>(null)
const isChecking = ref(false)
const isResuming = ref(false)

onMounted(async () => {
  await metaStore.fetchAll()
  // 检查是否有进行中的副本
  isChecking.value = true
  try {
    const result = await checkActiveSession(authStore.userId)
    if (result) activeSession.value = result
  } catch {
    // 检查失败不影响正常流程
  } finally {
    isChecking.value = false
  }
})

function startDungeon() {
  router.push('/dungeon-select')
}

async function resumeDungeon() {
  if (!activeSession.value) return
  isResuming.value = true
  try {
    // 1. 从服务端数据恢复客户端状态
    gameStore.restoreFromServer({
      sessionId: activeSession.value.sessionId.toString(),
      worldInfo: activeSession.value.worldInfo,
      gameState: activeSession.value.gameState,
    })

    // 恢复叙事历史
    for (const n of activeSession.value.recentNarratives) {
      gameStore.appendNarrative({
        text: n.text,
        chunkType: n.chunkType,
        isLast: true,
        timestamp: new Date().toISOString(),
      })
    }

    // 2. 连接 SignalR 并注册 DungeonReady 回调
    await signalR.connect()
    signalR.onDungeonReady(() => {
      router.push('/game')
    })

    // 3. 通过 SelectDungeon 触发服务端续玩流程（后端会自动识别已有会话）
    await signalR.selectDungeon({
      dungeonTemplateId: activeSession.value.templateId,
      characterName: '',
      strength: 10,
      dexterity: 10,
      constitution: 10,
      intelligence: 10,
      wisdom: 10,
      charisma: 10,
    })
  } catch (err) {
    console.error('续玩失败:', err)
    gameStore.addSystemMessage({ type: 'error', content: '续玩恢复失败，请重试' })
    isResuming.value = false
  }
}

function dismissResume() {
  activeSession.value = null
}

function logout() {
  signalR.destroy()
  authStore.logout()
  gameStore.clearSession()
  router.push('/login')
}
</script>

<template>
  <div class="min-h-screen bg-slate-900 flex flex-col">
    <!-- 顶部 -->
    <div class="px-4 pt-6 pb-4 safe-top">
      <div class="flex items-center justify-between">
        <div>
          <h2 class="text-lg font-bold text-gray-100">{{ authStore.username }}</h2>
          <p class="text-xs text-gray-500">Meta Lv.{{ metaStore.meta.metaLevel }} · {{ metaStore.rank.rankName }}</p>
        </div>
        <button @click="logout" class="text-xs text-gray-500 hover:text-gray-300">退出</button>
      </div>
    </div>

    <!-- 主内容 -->
    <div class="flex-1 flex flex-col items-center justify-center px-6">
      <!-- 续玩提示卡片 -->
      <div
        v-if="activeSession"
        class="w-full max-w-sm bg-amber-900/30 border border-amber-600/40 rounded-2xl p-5 mb-6"
      >
        <div class="text-center mb-3">
          <p class="text-amber-400 text-sm font-bold">🗡️ 进行中的副本</p>
          <p class="text-gray-200 text-base font-bold mt-1">{{ activeSession.dungeonName }}</p>
          <p class="text-gray-400 text-xs mt-1">第 {{ activeSession.gameState.currentDay }} 天 · {{ activeSession.gameState.currentSegment }} · HP {{ activeSession.gameState.currentHp }}/{{ activeSession.gameState.maxHp }}</p>
        </div>
        <div class="flex gap-3">
          <button
            @click="resumeDungeon"
            :disabled="isResuming"
            class="flex-1 py-2.5 rounded-xl bg-amber-600 hover:bg-amber-500 disabled:opacity-50 text-white font-bold text-sm transition-all active:scale-95"
          >
            {{ isResuming ? '恢复中...' : '继续副本' }}
          </button>
          <button
            @click="dismissResume"
            class="px-4 py-2.5 rounded-xl bg-slate-700 hover:bg-slate-600 text-gray-300 text-sm transition-all active:scale-95"
          >
            忽略
          </button>
        </div>
      </div>

      <!-- Meta信息卡 -->
      <div class="w-full max-w-sm bg-slate-800/50 border border-gray-700/50 rounded-2xl p-5 mb-8">
        <div class="grid grid-cols-2 gap-3 text-center">
          <div>
            <div class="text-2xl font-bold text-indigo-400">{{ metaStore.meta.metaLevel }}</div>
            <div class="text-xs text-gray-500 mt-1">Meta等级</div>
          </div>
          <div>
            <div class="text-2xl font-bold text-amber-400">{{ metaStore.meta.dungeonCount }}</div>
            <div class="text-xs text-gray-500 mt-1">副本次数</div>
          </div>
          <div>
            <div class="text-2xl font-bold text-emerald-400">{{ metaStore.rank.rankName }}</div>
            <div class="text-xs text-gray-500 mt-1">当前段位</div>
          </div>
          <div>
            <div class="text-2xl font-bold text-purple-400">{{ metaStore.meta.talentPoints }}</div>
            <div class="text-xs text-gray-500 mt-1">天赋点数</div>
          </div>
        </div>
      </div>

      <!-- 开始按钮 -->
      <button
        @click="startDungeon"
        class="w-full max-w-sm py-4 rounded-2xl bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white font-bold text-lg shadow-lg shadow-indigo-500/25 transition-all active:scale-95"
      >
        开始副本
      </button>
    </div>

    <!-- 底部导航 -->
    <nav class="border-t border-gray-800 bg-slate-900/95 backdrop-blur px-4 py-3 safe-bottom">
      <div class="flex justify-around">
        <router-link to="/" class="flex flex-col items-center text-indigo-400">
          <span class="text-lg">🏠</span>
          <span class="text-[10px] mt-0.5">大厅</span>
        </router-link>
        <router-link to="/character" class="flex flex-col items-center text-gray-500 hover:text-gray-300">
          <span class="text-lg">👤</span>
          <span class="text-[10px] mt-0.5">角色</span>
        </router-link>
        <router-link to="/meta" class="flex flex-col items-center text-gray-500 hover:text-gray-300">
          <span class="text-lg">🌟</span>
          <span class="text-[10px] mt-0.5">天赋</span>
        </router-link>
        <router-link to="/rank" class="flex flex-col items-center text-gray-500 hover:text-gray-300">
          <span class="text-lg">🏆</span>
          <span class="text-[10px] mt-0.5">段位</span>
        </router-link>
      </div>
    </nav>
  </div>
</template>
