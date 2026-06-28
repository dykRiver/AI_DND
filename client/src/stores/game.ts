import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  GameState,
  NarrativeChunk,
  DiceResult,
  TimeTransition,
  PlayerChoice,
  DangerousActionConfirm,
  SystemMessage,
  WorldInfo,
  InventoryUpdate,
  SideQuestUpdate,
  SessionEndData,
} from '@/types/game'

// ========== localStorage 持久化辅助 ==========
const STORAGE_KEY = 'game_session'

interface PersistedState {
  sessionId: string
  worldInfo: WorldInfo | null
  gameState: GameState
}

function loadPersistedState(): PersistedState | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    return JSON.parse(raw) as PersistedState
  } catch {
    return null
  }
}

function savePersistedState(state: PersistedState) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state))
  } catch {
    // 静默忽略存储异常
  }
}

function clearPersistedState() {
  localStorage.removeItem(STORAGE_KEY)
}

export const useGameStore = defineStore('game', () => {
  // 从 localStorage 恢复持久化状态
  const persisted = loadPersistedState()

  // 会话
  const sessionId = ref<string>(persisted?.sessionId ?? '')
  const isInSession = computed(() => !!sessionId.value)

  // 游戏状态
  const defaultGameState: GameState = {
    currentHp: 100,
    maxHp: 100,
    hpPercent: 100,
    status: '正常',
    currentDay: 1,
    currentSegment: '清晨',
    tensionLevel: 1,
    isFatigued: false,
    isInCombat: false,
  }
  const gameState = ref<GameState>(persisted?.gameState ?? { ...defaultGameState })

  // 叙事流
  const narrativeChunks = ref<NarrativeChunk[]>([])
  const isTyping = ref(false)

  // 骰子
  const latestDiceResult = ref<DiceResult | null>(null)
  const showDice = ref(false)

  // 时段
  const latestTimeTransition = ref<TimeTransition | null>(null)
  const showTimeTransition = ref(false)

  // 选择
  const currentChoice = ref<PlayerChoice | null>(null)
  const showChoice = ref(false)

  // 危险动作确认
  const dangerousAction = ref<DangerousActionConfirm | null>(null)
  const showDangerConfirm = ref(false)

  // 系统消息
  const systemMessages = ref<SystemMessage[]>([])

  // 加载状态
  const isLoading = ref(false)
  const loadingText = ref('世界推演中...')

  // 输入状态
  const isInputDisabled = ref(false)

  // ★ 世界信息（背景+主线任务）
  const worldInfo = ref<WorldInfo | null>(persisted?.worldInfo ?? null)
  const showWorldInfo = ref(false)

  // ★ 背包状态
  const backpackUpdate = ref<InventoryUpdate | null>(null)
  const showBackpack = ref(false)

  // ★ 成人模式开关
  const isAdultMode = ref(false)

  // ★ 结算数据
  const settlementData = ref<SessionEndData | null>(null)
  const showSettlement = ref(false)

  function toggleAdultMode() {
    isAdultMode.value = !isAdultMode.value
  }

  // ========== 持久化辅助 ==========
  function persist() {
    savePersistedState({
      sessionId: sessionId.value,
      worldInfo: worldInfo.value,
      gameState: gameState.value,
    })
  }

  // ========== Actions ==========
  function setSession(id: string) {
    sessionId.value = id
    // 不清空 narrativeChunks：开场叙事在 DungeonReady 之前已推送，需保留
    latestDiceResult.value = null
    isInputDisabled.value = false
    persist()
  }

  function appendNarrative(chunk: NarrativeChunk) {
    narrativeChunks.value.push(chunk)
    // 流式输出开始即消除loading（"世界推演中..."），而非等到最后一个chunk
    if (isLoading.value) {
      isLoading.value = false
      // 叙事开始推演后隐藏骰子（骰子已在等待期间展示完毕）
      if (showDice.value) {
        setTimeout(() => { showDice.value = false }, 2000)
      }
    }
    if (chunk.isLast) {
      isInputDisabled.value = false
    }
  }

  function updateGameState(state: GameState) {
    gameState.value = state
    persist()
  }

  function setDiceResult(result: DiceResult) {
    latestDiceResult.value = result
    showDice.value = true
    // loading期间不自动隐藏骰子，让玩家在等待期间看到判定详情
    // 叙事开始后（appendNarrative）会自动隐藏
    if (!isLoading.value) {
      setTimeout(() => {
        showDice.value = false
      }, 10000)
    }
  }

  function setTimeTransition(transition: TimeTransition) {
    latestTimeTransition.value = transition
    showTimeTransition.value = true
    setTimeout(() => {
      showTimeTransition.value = false
    }, 4000)
  }

  function setChoice(choice: PlayerChoice) {
    currentChoice.value = choice
    showChoice.value = true
  }

  function clearChoice() {
    currentChoice.value = null
    showChoice.value = false
  }

  function setDangerousAction(action: DangerousActionConfirm) {
    dangerousAction.value = action
    showDangerConfirm.value = true
  }

  function clearDangerousAction() {
    dangerousAction.value = null
    showDangerConfirm.value = false
  }

  function addSystemMessage(msg: SystemMessage) {
    systemMessages.value.push(msg)
    setTimeout(() => {
      systemMessages.value.shift()
    }, 5000)
  }

  function setLoading(loading: boolean, text?: string) {
    isLoading.value = loading
    if (text) loadingText.value = text
  }

  function disableInput() {
    isInputDisabled.value = true
  }

  function enableInput() {
    isInputDisabled.value = false
  }

  function setWorldInfo(info: WorldInfo) {
    worldInfo.value = info
    showWorldInfo.value = true
    persist()
  }

  function toggleWorldInfo() {
    showWorldInfo.value = !showWorldInfo.value
  }

  function updateBackpack(data: InventoryUpdate) {
    backpackUpdate.value = data
  }

  function toggleBackpack() {
    showBackpack.value = !showBackpack.value
  }

  /// 更新支线任务完成状态（导演AI标记完成时推送）
  function updateSideQuestStatus(update: SideQuestUpdate) {
    if (!worldInfo.value) return

    // 追加新解锁的隐藏支线（去重）
    if (update.unlockedSideQuests && update.unlockedSideQuests.length > 0) {
      const existingNames = new Set(worldInfo.value.sideQuests.map(sq => sq.name))
      for (const unlocked of update.unlockedSideQuests) {
        if (!existingNames.has(unlocked.name)) {
          worldInfo.value.sideQuests.push(unlocked)
        }
      }
    }

    // 标记已完成
    if (update.completedSideQuests) {
      const completed = new Set(update.completedSideQuests)
      for (const sq of worldInfo.value.sideQuests) {
        if (completed.has(sq.name)) {
          sq.isCompleted = true
        }
      }
    }

    persist()
  }

  function setSettlementData(data: SessionEndData) {
    settlementData.value = data
    showSettlement.value = true
  }

  function clearSession() {
    sessionId.value = ''
    narrativeChunks.value = []
    latestDiceResult.value = null
    latestTimeTransition.value = null
    currentChoice.value = null
    dangerousAction.value = null
    systemMessages.value = []
    worldInfo.value = null
    showWorldInfo.value = false
    isLoading.value = false
    isInputDisabled.value = false
    clearPersistedState()
  }

  /// 清除叙事历史（用于“重新开始”，保留 sessionId 和 worldInfo）
  function clearNarrativeHistory() {
    narrativeChunks.value = []
    latestDiceResult.value = null
    showDice.value = false
    latestTimeTransition.value = null
    showTimeTransition.value = false
    currentChoice.value = null
    showChoice.value = false
    dangerousAction.value = null
    showDangerConfirm.value = false
    systemMessages.value = []
    isInputDisabled.value = false
    isLoading.value = false
  }

  /// 从服务端恢复会话上下文（断线续玩专用，不清空叙事历史）
  function restoreFromServer(data: {
    sessionId: string
    worldInfo: WorldInfo
    gameState: GameState
  }) {
    sessionId.value = data.sessionId
    worldInfo.value = data.worldInfo
    gameState.value = data.gameState
    showWorldInfo.value = true
    isInputDisabled.value = false
    isLoading.value = false
    persist()
  }

  return {
    sessionId,
    isInSession,
    gameState,
    narrativeChunks,
    isTyping,
    latestDiceResult,
    showDice,
    latestTimeTransition,
    showTimeTransition,
    currentChoice,
    showChoice,
    dangerousAction,
    showDangerConfirm,
    systemMessages,
    isLoading,
    loadingText,
    isInputDisabled,
    worldInfo,
    showWorldInfo,
    backpackUpdate,
    showBackpack,
    isAdultMode,
    settlementData,
    showSettlement,
    toggleAdultMode,
    setSession,
    appendNarrative,
    updateGameState,
    setDiceResult,
    setTimeTransition,
    setChoice,
    clearChoice,
    setDangerousAction,
    clearDangerousAction,
    addSystemMessage,
    setLoading,
    disableInput,
    enableInput,
    setWorldInfo,
    toggleWorldInfo,
    updateBackpack,
    toggleBackpack,
    updateSideQuestStatus,
    setSettlementData,
    clearSession,
    clearNarrativeHistory,
    restoreFromServer,
  }
})
