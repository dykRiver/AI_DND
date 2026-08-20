<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useGameStore } from '@/stores/game'
import { getBackpack, equipItem, unequipItem, dropItem, getKnownAssets } from '@/api/game'
import type { BackpackStatus, InventoryItem } from '@/types/game'

const gameStore = useGameStore()
const backpack = ref<BackpackStatus | null>(null)
const loading = ref(false)

const emit = defineEmits<{ close: [] }>()

// 已知情报：优先用 SignalR 实时推送的 store 数据；面板打开时也主动拉取一次以防断线错过推送。
const knownAssets = computed(() => gameStore.knownAssets)

onMounted(async () => {
  await Promise.all([refreshBackpack(), refreshKnownAssets()])
})

async function refreshKnownAssets() {
  if (!gameStore.sessionId) return
  try {
    const data = await getKnownAssets(gameStore.sessionId)
    gameStore.updateKnownAssets({ assets: data })
  } catch {
    // 情报拉取失败时静默，保留现有推送数据
  }
}

async function refreshBackpack() {
  if (!gameStore.sessionId) return
  loading.value = true
  try {
    const data = await getBackpack(gameStore.sessionId)
    backpack.value = data
  } catch (e: any) {
    gameStore.addSystemMessage({ type: 'error', content: `背包加载失败: ${e?.message || e}` })
  } finally {
    loading.value = false
  }
}

const weightBarColor = computed(() => {
  if (!backpack.value) return 'bg-emerald-500'
  const pct = backpack.value.weightPercent
  if (pct >= 100) return 'bg-rose-500'
  if (pct >= 70) return 'bg-amber-500'
  return 'bg-emerald-500'
})

const weightBarWidth = computed(() => {
  if (!backpack.value) return '0%'
  return `${Math.min(backpack.value.weightPercent, 100)}%`
})

function attrLabel(attr?: string): string {
  if (!attr) return ''
  const map: Record<string, string> = {
    STR: '力量', DEX: '敏捷', CON: '体质', INT: '智力', WIS: '感知', CHA: '魅力'
  }
  return map[attr.toUpperCase()] ?? attr
}

function isEquippable(item: InventoryItem): boolean {
  return (item.itemType === '武器' || item.itemType === '防具') && !item.isEquipped
}

function isUnequippable(item: InventoryItem): boolean {
  return item.isEquipped
}

function isDroppable(item: InventoryItem): boolean {
  return !item.isKeyItem
}

function usesText(item: InventoryItem): string {
  if (item.isUnlimited) return '∞'
  if (item.maxUses === 0) return '-'
  return `${item.currentUses}/${item.maxUses}`
}

function isOutOfUses(item: InventoryItem): boolean {
  return !item.isUnlimited && item.maxUses > 0 && item.currentUses <= 0
}

async function handleEquip(item: InventoryItem) {
  if (!gameStore.sessionId) return
  try {
    await equipItem(gameStore.sessionId, item.id)
    await refreshBackpack()
    gameStore.addSystemMessage({ type: 'success', content: `已装备 ${item.itemName}` })
  } catch (e: any) {
    gameStore.addSystemMessage({ type: 'error', content: `装备失败: ${e?.message || e}` })
  }
}

async function handleUnequip(item: InventoryItem) {
  if (!gameStore.sessionId) return
  try {
    await unequipItem(gameStore.sessionId, item.id)
    await refreshBackpack()
    gameStore.addSystemMessage({ type: 'info', content: `已卸装 ${item.itemName}` })
  } catch (e: any) {
    gameStore.addSystemMessage({ type: 'error', content: `卸装失败: ${e?.message || e}` })
  }
}

async function handleDrop(item: InventoryItem) {
  if (!gameStore.sessionId || item.isKeyItem) return
  if (!confirm(`确定丢弃 ${item.itemName}？`)) return
  try {
    await dropItem(gameStore.sessionId, item.id, item.quantity)
    await refreshBackpack()
    gameStore.addSystemMessage({ type: 'info', content: `已丢弃 ${item.itemName}` })
  } catch (e: any) {
    gameStore.addSystemMessage({ type: 'error', content: `丢弃失败: ${e?.message || e}` })
  }
}

// 暴露refreshBackpack供父组件调用
defineExpose({ refreshBackpack })
</script>

<template>
  <Transition name="slide">
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm px-4"
      @click.self="emit('close')"
    >
      <div class="bg-slate-800 border border-gray-700/50 rounded-2xl w-full max-w-md max-h-[80vh] flex flex-col overflow-hidden">
        <!-- 标题栏 -->
        <div class="flex items-center justify-between px-5 py-4 border-b border-gray-700/50">
          <h2 class="text-gray-100 font-bold text-lg">背包</h2>
          <button
            @click="emit('close')"
            class="text-gray-400 hover:text-gray-200 text-xl leading-none"
          >×</button>
        </div>

        <!-- 重量条 -->
        <div class="px-5 py-3">
          <div class="flex justify-between text-xs text-gray-400 mb-1">
            <span>重量</span>
            <span :class="{ 'text-rose-400': backpack?.isOverloaded, 'text-amber-400': backpack?.isEncumbered && !backpack?.isOverloaded }">
              {{ backpack?.currentWeight ?? 0 }} / {{ backpack?.maxWeight ?? 0 }}
            </span>
          </div>
          <div class="h-2 bg-slate-700 rounded-full overflow-hidden relative">
            <div
              class="h-full rounded-full transition-all duration-300"
              :class="weightBarColor"
              :style="{ width: weightBarWidth }"
            ></div>
            <!-- 70%标记线 -->
            <div class="absolute top-0 bottom-0 w-px bg-amber-400/60" style="left: 70%"></div>
          </div>
          <div v-if="backpack?.isOverloaded" class="text-rose-400 text-xs mt-1">
            ⚠ 背包超重！必须先丢弃道具才能行动
          </div>
          <div v-else-if="backpack?.isEncumbered" class="text-amber-400 text-xs mt-1">
            ⚠ 负重过大，DEX检定 -2
          </div>
        </div>

        <!-- 装备槽 -->
        <div class="px-5 py-2 border-t border-gray-700/30">
          <p class="text-xs text-gray-500 mb-2">装备槽</p>
          <div class="grid grid-cols-2 gap-2">
            <!-- 武器槽 -->
            <div class="bg-slate-700/50 rounded-xl p-3 border border-gray-600/30">
              <p class="text-xs text-gray-500 mb-1">🗡 武器</p>
              <template v-if="backpack?.equippedWeapon">
                <p class="text-sm text-gray-100 font-medium truncate">{{ backpack.equippedWeapon.itemName }}</p>
                <p class="text-xs text-emerald-400">{{ attrLabel(backpack.equippedWeapon.linkedAttribute) }}+{{ backpack.equippedWeapon.attributeBonus }}</p>
                <p class="text-xs text-gray-500">{{ usesText(backpack.equippedWeapon) }}</p>
              </template>
              <p v-else class="text-xs text-gray-600">空</p>
            </div>
            <!-- 防具槽 -->
            <div class="bg-slate-700/50 rounded-xl p-3 border border-gray-600/30">
              <p class="text-xs text-gray-500 mb-1">🛡 防具</p>
              <template v-if="backpack?.equippedArmor">
                <p class="text-sm text-gray-100 font-medium truncate">{{ backpack.equippedArmor.itemName }}</p>
                <p class="text-xs text-emerald-400">{{ attrLabel(backpack.equippedArmor.linkedAttribute) }}+{{ backpack.equippedArmor.attributeBonus }}</p>
                <p class="text-xs text-gray-500">{{ usesText(backpack.equippedArmor) }}</p>
              </template>
              <p v-else class="text-xs text-gray-600">空</p>
            </div>
          </div>
        </div>

        <!-- 道具列表 -->
        <div class="flex-1 overflow-y-auto px-5 py-3 border-t border-gray-700/30">
          <p class="text-xs text-gray-500 mb-2">道具 ({{ backpack?.items.length ?? 0 }})</p>
          <div v-if="loading" class="text-center text-gray-500 text-sm py-4">加载中...</div>
          <div v-else-if="!backpack?.items.length" class="text-center text-gray-600 text-sm py-4">背包空空如也</div>
          <div v-else class="space-y-2">
            <div
              v-for="item in backpack.items"
              :key="item.id"
              class="flex items-center gap-3 bg-slate-700/30 rounded-xl px-3 py-2.5 border border-gray-700/30"
              :class="{ 'opacity-50': isOutOfUses(item) }"
            >
              <!-- 道具信息 -->
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2">
                  <span class="text-sm text-gray-100 truncate">{{ item.itemName }}</span>
                  <span
                    v-if="item.isEquipped"
                    class="text-xs px-1.5 py-0.5 rounded bg-emerald-500/20 text-emerald-400 border border-emerald-500/30"
                  >已装备</span>
                  <span
                    v-if="item.isKeyItem"
                    class="text-xs px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-400 border border-amber-500/30"
                  >关键</span>
                </div>
                <div class="flex gap-2 text-xs text-gray-500 mt-0.5">
                  <span>重量{{ item.weight }}</span>
                  <span v-if="item.attributeBonus > 0">{{ attrLabel(item.linkedAttribute) }}+{{ item.attributeBonus }}</span>
                  <span v-if="item.maxUses > 0 || item.isUnlimited">次数{{ usesText(item) }}</span>
                  <span v-if="item.quantity > 1">x{{ item.quantity }}</span>
                </div>
              </div>
              <!-- 操作按钮 -->
              <div class="flex gap-1 shrink-0">
                <button
                  v-if="isEquippable(item)"
                  @click="handleEquip(item)"
                  class="text-xs px-2 py-1 rounded bg-indigo-600/20 border border-indigo-500/40 text-indigo-300 hover:bg-indigo-600/30"
                >装备</button>
                <button
                  v-if="isUnequippable(item)"
                  @click="handleUnequip(item)"
                  class="text-xs px-2 py-1 rounded bg-slate-600/40 border border-gray-500/40 text-gray-400 hover:bg-slate-600/60"
                >卸装</button>
                <button
                  v-if="isDroppable(item)"
                  @click="handleDrop(item)"
                  class="text-xs px-2 py-1 rounded bg-rose-600/20 border border-rose-500/40 text-rose-300 hover:bg-rose-600/30"
                >丢弃</button>
              </div>
            </div>
          </div>
        </div>

        <!-- 已知线索/情报（无形资产，由物资官记账登记） -->
        <div class="px-5 py-3 border-t border-gray-700/30 max-h-48 overflow-y-auto">
          <p class="text-xs text-gray-500 mb-2">📜 已知线索 ({{ knownAssets.length }})</p>
          <div v-if="!knownAssets.length" class="text-center text-gray-600 text-sm py-3">暂无已知情报</div>
          <div v-else class="space-y-2">
            <div
              v-for="asset in knownAssets"
              :key="asset.id"
              class="bg-slate-700/30 rounded-xl px-3 py-2.5 border border-gray-700/30"
            >
              <div class="flex items-center gap-2">
                <span class="text-xs px-1.5 py-0.5 rounded bg-sky-500/20 text-sky-300 border border-sky-500/30 shrink-0">{{ asset.assetType }}</span>
                <span class="text-sm text-gray-100 truncate">{{ asset.name }}</span>
              </div>
              <p v-if="asset.content" class="text-xs text-gray-400 mt-1 break-words">{{ asset.content }}</p>
              <p v-if="asset.source" class="text-xs text-gray-600 mt-0.5">来源：{{ asset.source }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.slide-enter-active,
.slide-leave-active {
  transition: opacity 0.2s ease;
}
.slide-enter-from,
.slide-leave-to {
  opacity: 0;
}
</style>
