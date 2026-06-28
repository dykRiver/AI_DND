<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { getDungeonTemplates } from '@/api/game'
import type { DungeonTemplate } from '@/types/game'

const router = useRouter()
const templates = ref<DungeonTemplate[]>([])
const isLoading = ref(true)

onMounted(async () => {
  try {
    templates.value = await getDungeonTemplates()
  } catch (e) {
    console.error('Failed to load templates:', e)
  } finally {
    isLoading.value = false
  }
})

function selectDungeon(template: DungeonTemplate) {
  router.push({
    path: '/character-create',
    query: { dungeonId: template.id.toString() },
  })
}

const difficultyColor: Record<string, string> = {
  '简单': 'text-emerald-400 border-emerald-500/30',
  '普通': 'text-blue-400 border-blue-500/30',
  '困难': 'text-amber-400 border-amber-500/30',
  '噩梦': 'text-rose-400 border-rose-500/30',
}
</script>

<template>
  <div class="min-h-screen bg-slate-900 px-4 py-6 safe-top">
    <!-- 标题 -->
    <div class="mb-6">
      <button @click="router.back()" class="text-gray-500 text-sm mb-2">&larr; 返回</button>
      <h1 class="text-xl font-bold text-gray-100">选择副本</h1>
      <p class="text-sm text-gray-500 mt-1">每次副本都是独一无二的体验</p>
    </div>

    <!-- 加载 -->
    <div v-if="isLoading" class="flex justify-center py-20">
      <div class="w-8 h-8 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- 副本卡片列表 -->
    <div v-else class="space-y-4">
      <div
        v-for="tmpl in templates"
        :key="tmpl.id"
        @click="selectDungeon(tmpl)"
        class="bg-slate-800/70 border border-gray-700/50 rounded-2xl p-5 active:scale-[0.98] transition-transform cursor-pointer"
      >
        <div class="flex items-start justify-between mb-3">
          <h3 class="text-base font-bold text-gray-100">{{ tmpl.name }}</h3>
          <span
            class="text-xs px-2 py-0.5 rounded border"
            :class="difficultyColor[tmpl.difficulty] || 'text-gray-400 border-gray-600'"
          >
            {{ tmpl.difficulty }}
          </span>
        </div>

        <p class="text-sm text-gray-400 mb-3">{{ tmpl.description }}</p>

        <div class="flex items-center gap-2 flex-wrap">
          <span class="text-xs text-indigo-300 bg-indigo-500/10 px-2 py-0.5 rounded">
            {{ tmpl.worldTheme }}
          </span>
          <span class="text-xs text-gray-500">{{ tmpl.timeLimitDays }}天限制</span>
          <span
            v-for="tag in tmpl.tags"
            :key="tag"
            class="text-xs text-gray-500 bg-gray-700/50 px-2 py-0.5 rounded"
          >
            {{ tag }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
