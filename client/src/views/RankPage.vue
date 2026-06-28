<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useMetaStore } from '@/stores/meta'

const router = useRouter()
const metaStore = useMetaStore()

onMounted(async () => {
  await metaStore.fetchRank()
})

const rankIcons: Record<number, string> = {
  1: '🥉',
  2: '🥈',
  3: '🥇',
  4: '💎',
  5: '👑',
}
</script>

<template>
  <div class="min-h-screen bg-slate-900 px-4 py-6 safe-top pb-24">
    <div class="mb-6">
      <button @click="router.back()" class="text-gray-500 text-sm mb-2">&larr; 返回</button>
      <h1 class="text-xl font-bold text-gray-100">段位</h1>
    </div>

    <!-- 段位展示 -->
    <div class="flex flex-col items-center py-10">
      <div class="text-6xl mb-4">{{ rankIcons[metaStore.rank.rankTier] || '🎖️' }}</div>
      <h2 class="text-2xl font-bold text-gray-100 mb-1">{{ metaStore.rank.rankName }}</h2>
      <p class="text-sm text-gray-500">段位等级 {{ metaStore.rank.rankTier }}</p>
    </div>

    <!-- 晋级进度 -->
    <div class="bg-slate-800/50 border border-gray-700/50 rounded-2xl p-5 mb-4">
      <h3 class="text-sm text-gray-400 mb-3">晋级进度</h3>
      <div class="flex items-center gap-2 mb-2">
        <div class="flex-1 h-2 bg-gray-700 rounded-full overflow-hidden">
          <div
            class="h-full bg-amber-500 rounded-full transition-all"
            :style="{ width: `${((5 - metaStore.rank.dungeonCountToNext) / 5) * 100}%` }"
          ></div>
        </div>
        <span class="text-xs text-gray-400">{{ 5 - metaStore.rank.dungeonCountToNext }}/5</span>
      </div>
      <p class="text-xs text-gray-500">
        {{ metaStore.rank.dungeonCountToNext > 0 ? `还需完成 ${metaStore.rank.dungeonCountToNext} 个副本` : '可以参加晋级赛了！' }}
      </p>
    </div>

    <!-- 晋级赛按钮 -->
    <div v-if="metaStore.rank.canPromote" class="mb-4">
      <button
        class="w-full py-4 rounded-2xl bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-500 hover:to-orange-500 text-white font-bold text-lg shadow-lg shadow-amber-500/25 transition-all active:scale-95"
      >
        开始晋级赛
      </button>
    </div>

    <!-- 当前正在晋级 -->
    <div v-if="metaStore.rank.isInPromotion" class="bg-amber-500/10 border border-amber-500/30 rounded-2xl p-4 text-center">
      <p class="text-amber-300 text-sm font-medium">晋级赛进行中</p>
      <p class="text-amber-200/60 text-xs mt-1">完成考核副本以晋级</p>
    </div>

    <!-- 底部导航 -->
    <nav class="fixed bottom-0 left-0 right-0 border-t border-gray-800 bg-slate-900/95 backdrop-blur px-4 py-3 safe-bottom">
      <div class="flex justify-around">
        <router-link to="/" class="flex flex-col items-center text-gray-500 hover:text-gray-300">
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
        <router-link to="/rank" class="flex flex-col items-center text-indigo-400">
          <span class="text-lg">🏆</span>
          <span class="text-[10px] mt-0.5">段位</span>
        </router-link>
      </div>
    </nav>
  </div>
</template>
