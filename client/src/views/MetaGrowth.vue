<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useMetaStore } from '@/stores/meta'
import { unlockTalentNode } from '@/api/game'
import TalentTree from '@/components/TalentTree.vue'

const router = useRouter()
const metaStore = useMetaStore()

onMounted(async () => {
  await metaStore.fetchAll()
})

async function handleUnlock(nodePath: string) {
  if (!metaStore.meta.id) return
  const success = await unlockTalentNode(metaStore.meta.id, nodePath)
  if (success) {
    await metaStore.fetchTalentTree()
    await metaStore.fetchMeta()
  }
}

const attrLabels: Record<string, string> = {
  bonusStrength: '力量',
  bonusDexterity: '敏捷',
  bonusConstitution: '体质',
  bonusIntelligence: '智力',
  bonusWisdom: '感知',
  bonusCharisma: '魅力',
}

function getAttrBonus(key: string): number {
  const meta = metaStore.meta as Record<string, any>
  return meta[key] || 0
}
</script>

<template>
  <div class="min-h-screen bg-slate-900 px-4 py-6 safe-top pb-24">
    <div class="mb-6">
      <button @click="router.back()" class="text-gray-500 text-sm mb-2">&larr; 返回</button>
      <h1 class="text-xl font-bold text-gray-100">Meta成长</h1>
    </div>

    <!-- 等级经验条 -->
    <div class="bg-slate-800/50 border border-gray-700/50 rounded-2xl p-5 mb-4">
      <div class="flex items-center justify-between mb-2">
        <span class="text-sm text-gray-400">Meta等级</span>
        <span class="text-lg font-bold text-indigo-400">Lv.{{ metaStore.meta.metaLevel }}</span>
      </div>
      <div class="h-2 bg-gray-700 rounded-full overflow-hidden">
        <div
          class="h-full bg-indigo-500 rounded-full transition-all"
          :style="{ width: `${Math.min((metaStore.meta.experience % 100), 100)}%` }"
        ></div>
      </div>
      <div class="text-xs text-gray-500 mt-1 text-right">{{ metaStore.meta.experience }} EXP</div>
    </div>

    <!-- 属性加成 -->
    <div class="bg-slate-800/50 border border-gray-700/50 rounded-2xl p-5 mb-4">
      <h3 class="text-sm text-gray-400 mb-3">属性加成</h3>
      <div class="grid grid-cols-3 gap-3">
        <div v-for="(label, key) in attrLabels" :key="key" class="text-center">
          <div class="text-lg font-bold text-gray-200">
            +{{ getAttrBonus(key) }}
          </div>
          <div class="text-xs text-gray-500">{{ label }}</div>
        </div>
      </div>
    </div>

    <!-- 天赋树 -->
    <div class="bg-slate-800/50 border border-gray-700/50 rounded-2xl p-5 mb-4">
      <TalentTree
        :nodes="metaStore.talentNodes"
        :available-points="metaStore.availableTalentPoints"
        @unlock="handleUnlock"
      />
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
        <router-link to="/meta" class="flex flex-col items-center text-indigo-400">
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
