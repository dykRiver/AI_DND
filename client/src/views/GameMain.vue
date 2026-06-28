<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '@/stores/game'
import { useGameSession } from '@/composables/useGameSession'
import { reinitCharacter } from '@/api/game'
import NarrativeDisplay from '@/components/NarrativeDisplay.vue'
import PlayerInput from '@/components/PlayerInput.vue'
import StatusPanel from '@/components/StatusPanel.vue'
import LoadingOverlay from '@/components/LoadingOverlay.vue'
import WorldInfoPanel from '@/components/WorldInfoPanel.vue'
import BackpackPanel from '@/components/BackpackPanel.vue'

const router = useRouter()
const gameStore = useGameStore()
const { sendAction, handleDangerConfirm, abandon, suspend, restart, initConnection } = useGameSession()

const showExitDialog = ref(false)

onMounted(async () => {
  if (!gameStore.sessionId) {
    router.push('/')
    return
  }
  await initConnection()
})

function handleSend(text: string) {
  sendAction(text)
}

function confirmDanger(confirmed: boolean) {
  if (gameStore.dangerousAction) {
    handleDangerConfirm(gameStore.dangerousAction.actionId, confirmed)
  }
}

async function handleAbandon() {
  showExitDialog.value = false
  await abandon()
}

async function handleSuspend() {
  showExitDialog.value = false
  await suspend()
}

// [测试] 重建角色信息（修复历史数据 + 重建技能）
const isReiniting = ref(false)
async function handleReinit() {
  if (isReiniting.value || !gameStore.sessionId) return
  isReiniting.value = true
  try {
    const msg = await reinitCharacter(gameStore.sessionId)
    gameStore.addSystemMessage({ type: 'success', content: msg })
  } catch (e: any) {
    gameStore.addSystemMessage({ type: 'error', content: `重建失败: ${e?.message || e}` })
  } finally {
    isReiniting.value = false
  }
}

// [测试] 重新开始（保留世界/副本，清除历史记录）
const showRestartDialog = ref(false)
const isRestarting = ref(false)
async function handleRestartConfirm() {
  showRestartDialog.value = false
  if (isRestarting.value || !gameStore.sessionId) return
  isRestarting.value = true
  try {
    await restart()
  } catch (e: any) {
    gameStore.addSystemMessage({ type: 'error', content: `重置失败: ${e?.message || e}` })
  } finally {
    isRestarting.value = false
  }
}
</script>

<template>
  <div class="h-screen flex flex-col bg-slate-900 overflow-hidden">
    <!-- 顶部状态栏 -->
    <StatusPanel
      :is-reiniting="isReiniting"
      :is-restarting="isRestarting"
      :dungeon-name="gameStore.worldInfo?.dungeonName"
      @reinit="handleReinit"
      @restart="showRestartDialog = true"
      @exit="showExitDialog = true"
      @toggle-backpack="gameStore.toggleBackpack()"
      @toggle-world-info="gameStore.toggleWorldInfo()"
    />

    <!-- ★ 世界信息面板 -->
    <WorldInfoPanel />

    <!-- ★ 背包面板 -->
    <BackpackPanel v-if="gameStore.showBackpack" @close="gameStore.toggleBackpack()" />

    <!-- 叙事区 -->
    <NarrativeDisplay />

    <!-- 选择面板 -->
    <div
      v-if="gameStore.showChoice && gameStore.currentChoice"
      class="border-t border-gray-700/50 bg-slate-800/95 backdrop-blur px-4 py-3"
    >
      <p class="text-sm text-gray-300 mb-3">{{ gameStore.currentChoice.prompt }}</p>
      <div class="flex flex-wrap gap-2">
        <button
          v-for="(option, idx) in gameStore.currentChoice.choices"
          :key="idx"
          @click="sendAction(option)"
          class="px-4 py-2 rounded-lg bg-indigo-600/20 border border-indigo-500/40 text-indigo-300 text-sm hover:bg-indigo-600/30 transition-colors"
        >
          {{ option }}
        </button>
      </div>
    </div>

    <!-- 危险行动确认 -->
    <div
      v-if="gameStore.showDangerConfirm && gameStore.dangerousAction"
      class="fixed inset-0 z-40 flex items-center justify-center bg-black/60 backdrop-blur-sm px-6"
    >
      <div class="bg-slate-800 border border-rose-500/30 rounded-2xl p-6 max-w-sm w-full">
        <h3 class="text-rose-400 font-bold mb-2">危险行动确认</h3>
        <p class="text-gray-300 text-sm mb-2">{{ gameStore.dangerousAction.description }}</p>
        <p class="text-rose-300/80 text-xs mb-4">警告: {{ gameStore.dangerousAction.warning }}</p>
        <div class="flex gap-3">
          <button
            @click="confirmDanger(false)"
            class="flex-1 py-2.5 rounded-xl bg-slate-700 text-gray-300 text-sm hover:bg-slate-600"
          >
            取消
          </button>
          <button
            @click="confirmDanger(true)"
            class="flex-1 py-2.5 rounded-xl bg-rose-600 text-white text-sm hover:bg-rose-500"
          >
            确认执行
          </button>
        </div>
      </div>
    </div>

    <!-- 玩家输入区 -->
    <PlayerInput
      :disabled="gameStore.isInputDisabled || gameStore.isLoading"
      @send="handleSend"
    />

    <!-- 加载遮罩（含骰子判定详情展示） -->
    <LoadingOverlay
      :show="gameStore.isLoading"
      :text="gameStore.loadingText"
      :dice-result="gameStore.latestDiceResult"
      :show-dice="gameStore.showDice"
    />

    <!-- 系统消息浮层 -->
    <div class="fixed top-16 left-4 right-4 z-50 space-y-2 pointer-events-none">
      <div
        v-for="(msg, idx) in gameStore.systemMessages"
        :key="idx"
        class="fade-in px-4 py-2 rounded-xl text-sm backdrop-blur pointer-events-auto"
        :class="{
          'bg-blue-500/20 border border-blue-500/30 text-blue-300': msg.type === 'info',
          'bg-amber-500/20 border border-amber-500/30 text-amber-300': msg.type === 'warning',
          'bg-rose-500/20 border border-rose-500/30 text-rose-300': msg.type === 'error',
          'bg-emerald-500/20 border border-emerald-500/30 text-emerald-300': msg.type === 'success',
        }"
      >
        {{ msg.content }}
      </div>
    </div>

    <!-- 重新开始确认弹窗 -->
    <Transition name="fade">
      <div
        v-if="showRestartDialog"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm px-6"
      >
        <div class="bg-slate-800 border border-cyan-500/30 rounded-2xl p-6 max-w-sm w-full">
          <h3 class="text-cyan-400 font-bold mb-2">重新开始</h3>
          <p class="text-gray-300 text-sm mb-2">将清除所有历史记录和游戏进度，但保留当前世界设定和副本模板。</p>
          <p class="text-cyan-300/80 text-xs mb-4">提示：角色属性、背包道具、NPC交互历史均会被重置。</p>
          <div class="flex gap-3">
            <button
              @click="showRestartDialog = false"
              class="flex-1 py-2.5 rounded-xl bg-slate-700 text-gray-300 text-sm hover:bg-slate-600"
            >
              取消
            </button>
            <button
              @click="handleRestartConfirm"
              class="flex-1 py-2.5 rounded-xl bg-cyan-600 text-white text-sm hover:bg-cyan-500"
            >
              确认重置
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- 退出选择弹窗 -->
    <Transition name="fade">
      <div
        v-if="showExitDialog"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm px-6"
      >
        <div class="bg-slate-800 border border-gray-700/50 rounded-2xl p-6 max-w-sm w-full">
          <h3 class="text-gray-100 font-bold text-lg mb-2">退出副本</h3>
          <p class="text-gray-400 text-sm mb-6">你希望如何处理当前副本？</p>
          <div class="space-y-3">
            <button
              @click="handleSuspend"
              class="w-full py-3 rounded-xl bg-indigo-600/20 border border-indigo-500/40 text-indigo-300 text-sm hover:bg-indigo-600/30 transition-colors"
            >
              <span class="font-medium">挂起副本</span>
              <span class="block text-xs text-indigo-400/70 mt-1">保存进度，下次可以继续</span>
            </button>
            <button
              @click="handleAbandon"
              class="w-full py-3 rounded-xl bg-rose-600/20 border border-rose-500/40 text-rose-300 text-sm hover:bg-rose-600/30 transition-colors"
            >
              <span class="font-medium">结束副本</span>
              <span class="block text-xs text-rose-400/70 mt-1">彻底结束，进入结算</span>
            </button>
            <button
              @click="showExitDialog = false"
              class="w-full py-2.5 rounded-xl bg-slate-700 text-gray-400 text-sm hover:bg-slate-600 transition-colors"
            >
              取消
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
