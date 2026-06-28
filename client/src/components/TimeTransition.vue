<script setup lang="ts">
import type { TimeTransition as TimeTransitionType } from '@/types/game'

defineProps<{
  transition: TimeTransitionType
}>()

const segmentIcon: Record<string, string> = {
  '清晨': '🌅',
  '上午': '☀️',
  '下午': '🌤️',
  '傍晚': '🌆',
  '夜晚': '🌙',
}
</script>

<template>
  <div class="my-6 fade-in">
    <!-- 分隔线 -->
    <div class="flex items-center gap-3 mb-3">
      <div class="flex-1 h-px bg-gradient-to-r from-transparent via-purple-500/50 to-transparent"></div>
      <div class="flex items-center gap-2 text-purple-300">
        <span class="text-lg">{{ segmentIcon[transition.newSegment] || '⏳' }}</span>
        <span class="text-xs tracking-wider">
          Day {{ transition.newDay }} · {{ transition.newSegment }}
        </span>
      </div>
      <div class="flex-1 h-px bg-gradient-to-r from-transparent via-purple-500/50 to-transparent"></div>
    </div>
    <!-- 转换叙事 -->
    <p class="narrative-text text-purple-200/80 text-sm text-center italic">
      {{ transition.narrative }}
    </p>
  </div>
</template>
