<script setup lang="ts">
import { ref, watch, nextTick, onMounted } from 'vue'
import type { NarrativeChunk } from '@/types/game'
import DiceAnimation from './DiceAnimation.vue'
import TimeTransition from './TimeTransition.vue'
import { useGameStore } from '@/stores/game'

const gameStore = useGameStore()
const scrollContainer = ref<HTMLElement | null>(null)
const displayedTexts = ref<{ text: string; done: boolean; chunk: NarrativeChunk }[]>([])
const currentTypingIndex = ref(-1)
const currentTypingText = ref('')

// 当前正在流式接收的叙事项索引（-1表示无活跃流）
const activeStreamIndex = ref(-1)

// 滚动到底部（仅用于初始加载恢复）
function scrollToBottom() {
  nextTick(() => {
    if (scrollContainer.value) {
      scrollContainer.value.scrollTop = scrollContainer.value.scrollHeight
    }
  })
}

// 打字机效果（仅用于非流式场景，如场景转换、固定文本）
function typeText(text: string, index: number) {
  currentTypingIndex.value = index
  currentTypingText.value = ''
  let charIndex = 0
  const speed = 30 // ms per char

  const timer = setInterval(() => {
    if (charIndex < text.length) {
      currentTypingText.value += text[charIndex]
      charIndex++
    } else {
      clearInterval(timer)
      displayedTexts.value[index].done = true
      currentTypingIndex.value = -1
    }
  }, speed)
}

// 监听新叙事：流式场景将多个chunk合并到同一显示项
watch(
  () => gameStore.narrativeChunks.length,
  (newLen) => {
    if (newLen === 0) return
    const chunk = gameStore.narrativeChunks[newLen - 1]

    // 有活跃的流式项 → 追加文本到该项
    if (activeStreamIndex.value >= 0 && activeStreamIndex.value < displayedTexts.value.length) {
      displayedTexts.value[activeStreamIndex.value].text += chunk.text

      if (chunk.isLast) {
        displayedTexts.value[activeStreamIndex.value].done = true
        activeStreamIndex.value = -1
      }
      return
    }

    // 无活跃流式项 → 创建新显示项
    const index = displayedTexts.value.length
    displayedTexts.value.push({ text: chunk.text, done: chunk.isLast, chunk })

    if (!chunk.isLast) {
      // 流式场景：直接显示文本，由AI流式节奏提供视觉节奏感
      activeStreamIndex.value = index
    } else {
      // 单块完整叙事（如固定文本）→ 打字机效果
      nextTick(() => typeText(chunk.text, index))
    }
  }
)

onMounted(() => {
  // 恢复已有的叙事 —— 按 isLast 边界将连续 chunk 合并为同一显示项
  const chunks = gameStore.narrativeChunks
  let buffer = ''
  let bufferChunk: NarrativeChunk | null = null

  for (let i = 0; i < chunks.length; i++) {
    const chunk = chunks[i]

    // scene_transition 始终独立显示，先 flush 缓冲区
    if (chunk.chunkType === 'scene_transition') {
      if (bufferChunk) {
        displayedTexts.value.push({ text: buffer, done: true, chunk: bufferChunk })
        buffer = ''
        bufferChunk = null
      }
      displayedTexts.value.push({ text: chunk.text, done: true, chunk })
      continue
    }

    // 累积文本到缓冲区
    buffer += chunk.text
    if (!bufferChunk) bufferChunk = chunk

    // isLast 表示一个完整叙事段的结束，flush 缓冲区
    if (chunk.isLast) {
      displayedTexts.value.push({ text: buffer, done: true, chunk: bufferChunk })
      buffer = ''
      bufferChunk = null
    }
  }

  // 处理未以 isLast 结尾的残留缓冲（如中途断开重连）
  if (bufferChunk) {
    displayedTexts.value.push({ text: buffer, done: true, chunk: bufferChunk })
  }

  scrollToBottom()
})
</script>

<template>
  <div ref="scrollContainer" class="flex-1 overflow-y-auto custom-scrollbar px-4 py-3 space-y-4">
    <!-- 历史叙事 -->
    <div
      v-for="(item, index) in displayedTexts"
      :key="index"
      class="fade-in"
    >
      <!-- 场景转换 -->
      <div
        v-if="item.chunk.chunkType === 'scene_transition'"
        class="flex items-center gap-3 my-6"
      >
        <div class="flex-1 h-px bg-gradient-to-r from-transparent via-indigo-500/50 to-transparent"></div>
        <span class="text-indigo-400 text-xs tracking-widest uppercase">场景转换</span>
        <div class="flex-1 h-px bg-gradient-to-r from-transparent via-indigo-500/50 to-transparent"></div>
      </div>

      <!-- 叙事/行动结果文字 -->
      <p
        v-else
        class="narrative-text text-gray-200 leading-relaxed"
        :class="{
          'text-amber-200/90': item.chunk.chunkType === 'action_result',
          'typewriter-cursor': index === currentTypingIndex,
        }"
      >
        {{ index === currentTypingIndex ? currentTypingText : item.text }}
      </p>
    </div>

    <!-- 骰子结果 (行内) -->
    <DiceAnimation
      v-if="gameStore.showDice && gameStore.latestDiceResult"
      :result="gameStore.latestDiceResult"
    />

    <!-- 时段转换 -->
    <TimeTransition
      v-if="gameStore.showTimeTransition && gameStore.latestTimeTransition"
      :transition="gameStore.latestTimeTransition"
    />
  </div>
</template>
