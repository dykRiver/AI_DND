<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useGameSession } from '@/composables/useGameSession'
import { useGameStore } from '@/stores/game'
import type { CharacterCreateInput } from '@/types/game'
import LoadingOverlay from '@/components/LoadingOverlay.vue'

const router = useRouter()
const route = useRoute()
const { startDungeon, isStarting } = useGameSession()
const gameStore = useGameStore()

// 进入页面时清除可能残留的loading状态
onMounted(() => {
  gameStore.setLoading(false)
})

const dungeonId = Number(route.query.dungeonId) || 0
const characterName = ref('')

// 六维属性 (点买法, 总27点, 每项8-15)
const attributes = ref({
  strength: 10,
  dexterity: 10,
  constitution: 10,
  intelligence: 10,
  wisdom: 10,
  charisma: 10,
})

const pointsUsed = computed(() => {
  return Object.values(attributes.value).reduce((sum, val) => sum + getPointCost(val), 0)
})

const pointsRemaining = computed(() => 27 - pointsUsed.value)

// 点买费用表
function getPointCost(value: number): number {
  if (value <= 8) return 0
  if (value <= 13) return value - 8
  if (value === 14) return 7
  if (value === 15) return 9
  return 9
}

function increase(attr: string) {
  const key = attr as keyof typeof attributes.value
  if (attributes.value[key] >= 15) return
  const newVal = attributes.value[key] + 1
  const costDiff = getPointCost(newVal) - getPointCost(attributes.value[key])
  if (pointsRemaining.value >= costDiff) {
    attributes.value[key] = newVal
  }
}

function decrease(attr: string) {
  const key = attr as keyof typeof attributes.value
  if (attributes.value[key] <= 8) return
  attributes.value[key]--
}

// HP预览: 基础10 + 体质调整值
const hpPreview = computed(() => {
  const conMod = Math.floor((attributes.value.constitution - 10) / 2)
  return 10 + conMod
})

const attrLabels: Record<string, string> = {
  strength: '力量',
  dexterity: '敏捷',
  constitution: '体质',
  intelligence: '智力',
  wisdom: '感知',
  charisma: '魅力',
}

async function handleStart() {
  if (!characterName.value.trim()) return
  const input: CharacterCreateInput = {
    dungeonTemplateId: dungeonId,
    characterName: characterName.value.trim(),
    ...attributes.value,
  }
  await startDungeon(input)
}
</script>

<template>
  <div class="min-h-screen bg-slate-900 px-4 py-6 safe-top">
    <div class="mb-6">
      <button @click="router.back()" class="text-gray-500 text-sm mb-2">&larr; 返回</button>
      <h1 class="text-xl font-bold text-gray-100">创建角色</h1>
    </div>

    <!-- 姓名 -->
    <div class="mb-6">
      <label class="text-sm text-gray-400 mb-2 block">角色名</label>
      <input
        v-model="characterName"
        type="text"
        placeholder="输入角色名..."
        class="w-full px-4 py-3 bg-slate-800 border border-gray-700/50 rounded-xl text-gray-100 placeholder-gray-500 focus:outline-none focus:border-indigo-500/70"
      />
    </div>

    <!-- 属性分配 -->
    <div class="mb-6">
      <div class="flex items-center justify-between mb-3">
        <span class="text-sm text-gray-400">属性分配</span>
        <span class="text-xs" :class="pointsRemaining >= 0 ? 'text-indigo-400' : 'text-rose-400'">
          剩余点数: {{ pointsRemaining }}
        </span>
      </div>

      <div class="space-y-3">
        <div
          v-for="(val, key) in attributes"
          :key="key"
          class="flex items-center justify-between bg-slate-800/50 rounded-xl px-4 py-2.5"
        >
          <span class="text-sm text-gray-300 w-12">{{ attrLabels[key] }}</span>
          <div class="flex items-center gap-3">
            <button
              @click="decrease(key)"
              :disabled="val <= 8"
              class="w-7 h-7 rounded-lg bg-slate-700 text-gray-300 flex items-center justify-center disabled:opacity-30 disabled:cursor-not-allowed hover:bg-slate-600"
            >-</button>
            <span class="text-lg font-bold text-gray-100 w-8 text-center">{{ val }}</span>
            <button
              @click="increase(key)"
              :disabled="val >= 15 || pointsRemaining <= 0"
              class="w-7 h-7 rounded-lg bg-slate-700 text-gray-300 flex items-center justify-center disabled:opacity-30 disabled:cursor-not-allowed hover:bg-slate-600"
            >+</button>
          </div>
          <span class="text-xs text-gray-500 w-10 text-right">
            修正{{ Math.floor((val - 10) / 2) >= 0 ? '+' : '' }}{{ Math.floor((val - 10) / 2) }}
          </span>
        </div>
      </div>
    </div>

    <!-- HP预览 -->
    <div class="mb-8 bg-slate-800/50 rounded-xl px-4 py-3">
      <div class="flex items-center justify-between">
        <span class="text-sm text-gray-400">预计HP</span>
        <span class="text-lg font-bold text-emerald-400">{{ hpPreview }}</span>
      </div>
    </div>

    <!-- 进入按钮 -->
    <button
      @click="handleStart"
      :disabled="!characterName.trim() || pointsRemaining < 0 || isStarting"
      class="w-full py-4 rounded-2xl bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white font-bold text-lg shadow-lg shadow-indigo-500/25 transition-all active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed"
    >
      {{ isStarting ? '创建中...' : '进入副本' }}
    </button>

    <!-- 副本生成Loading遮罩 -->
    <LoadingOverlay :show="gameStore.isLoading" :text="gameStore.loadingText" />
  </div>
</template>
