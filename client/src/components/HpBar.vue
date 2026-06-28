<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  current: number
  max: number
  percent: number
}>()

const barColor = computed(() => {
  if (props.percent > 60) return 'bg-emerald-500'
  if (props.percent > 30) return 'bg-amber-500'
  return 'bg-rose-500'
})

const statusText = computed(() => {
  if (props.percent > 75) return ''
  if (props.percent > 50) return '轻伤'
  if (props.percent > 25) return '重伤'
  return '濒死'
})
</script>

<template>
  <div class="flex items-center gap-2">
    <div class="flex-1 h-2 bg-gray-700/80 rounded-full overflow-hidden">
      <div
        :class="barColor"
        class="h-full rounded-full hp-bar-transition"
        :style="{ width: `${percent}%` }"
      ></div>
    </div>
    <span class="text-xs text-gray-400 whitespace-nowrap min-w-[3rem] text-right">
      {{ current }}/{{ max }}
    </span>
    <span v-if="statusText" class="text-xs text-rose-400">{{ statusText }}</span>
  </div>
</template>
