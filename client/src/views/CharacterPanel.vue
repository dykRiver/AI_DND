<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useGameStore } from '@/stores/game'
import HpBar from '@/components/HpBar.vue'

const router = useRouter()
const gameStore = useGameStore()
</script>

<template>
  <div class="min-h-screen bg-slate-900 px-4 py-6 safe-top">
    <div class="mb-6">
      <button @click="router.back()" class="text-gray-500 text-sm mb-2">&larr; 返回</button>
      <h1 class="text-xl font-bold text-gray-100">角色面板</h1>
    </div>

    <!-- HP -->
    <div class="bg-slate-800/50 border border-gray-700/50 rounded-2xl p-5 mb-4">
      <h3 class="text-sm text-gray-400 mb-3">生命值</h3>
      <HpBar
        :current="gameStore.gameState.currentHp"
        :max="gameStore.gameState.maxHp"
        :percent="gameStore.gameState.hpPercent"
      />
      <div class="mt-2 text-xs text-gray-500">
        状态: <span class="text-gray-300">{{ gameStore.gameState.status }}</span>
      </div>
    </div>

    <!-- 当前状态 -->
    <div class="bg-slate-800/50 border border-gray-700/50 rounded-2xl p-5 mb-4">
      <h3 class="text-sm text-gray-400 mb-3">当前状态</h3>
      <div class="grid grid-cols-2 gap-3 text-sm">
        <div>
          <span class="text-gray-500">时间</span>
          <p class="text-gray-200">Day{{ gameStore.gameState.currentDay }} · {{ gameStore.gameState.currentSegment }}</p>
        </div>
        <div>
          <span class="text-gray-500">紧张度</span>
          <p class="text-gray-200">{{ gameStore.gameState.tensionLevel }}/10</p>
        </div>
        <div>
          <span class="text-gray-500">疲劳</span>
          <p :class="gameStore.gameState.isFatigued ? 'text-amber-400' : 'text-emerald-400'">
            {{ gameStore.gameState.isFatigued ? '是' : '否' }}
          </p>
        </div>
        <div>
          <span class="text-gray-500">战斗中</span>
          <p :class="gameStore.gameState.isInCombat ? 'text-rose-400' : 'text-emerald-400'">
            {{ gameStore.gameState.isInCombat ? '是' : '否' }}
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
