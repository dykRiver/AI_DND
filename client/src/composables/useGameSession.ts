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
