import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getPlayerMeta, getTalentTree, getPlayerRank } from '@/api/game'
import type { PlayerMeta, TalentNode, PlayerRank } from '@/types/game'
import { useAuthStore } from '@/stores/auth'

const DEFAULT_META: PlayerMeta = {
  id: 0,
  metaLevel: 1,
  experience: 0,
  bonusStrength: 0,
  bonusDexterity: 0,
  bonusConstitution: 0,
  bonusIntelligence: 0,
  bonusWisdom: 0,
  bonusCharisma: 0,
  talentPoints: 0,
  dungeonCount: 0,
}

const DEFAULT_RANK: PlayerRank = {
  rankTier: 1,
  rankName: '初入者',
  canPromote: false,
  dungeonCountToNext: 5,
  isInPromotion: false,
}

export const useMetaStore = defineStore('meta', () => {
  const meta = ref<PlayerMeta>({ ...DEFAULT_META })

  const talentNodes = ref<TalentNode[]>([])
  const availableTalentPoints = ref(0)

  const rank = ref<PlayerRank>({ ...DEFAULT_RANK })

  const isLoading = ref(false)

  async function fetchMeta() {
    const authStore = useAuthStore()
    if (!authStore.userId) return
    isLoading.value = true
    try {
      const data = await getPlayerMeta(authStore.userId)
      // API 返回 null 时保留默认值（新用户尚无数据）
      if (data) meta.value = data
    } catch (e) {
      console.error('Failed to fetch meta:', e)
    } finally {
      isLoading.value = false
    }
  }

  async function fetchTalentTree() {
    if (!meta.value?.id) return
    try {
      const data = await getTalentTree(meta.value.id)
      if (data) {
        talentNodes.value = data.nodes ?? []
        availableTalentPoints.value = data.availablePoints ?? 0
      }
    } catch (e) {
      console.error('Failed to fetch talent tree:', e)
    }
  }

  async function fetchRank() {
    const authStore = useAuthStore()
    if (!authStore.userId) return
    try {
      const data = await getPlayerRank(authStore.userId)
      // API 返回 null 时保留默认值
      if (data) rank.value = data
    } catch (e) {
      console.error('Failed to fetch rank:', e)
    }
  }

  async function fetchAll() {
    // 必须先获取meta（拿到metaId），再并行获取天赋树和段位
    await fetchMeta()
    await Promise.all([fetchTalentTree(), fetchRank()])
  }

  return {
    meta,
    talentNodes,
    availableTalentPoints,
    rank,
    isLoading,
    fetchMeta,
    fetchTalentTree,
    fetchRank,
    fetchAll,
  }
})
