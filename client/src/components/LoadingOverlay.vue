<script setup lang="ts">
import type { DiceResult } from '@/types/game'

defineProps<{
  show: boolean
  text?: string
  diceResult?: DiceResult | null
  showDice?: boolean
}>()

function getSuccessLabel(result: DiceResult): string {
  if (result.isNatural20) return '大成功'
  if (result.isNatural1) return '大失败'
  return result.isSuccess ? '成功' : '失败'
}

function getSuccessColor(result: DiceResult): string {
  if (result.isNatural20) return 'text-amber-300'
  if (result.isNatural1) return 'text-rose-400'
  return result.isSuccess ? 'text-emerald-400' : 'text-rose-400'
}
</script>

<template>
  <Transition name="fade">
    <div v-if="show" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/80 backdrop-blur-sm">
      <div class="text-center max-w-sm px-6">
        <!-- 加载动画 -->
        <div class="relative w-16 h-16 mx-auto mb-4">
          <div class="absolute inset-0 border-2 border-indigo-500/30 rounded-full"></div>
          <div class="absolute inset-0 border-2 border-transparent border-t-indigo-500 rounded-full animate-spin"></div>
          <div class="absolute inset-2 border-2 border-transparent border-b-purple-500 rounded-full animate-spin" style="animation-direction: reverse; animation-duration: 1.5s;"></div>
        </div>
        <p class="text-gray-300 text-sm animate-pulse mb-4">{{ text || '世界推演中...' }}</p>

        <!-- 骰子判定详情（loading期间展示，让玩家消磨等待时间） -->
        <Transition name="dice-fade">
          <div v-if="showDice && diceResult" class="mt-4 bg-slate-800/90 border border-indigo-500/30 rounded-2xl p-4 shadow-lg shadow-indigo-500/10">
            <!-- 技能名称 -->
            <div class="text-indigo-300 font-bold text-base mb-3">
              🎲 {{ diceResult.skillName }} 检定
            </div>

            <!-- D20骰子点数（突出显示） -->
            <div class="flex items-center justify-center gap-3 mb-3">
              <div
                class="w-16 h-16 flex items-center justify-center rounded-xl text-2xl font-black"
                :class="[
                  diceResult.isNatural20 ? 'bg-amber-500/20 border-2 border-amber-400 text-amber-300 shadow-lg shadow-amber-500/30' :
                  diceResult.isNatural1 ? 'bg-rose-500/20 border-2 border-rose-400 text-rose-300' :
                  diceResult.isSuccess ? 'bg-emerald-500/15 border-2 border-emerald-400/50 text-emerald-300' :
                  'bg-rose-500/15 border-2 border-rose-400/50 text-rose-300'
                ]"
              >
                {{ diceResult.d20Roll }}
              </div>
              <div class="text-gray-400 text-lg">+</div>
              <div class="text-gray-200 text-xl font-bold">
                {{ diceResult.modifier >= 0 ? '+' : '' }}{{ diceResult.modifier }}
              </div>
              <div class="text-gray-400 text-lg">=</div>
              <div class="text-white text-2xl font-black">{{ diceResult.total }}</div>
            </div>

            <!-- DC对比 -->
            <div class="flex items-center justify-center gap-2 text-sm mb-3">
              <span class="text-gray-400">DC {{ diceResult.dc }}</span>
              <span v-if="diceResult.worldDifficultyModifier !== 0" class="text-gray-500">
                ({{ diceResult.worldDifficultyModifier >= 0 ? '+' : '' }}{{ diceResult.worldDifficultyModifier }})
              </span>
              <span class="text-gray-500">→</span>
              <span class="text-gray-300">有效DC {{ diceResult.effectiveDC }}</span>
            </div>

            <!-- 成功/失败标签 -->
            <div
              class="inline-block px-4 py-1.5 rounded-full font-bold text-sm"
              :class="[
                getSuccessColor(diceResult),
                diceResult.isNatural20 ? 'bg-amber-500/15 border border-amber-400/40' :
                diceResult.isNatural1 ? 'bg-rose-500/15 border border-rose-400/40' :
                diceResult.isSuccess ? 'bg-emerald-500/15 border border-emerald-400/40' :
                'bg-rose-500/15 border border-rose-400/40'
              ]"
            >
              {{ getSuccessLabel(diceResult) }}
            </div>

            <!-- 叙事暗示 -->
            <p v-if="diceResult.narrativeHint" class="mt-3 text-xs text-gray-500 italic">
              {{ diceResult.narrativeHint }}
            </p>
          </div>
        </Transition>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
.dice-fade-enter-active {
  transition: all 0.4s ease-out;
}
.dice-fade-enter-from {
  opacity: 0;
  transform: translateY(10px) scale(0.95);
}
</style>
