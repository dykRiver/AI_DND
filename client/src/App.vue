<script setup lang="ts">
import { useGameStore } from '@/stores/game'

const gameStore = useGameStore()
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-gray-100">
    <router-view />

    <!-- 全局系统消息 (非游戏页面时显示) -->
    <div class="fixed top-4 left-4 right-4 z-50 space-y-2 pointer-events-none">
      <div
        v-for="(msg, idx) in gameStore.systemMessages"
        :key="idx"
        class="fade-in px-4 py-2 rounded-xl text-sm backdrop-blur pointer-events-auto"
        :class="{
          'bg-blue-500/20 border border-blue-500/30 text-blue-300': msg.type === 'info',
          'bg-amber-500/20 border border-amber-500/30 text-amber-300': msg.type === 'warning',
          'bg-rose-500/20 border border-rose-500/30 text-rose-300': msg.type === 'error',
          'bg-emerald-500/20 border border-emerald-500/30 text-emerald-300': msg.type === 'success',
        }"
      >
        {{ msg.content }}
      </div>
    </div>
  </div>
</template>
