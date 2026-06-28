<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { useGameStore } from '@/stores/game'
import HpBar from './HpBar.vue'

const gameStore = useGameStore()

const emit = defineEmits<{
  (e: 'reinit'): void
  (e: 'restart'): void
  (e: 'exit'): void
  (e: 'toggleBackpack'): void
  (e: 'toggleWorldInfo'): void
}>()

const props = defineProps<{
  isReiniting?: boolean
  isRestarting?: boolean
  dungeonName?: string
}>()

const showMenu = ref(false)
const menuRef = ref<HTMLElement | null>(null)
const toggleBtnRef = ref<HTMLElement | null>(null)
const menuPos = ref({ top: 0, right: 0 })

function toggleMenu() {
  if (!showMenu.value && toggleBtnRef.value) {
    const rect = toggleBtnRef.value.getBoundingClientRect()
    menuPos.value = {
      top: rect.bottom + 4,
      right: window.innerWidth - rect.right,
    }
  }
  showMenu.value = !showMenu.value
}

function closeMenu() {
  showMenu.value = false
}

function handleClickOutside(e: MouseEvent) {
  if (!showMenu.value) return
  const target = e.target as Node
  if (
    menuRef.value && !menuRef.value.contains(target) &&
    toggleBtnRef.value && !toggleBtnRef.value.contains(target)
  ) {
    showMenu.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside, true)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside, true)
})

const segmentIcon = computed(() => {
  const map: Record<string, string> = {
    '清晨': '🌅',
    '上午': '☀️',
    '下午': '🌤️',
    '傍晚': '🌆',
    '夜晚': '🌙',
  }
  return map[gameStore.gameState.currentSegment] || '⏳'
})

const tensionColor = computed(() => {
  const t = gameStore.gameState.tensionLevel
  if (t <= 3) return 'bg-blue-500'
  if (t <= 6) return 'bg-amber-500'
  return 'bg-rose-500'
})
</script>

<template>
  <div class="bg-slate-800/90 backdrop-blur border-b border-gray-700/50 px-3 py-1.5 safe-top relative">
    <div class="flex items-center gap-2">
      <!-- 左侧：HP -->
      <div class="w-24 shrink-0">
        <HpBar
          :current="gameStore.gameState.currentHp"
          :max="gameStore.gameState.maxHp"
          :percent="gameStore.gameState.hpPercent"
        />
      </div>

      <!-- 中间：时段 + 紧张度 -->
      <div class="flex items-center gap-2 flex-1 min-w-0 justify-end">
        <div class="flex items-center gap-1 text-xs text-gray-300 shrink-0">
          <span>{{ segmentIcon }}</span>
          <span>Day{{ gameStore.gameState.currentDay }}</span>
          <span class="text-gray-500 hidden sm:inline">{{ gameStore.gameState.currentSegment }}</span>
        </div>
        <div class="flex items-center gap-1 shrink-0">
          <div class="w-10 h-1.5 bg-gray-700 rounded-full overflow-hidden">
            <div
              :class="tensionColor"
              class="h-full rounded-full transition-all duration-500"
              :style="{ width: `${gameStore.gameState.tensionLevel * 10}%` }"
            ></div>
          </div>
          <span v-if="gameStore.gameState.isFatigued" class="text-xs" title="疲劳">😴</span>
          <span v-if="gameStore.gameState.isInCombat" class="text-xs" title="战斗中">⚔️</span>
        </div>

        <!-- 展开菜单按钮 -->
        <div class="relative shrink-0">
          <button
            ref="toggleBtnRef"
            @click.stop="toggleMenu"
            class="w-8 h-8 flex items-center justify-center rounded-lg bg-slate-700/60 border border-gray-600/40 text-gray-300 hover:bg-slate-600/60 transition-colors text-lg leading-none"
          >
            ⋯
          </button>

          <!-- 展开菜单（Teleport 到 body，避免被父级 overflow-hidden 裁剪） -->
          <Teleport to="body">
            <Transition name="menu-fade">
              <div
                v-if="showMenu"
                ref="menuRef"
                :style="{ top: menuPos.top + 'px', right: menuPos.right + 'px' }"
                class="fixed z-[9999] w-48 bg-gray-100 border border-gray-300 rounded-xl shadow-2xl shadow-black/30 py-1 overflow-hidden"
              >
                <!-- 功能按钮组 -->
                <button
                  @click="emit('toggleBackpack'); closeMenu()"
                  class="w-full text-left text-sm px-4 py-2.5 text-gray-700 hover:bg-gray-200 transition-colors flex items-center gap-2"
                >
                  <span>🎒</span> 背包
                </button>

                <router-link
                  to="/character"
                  @click="closeMenu()"
                  class="block text-sm px-4 py-2.5 text-gray-700 hover:bg-gray-200 transition-colors"
                >
                  <span class="mr-2">📋</span>角色面板
                </router-link>

                <button
                  v-if="props.dungeonName"
                  @click="emit('toggleWorldInfo'); closeMenu()"
                  class="w-full text-left text-sm px-4 py-2.5 text-indigo-600 hover:bg-gray-200 transition-colors flex items-center gap-2"
                >
                  <span>📜</span> {{ props.dungeonName }}
                </button>

                <button
                  @click="gameStore.toggleAdultMode(); closeMenu()"
                  class="w-full text-left text-sm px-4 py-2.5 hover:bg-gray-200 transition-colors flex items-center gap-2"
                  :class="gameStore.isAdultMode ? 'text-rose-600' : 'text-gray-500'"
                >
                  <span>{{ gameStore.isAdultMode ? '🔞' : '🔒' }}</span>
                  {{ gameStore.isAdultMode ? '成人模式 ON' : '成人模式 OFF' }}
                </button>

                <!-- 分隔线 -->
                <div class="border-t border-gray-200 my-1"></div>

                <!-- 操作按钮组 -->
                <button
                  @click="emit('restart'); closeMenu()"
                  :disabled="props.isRestarting"
                  class="w-full text-left text-sm px-4 py-2.5 text-cyan-600 hover:bg-gray-200 transition-colors disabled:opacity-50 flex items-center gap-2"
                >
                  <span>🔄</span> {{ props.isRestarting ? '重置中...' : '重新开始' }}
                </button>
                <button
                  @click="emit('reinit'); closeMenu()"
                  :disabled="props.isReiniting"
                  class="w-full text-left text-sm px-4 py-2.5 text-amber-600 hover:bg-gray-200 transition-colors disabled:opacity-50 flex items-center gap-2"
                >
                  <span>🔧</span> {{ props.isReiniting ? '重建中...' : '重建角色' }}
                </button>
                <button
                  @click="emit('exit'); closeMenu()"
                  class="w-full text-left text-sm px-4 py-2.5 text-gray-500 hover:bg-gray-200 transition-colors flex items-center gap-2"
                >
                  <span>🚪</span> 退出副本
                </button>
              </div>
            </Transition>
          </Teleport>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.menu-fade-enter-active,
.menu-fade-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.menu-fade-enter-from,
.menu-fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
