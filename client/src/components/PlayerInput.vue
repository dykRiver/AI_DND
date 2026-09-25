<script setup lang="ts">
import { ref, computed } from 'vue'

const emit = defineEmits<{
  send: [text: string]
  'clear-goal': []
}>()

const props = defineProps<{
  disabled: boolean
  /// 当前目标文本（由父组件从 gameStore.currentGoal 传入）；非空时渲染目标chip+清空按钮
  currentGoal?: string
}>()

// 自由输入框新语义：提交中长期目标声明（非本轮行动），前端限100字
const MAX_LEN = 100
const inputText = ref('')
const remaining = computed(() => MAX_LEN - inputText.value.length)
const hasGoal = computed(() => !!(props.currentGoal && props.currentGoal.trim()))

function handleInput(e: Event) {
  const target = e.target as HTMLTextAreaElement
  if (target.value.length > MAX_LEN) {
    target.value = target.value.slice(0, MAX_LEN)
    inputText.value = target.value
  }
}

function handleSend() {
  if (!inputText.value.trim()) return
  emit('send', inputText.value.trim())
  inputText.value = ''
}

function handleClearGoal() {
  emit('clear-goal')
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault()
    handleSend()
  }
}
</script>

<template>
  <div class="border-t border-gray-700/50 bg-slate-900/95 backdrop-blur px-4 py-3 safe-bottom">
    <!-- 目标chip：当前有目标时显示，点击×清空 -->
    <div
      v-if="hasGoal"
      class="mb-2 flex items-center gap-2 px-3 py-1.5 rounded-lg bg-indigo-500/10 border border-indigo-500/30 max-w-full"
    >
      <span class="text-indigo-400 text-xs flex-shrink-0">🎯 当前目标</span>
      <span
        class="text-indigo-200/90 text-xs truncate flex-1 min-w-0"
        :title="currentGoal"
      >{{ currentGoal }}</span>
      <button
        @click="handleClearGoal"
        :disabled="disabled"
        class="flex-shrink-0 w-5 h-5 flex items-center justify-center rounded-full text-indigo-300/70 hover:text-rose-300 hover:bg-rose-500/20 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        title="清空目标"
        aria-label="清空目标"
      >
        <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"/>
        </svg>
      </button>
    </div>

    <div class="flex items-end gap-2">
      <div class="flex-1 relative">
        <textarea
          v-model="inputText"
          :disabled="disabled"
          :maxlength="MAX_LEN"
          @input="handleInput"
          @keydown="handleKeydown"
          placeholder="表达你的意图或目标（下一轮选项会围绕它展开）..."
          rows="1"
          class="w-full resize-none bg-slate-800 border border-gray-600/50 rounded-xl px-4 py-2.5 pr-14 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-indigo-500/70 focus:ring-1 focus:ring-indigo-500/30 disabled:opacity-50 disabled:cursor-not-allowed text-sm leading-relaxed max-h-24 overflow-y-auto"
        ></textarea>
        <span
          class="absolute right-3 bottom-2 text-[10px] tabular-nums pointer-events-none"
          :class="remaining <= 20 ? 'text-amber-400/80' : 'text-gray-500'"
        >{{ remaining }}</span>
      </div>
      <button
        @click="handleSend"
        :disabled="disabled || !inputText.trim()"
        class="flex-shrink-0 w-10 h-10 flex items-center justify-center rounded-xl bg-indigo-600 hover:bg-indigo-500 disabled:bg-gray-700 disabled:cursor-not-allowed transition-colors"
      >
        <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19V5m0 0l-7 7m7-7l7 7" transform="rotate(45 12 12)"/>
        </svg>
      </button>
    </div>
  </div>
</template>
