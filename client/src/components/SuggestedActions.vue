<script setup lang="ts">
import { computed } from 'vue'
import { useGameStore } from '@/stores/game'
import { useGameSession } from '@/composables/useGameSession'

const gameStore = useGameStore()
const { selectCachedAction } = useGameSession()

const suggestedActions = computed(() => gameStore.suggestedActions)
const show = computed(() => suggestedActions.value != null && suggestedActions.value.options.length > 0)
const isComputing = computed(() => suggestedActions.value?.isComputing ?? false)

function handleClick(option: { index: number; actionText: string }) {
  // 携带选项文本：缓存过期时服务端以此文本走常规流程，避免执行与按钮不符的行动
  selectCachedAction(option.index, option.actionText)
}

// 不可行选项（预计算判定为无法执行）置灰不可点击
function isDisabled(option: { isFeasible?: boolean }) {
  return isComputing.value || option.isFeasible === false
}
</script>

<template>
  <Transition name="slide-up">
    <div
      v-if="show"
      class="border-t border-gray-700/50 bg-slate-800/95 backdrop-blur px-4 py-3"
    >
      <p class="text-xs text-gray-500 mb-2.5 flex items-center gap-1.5">
        <span>快速行动</span>
        <span v-if="isComputing" class="inline-flex items-center gap-1 text-amber-400/70">
          <svg class="animate-spin h-3 w-3" viewBox="0 0 24 24" fill="none">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
          </svg>
          预计算中...
        </span>
      </p>
      <div class="flex gap-2">
        <button
          v-for="option in suggestedActions?.options ?? []"
          :key="option.index"
          @click="handleClick(option)"
          :disabled="isDisabled(option)"
          class="flex-1 px-3 py-2.5 rounded-lg text-sm text-left transition-all duration-200 border"
          :class="isComputing
            ? 'bg-slate-700/40 border-slate-600/30 text-gray-500 cursor-wait'
            : (option.isFeasible === false
              ? 'bg-slate-700/30 border-slate-600/30 text-gray-500 cursor-not-allowed opacity-60'
              : 'bg-emerald-600/15 border-emerald-500/30 text-emerald-300 hover:bg-emerald-600/25 hover:border-emerald-400/50 cursor-pointer')"
        >
          <span class="block font-medium">{{ option.actionText }}</span>
          <span
            class="block text-xs mt-0.5"
            :class="isComputing ? 'text-gray-600' : (option.isFeasible === false ? 'text-gray-500' : 'text-emerald-400/60')"
          >{{ option.isFeasible === false ? '无法执行' : option.hint }}</span>
        </button>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.slide-up-enter-active {
  transition: all 0.3s ease-out;
}
.slide-up-leave-active {
  transition: all 0.2s ease-in;
}
.slide-up-enter-from,
.slide-up-leave-to {
  opacity: 0;
  transform: translateY(8px);
}
</style>
