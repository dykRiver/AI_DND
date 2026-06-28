import { ref } from 'vue'
import * as signalR from '@microsoft/signalr'
import { useGameStore } from '@/stores/game'
import { useAuthStore } from '@/stores/auth'
import type {
  NarrativeChunk,
  DiceResult,
  GameState,
  TimeTransition,
  PlayerChoice,
  DangerousActionConfirm,
  CharacterCreateInput,
  GeneratingProgress,
  WorldInfo,
  DungeonReady,
  InventoryUpdate,
  SideQuestUpdate,
  SessionEndData,
} from '@/types/game'

// ===== 模块级单例状态（跨组件共享同一个 SignalR 连接） =====
const connection = ref<signalR.HubConnection | null>(null)
const isConnected = ref(false)
const reconnectAttempts = ref(0)
const maxReconnectAttempts = 5
const onDungeonReadyCallback = ref<((data: DungeonReady) => void) | null>(null)
let _initialized = false

export function useSignalR() {
  const gameStore = useGameStore()
  const authStore = useAuthStore()

  function buildConnection() {
    const url = import.meta.env.VITE_SIGNALR_URL

    connection.value = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: () => authStore.token,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount >= maxReconnectAttempts) {
            return null // 停止重连
          }
          return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000)
        },
      })
      .withServerTimeout(300000) // 5分钟超时，副本生成耗时较长
      .withKeepAliveInterval(15000) // 15秒心跳保活
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    registerEventHandlers()
    registerLifecycleHandlers()
    _initialized = true
  }

  function registerLifecycleHandlers() {
    if (!connection.value) return

    connection.value.onreconnecting(() => {
      isConnected.value = false
      gameStore.addSystemMessage({ type: 'warning', content: '连接中断，正在重连...' })
    })

    connection.value.onreconnected(() => {
      isConnected.value = true
      reconnectAttempts.value = 0
      gameStore.addSystemMessage({ type: 'success', content: '连接已恢复' })
    })

    connection.value.onclose(() => {
      isConnected.value = false
      gameStore.addSystemMessage({ type: 'error', content: '连接已断开' })
    })
  }

  function registerEventHandlers() {
    if (!connection.value) return

    // 接收叙事文本
    connection.value.on('ReceiveNarrative', (chunk: NarrativeChunk) => {
      gameStore.appendNarrative(chunk)
    })

    // 接收骰子结果
    connection.value.on('ReceiveDiceResult', (result: DiceResult) => {
      gameStore.setDiceResult(result)
    })

    // 更新游戏状态
    connection.value.on('UpdateGameState', (state: GameState) => {
      gameStore.updateGameState(state)
    })

    // 时段转换
    connection.value.on('SendTimeTransition', (transition: TimeTransition) => {
      gameStore.setTimeTransition(transition)
    })

    // 请求玩家选择
    connection.value.on('RequestPlayerChoice', (choice: PlayerChoice) => {
      gameStore.setChoice(choice)
    })

    // ★ 副本就绪通知（后台生成完成后推送）
    connection.value.on('DungeonReady', (data: DungeonReady) => {
      gameStore.setSession(data.sessionId.toString())
      gameStore.setWorldInfo(data.worldInfo)
      gameStore.updateGameState(data.gameState)
      gameStore.setLoading(false)
      onDungeonReadyCallback.value?.(data)
    })

    // 副本生成进度
    connection.value.on('DungeonGenerating', (progress: GeneratingProgress) => {
      const isLoading = progress.phase !== '完成'
      gameStore.setLoading(isLoading, progress.message || '副本世界生成中...')
    })

    // 会话结束
    connection.value.on('SessionEnded', (data: SessionEndData) => {
      if (data.exitNarrative || data.epilogue || data.comment) {
        gameStore.setSettlementData(data)
      }
      gameStore.addSystemMessage({
        type: 'info',
        content: data.scoreLevel
          ? `副本结束 — 评价: ${data.scoreLevel}`
          : '副本已结束',
      })
      // 由useGameSession处理跳转
    })

    // 系统消息
    connection.value.on('ReceiveSystemMessage', (msg: { type: string; message: string }) => {
      // 输入控制信号：服务端处理完AI后恢复输入
      if (msg.type === 'input_control') {
        if (msg.message === 'enable') {
          gameStore.enableInput()
          gameStore.setLoading(false)
        } else if (msg.message === 'disable') {
          gameStore.disableInput()
        }
        return
      }
      gameStore.addSystemMessage({
        type: msg.type as 'info' | 'warning' | 'error' | 'success',
        content: msg.message,
      })
    })

    // 危险行动确认
    connection.value.on('RequestDangerousActionConfirm', (action: DangerousActionConfirm) => {
      gameStore.setDangerousAction(action)
    })

    // ★ 副本世界信息
    connection.value.on('ReceiveWorldInfo', (info: WorldInfo) => {
      gameStore.setWorldInfo(info)
    })

    // ★ 背包更新推送
    connection.value.on('UpdateInventory', (inventory: InventoryUpdate) => {
      gameStore.updateBackpack(inventory)
    })

    // ★ 支线任务状态更新
    connection.value.on('UpdateSideQuests', (update: SideQuestUpdate) => {
      gameStore.updateSideQuestStatus(update)
    })
  }

  async function connect() {
    if (!_initialized) {
      buildConnection()
    }
    if (isConnected.value) return // 已连接则直接返回
    try {
      await connection.value!.start()
      isConnected.value = true
      reconnectAttempts.value = 0
    } catch (err) {
      console.error('SignalR connection failed:', err)
      isConnected.value = false
      throw err
    }
  }

  async function disconnect() {
    if (connection.value) {
      await connection.value.stop()
      isConnected.value = false
    }
  }

  /// 完全销毁连接（仅用于登出时）
  async function destroy() {
    await disconnect()
    connection.value = null
    isConnected.value = false
    reconnectAttempts.value = 0
    onDungeonReadyCallback.value = null
    _initialized = false
  }

  // ========== 发送方法 ==========
  async function sendPlayerAction(sessionId: string, actionText: string, isAdultMode: boolean = false) {
    if (!connection.value || !isConnected.value) return
    gameStore.disableInput()
    gameStore.setLoading(true, '世界推演中...')
    // fire-and-forget：只发送请求，结果通过推送事件返回
    await connection.value.send('PlayerAction', {
      sessionId: Number(sessionId),
      actionText,
      isAdultMode,
    })
  }

  async function selectDungeon(input: CharacterCreateInput) {
    if (!connection.value || !isConnected.value) return
    gameStore.setLoading(true, '副本世界生成中...')
    // fire-and-forget：只发送请求，不等待结果
    // 结果通过 DungeonReady 事件推送回来
    await connection.value.send('SelectDungeon', {
      templateId: input.dungeonTemplateId,
      characterName: input.characterName,
      strength: input.strength,
      dexterity: input.dexterity,
      constitution: input.constitution,
      intelligence: input.intelligence,
      wisdom: input.wisdom,
      charisma: input.charisma,
    })
  }

  function onDungeonReady(callback: (data: DungeonReady) => void) {
    onDungeonReadyCallback.value = callback
  }

  async function confirmTimeAdvance(sessionId: string, choice: string) {
    if (!connection.value || !isConnected.value) return
    await connection.value.invoke('ConfirmTimeAdvance', {
      sessionId: Number(sessionId),
      choice,
    })
  }

  async function chooseOvertime(sessionId: string, choice: string) {
    if (!connection.value || !isConnected.value) return
    await connection.value.invoke('ChooseOvertime', {
      sessionId: Number(sessionId),
      choice,
    })
  }

  async function confirmDangerousAction(sessionId: string, actionId: string, confirmed: boolean) {
    if (!connection.value || !isConnected.value) return
    gameStore.clearDangerousAction()
    if (confirmed) {
      gameStore.disableInput()
      gameStore.setLoading(true, '世界推演中...')
    }
    // fire-and-forget：只发送请求，结果通过推送事件返回
    await connection.value.send('ConfirmDangerousAction', {
      sessionId: Number(sessionId),
      actionId,
      confirmed,
    })
  }

  async function abandonSession(sessionId: string) {
    if (!connection.value || !isConnected.value) return
    await connection.value.invoke('AbandonSession', Number(sessionId))
  }

  async function suspendSession(sessionId: string) {
    if (!connection.value || !isConnected.value) return
    await connection.value.invoke('SuspendSession', Number(sessionId))
  }

  async function restartSession(sessionId: string) {
    if (!connection.value || !isConnected.value) return
    gameStore.clearNarrativeHistory()
    gameStore.setLoading(true, '世界重置中...')
    // fire-and-forget：只发送请求，结果通过 DungeonReady 事件推送回来
    await connection.value.send('RestartSession', {
      sessionId: Number(sessionId),
    })
  }

  return {
    connection,
    isConnected,
    connect,
    disconnect,
    destroy,
    sendPlayerAction,
    selectDungeon,
    onDungeonReady,
    confirmTimeAdvance,
    chooseOvertime,
    confirmDangerousAction,
    abandonSession,
    suspendSession,
    restartSession,
  }
}
