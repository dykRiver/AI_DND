<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { getSettlement } from '@/api/game'
import type { SettlementData } from '@/types/game'

const router = useRouter()
const route = useRoute()
const sessionId = route.params.sessionId as string

const settlement = ref<SettlementData | null>(null)
const isLoading = ref(true)
const showPhase = ref(0) // 0=loading, 1=exit, 2=epilogue, 3=score

onMounted(async () => {
  try {
    settlement.value = await getSettlement(sessionId)
    isLoading.value = false
    // 渐进展示三段
    showPhase.value = 1
    setTimeout(() => { showPhase.value = 2 }, 3000)
    setTimeout(() => { showPhase.value = 3 }, 6000)
  } catch (e) {
    console.error('Failed to load settlement:', e)
    isLoading.value = false
  }
})

const scoreLevelColor: Record<string, string> = {
  'S': 'text-amber-300',
  'A': 'text-emerald-300',
  'B': 'text-blue-300',
  'C': 'text-gray-300',
  'D': 'text-gray-500',
}
</script>

<template>
  <div class="min-h-screen bg-slate-900 flex flex-col items-center justify-center px-6 py-10">
    <!-- 加载 -->
    <div v-if="isLoading" class="flex justify-center">
      <div class="w-8 h-8 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <template v-if="settlement && !isLoading">
      <!-- 第一段：退出叙事 -->
      <div v-if="showPhase >= 1" class="max-w-sm w-full mb-8 fade-in">
        <p class="narrative-text text-gray-200 text-center leading-relaxed">
          {{ settlement.exitNarrative }}
        </p>
      </div>

      <!-- 第二段：后日谈 -->
      <div v-if="showPhase >= 2" class="max-w-sm w-full mb-8 fade-in">
        <div class="h-px bg-gradient-to-r from-transparent via-gray-600 to-transparent mb-6"></div>
        <p class="narrative-text text-gray-400 text-center text-sm leading-relaxed italic">
          {{ settlement.epilogue }}
        </p>
      </div>

      <!-- 第三段：评分和奖励 -->
      <div v-if="showPhase >= 3" class="max-w-sm w-full fade-in">
        <div class="h-px bg-gradient-to-r from-transparent via-gray-600 to-transparent mb-6"></div>

        <!-- 评分大字 -->
        <div class="text-center mb-6">
          <div
            class="text-6xl font-bold mb-2"
            :class="scoreLevelColor[settlement.scoreLevel] || 'text-gray-300'"
          >
            {{ settlement.scoreLevel }}
          </div>
          <p class="text-gray-400 text-sm">{{ settlement.comment }}</p>
        </div>

        <!-- 奖励列表 -->
        <div class="bg-slate-800/50 border border-gray-700/50 rounded-2xl p-4 space-y-2">
          <div class="flex justify-between text-sm">
            <span class="text-gray-400">属性点</span>
            <span class="text-indigo-300">+{{ settlement.rewards.attributePoints }}</span>
          </div>
          <div class="flex justify-between text-sm">
            <span class="text-gray-400">技能点</span>
            <span class="text-emerald-300">+{{ settlement.rewards.skillPoints }}</span>
          </div>
          <div class="flex justify-between text-sm">
            <span class="text-gray-400">Meta经验</span>
            <span class="text-amber-300">+{{ settlement.rewards.metaExp }}</span>
          </div>
          <div class="flex justify-between text-sm">
            <span class="text-gray-400">天赋碎片</span>
            <span class="text-purple-300">+{{ settlement.rewards.talentFragments }}</span>
          </div>
        </div>

        <!-- 返回按钮 -->
        <button
          @click="router.push('/')"
          class="w-full mt-6 py-3 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-medium transition-colors"
        >
          返回大厅
        </button>
      </div>
    </template>
  </div>
</template>
