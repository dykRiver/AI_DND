<script setup lang="ts">
import { ref } from 'vue'

const emit = defineEmits<{
  send: [text: string]
}>()

defineProps<{
  disabled: boolean
}>()

const inputText = ref('')

function handleSend() {
  if (!inputText.value.trim()) return
  emit('send', inputText.value.trim())
  inputText.value = ''
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
    <div class="flex items-end gap-2">
      <textarea
        v-model="inputText"
        :disabled="disabled"
        @keydown="handleKeydown"
        placeholder="描述你的行动..."
        rows="1"
        class="flex-1 resize-none bg-slate-800 border border-gray-600/50 rounded-xl px-4 py-2.5 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-indigo-500/70 focus:ring-1 focus:ring-indigo-500/30 disabled:opacity-50 disabled:cursor-not-allowed text-sm leading-relaxed max-h-24 overflow-y-auto"
      ></textarea>
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
