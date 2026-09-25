import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useSignalR } from './useSignalR'
import { useGameStore } from '@/stores/game'
import type { CharacterCreateInput } from '@/types/game'

export function useGameSession() {
  const router = useRouter()
  const gameStore = useGameStore()
  const signalR = useSignalR()
  const isStarting = ref(false)

  async function initConnection() {
    try {
      await signalR.connect()
    } catch (err) {
      gameStore.addSystemMessage({ type: 'error', content: '无法连接到游戏服务器' })
    }
  }

  async function startDungeon(input: CharacterCreateInput) {
    isStarting.value = true
    try {
      await signalR.connect()

      // 注册 DungeonReady 回调：收到服务端推送后跳转游戏页
      signalR.onDungeonReady(() => {
        router.push('/game')
      })

      // fire-and-forget：发送请求后立即返回，不阻塞
      await signalR.selectDungeon(input)
    } catch (err) {
      console.error('创建副本失败:', err)
      gameStore.addSystemMessage({ type: 'error', content: '创建副本失败' })
      gameStore.setLoading(false)
    } finally {
      isStarting.value = false
    }
  }

  async function sendAction(text: string) {
    if (!text.trim()) return
    if (!gameStore.sessionId) return
    await signalR.sendPlayerAction(gameStore.sessionId, text, gameStore.isAdultMode)
  }

  /// 自由输入框新语义：提交目标声明（非行动）。不触发AI链路、不刷新当前选项，
  /// 仅写库+推送一条info反馈；下一轮书记官产出的选项将围绕该目标生成。
  async function setGoal(text: string) {
    const trimmed = text.trim()
    if (!trimmed) return
    if (!gameStore.sessionId) return
    await signalR.setPlayerGoal(gameStore.sessionId, trimmed)
    // 乐观更新本地状态（服务端仅写库，失败概率极低；即使失败下次DungeonReady会以服务端为权威回填）
    gameStore.setCurrentGoal(trimmed)
  }

  /// 清空玩家当前目标（点击目标chip上的×按钮）。复用SetPlayerGoal传空串：
  /// 服务端存空字符串、不推送info反馈；书记官下轮不再注入[玩家当前目标]，选项回归默认行为。
  async function clearGoal() {
    if (!gameStore.sessionId) return
    await signalR.setPlayerGoal(gameStore.sessionId, '')
    gameStore.clearCurrentGoal()
  }

  async function selectCachedAction(optionIndex: number, actionText: string) {
    if (!gameStore.sessionId) return
    await signalR.selectCachedAction(gameStore.sessionId, optionIndex, actionText)
  }

  async function handleTimeAdvance(choice: string) {
    if (!gameStore.sessionId) return
    await signalR.confirmTimeAdvance(gameStore.sessionId, choice)
  }

  async function handleOvertime(choice: string) {
    if (!gameStore.sessionId) return
    await signalR.chooseOvertime(gameStore.sessionId, choice)
  }

  async function handleDangerConfirm(actionId: string, confirmed: boolean) {
    if (!gameStore.sessionId) return
    await signalR.confirmDangerousAction(gameStore.sessionId, actionId, confirmed)
  }

  async function abandon() {
    if (!gameStore.sessionId) return
    await signalR.abandonSession(gameStore.sessionId)
    gameStore.clearSession()
    router.push('/')
  }

  async function suspend() {
    if (!gameStore.sessionId) return
    await signalR.suspendSession(gameStore.sessionId)
    gameStore.clearSession()
    router.push('/')
  }

  async function restart() {
    if (!gameStore.sessionId) return
    await signalR.restartSession(gameStore.sessionId)
  }

  function endSession(sessionId: string) {
    gameStore.clearSession()
    router.push(`/settlement/${sessionId}`)
  }

  return {
    isStarting,
    initConnection,
    startDungeon,
    sendAction,
    setGoal,
    clearGoal,
    selectCachedAction,
    handleTimeAdvance,
    handleOvertime,
    handleDangerConfirm,
    abandon,
    suspend,
    restart,
    endSession,
    signalR,
  }
}
