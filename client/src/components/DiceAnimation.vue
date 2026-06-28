<script setup lang="ts">
import { ref } from 'vue'
import type { DiceResult } from '@/types/game'

const props = defineProps<{
  result: DiceResult
}>()

const isRolling = ref(true)
const displayNumber = ref(0)

// 骰子滚动数字动画
function animateRoll() {
  let count = 0
  const maxFrames = 12
  const timer = setInterval(() => {
    displayNumber.value = Math.floor(Math.random() * 20) + 1
    count++
    if (count >= maxFrames) {
      clearInterval(timer)
      displayNumber.value = props.result.d20Roll
      isRolling.value = false
    }
  }, 80)
}

animateRoll()
</script>

<template>
  <div class="my-3 fade-in">
    <div
      class="inline-flex items-center gap-3 px-4 py-2.5 rounded-xl border"
      :class="{
        'border-emerald-500/50 bg-emerald-950/30': result.isSuccess && !result.isNatural20,
        'border-rose-500/50 bg-rose-950/30': !result.isSuccess && !result.isNatural1,
        'border-amber-400/70 bg-amber-950/30 shadow-lg shadow-amber-500/20': result.isNatural20,
        'border-red-700/70 bg-red-950/40': result.isNatural1,
      }"
    >
      <!-- d20 图标 -->
      <div
        class="w-10 h-10 flex items-center justify-center rounded-lg font-bold text-lg"
        :class="{
          'dice-rolling': isRolling,
          'bg-emerald-600/30 text-emerald-300': result.isSuccess && !result.isNatural20,
          'bg-rose-600/30 text-rose-300': !result.isSuccess && !result.isNatural1,
          'bg-amber-500/40 text-amber-200': result.isNatural20,
          'bg-red-800/40 text-red-300': result.isNatural1,
        }"
      >
        {{ displayNumber }}
      </div>

      <!-- 信息 -->
      <div class="text-sm">
        <div class="text-gray-300">
          <span class="font-medium">{{ result.skillName }}</span>
          <span class="text-gray-500 ml-1">
            ({{ result.d20Roll }}{{ result.modifier >= 0 ? '+' : '' }}{{ result.modifier }}={{ result.total }} vs DC{{ result.dc }})
          </span>
        </div>
        <div
          class="text-xs mt-0.5 font-medium"
          :class="{
            'text-emerald-400': result.isSuccess,
            'text-rose-400': !result.isSuccess,
            'text-amber-300': result.isNatural20,
          }"
        >
          {{ result.isNatural20 ? '大成功！' : result.isNatural1 ? '大失败...' : result.isSuccess ? '成功' : '失败' }}
        </div>
      </div>
    </div>
  </div>
</template>
