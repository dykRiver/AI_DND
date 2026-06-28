<script setup lang="ts">
import { useGameStore } from '@/stores/game'

const gameStore = useGameStore()
</script>

<template>
  <div>
    <!-- 世界信息面板（可收起） -->
    <Transition name="slide">
      <div
        v-if="gameStore.showWorldInfo && gameStore.worldInfo"
        class="fixed inset-0 z-40 flex"
      >
        <!-- 半透明背景 -->
        <div class="flex-1 bg-black/40" @click="gameStore.toggleWorldInfo()"></div>

        <!-- 面板内容 -->
        <div class="w-80 max-w-[85vw] bg-slate-800/95 backdrop-blur border-l border-gray-700/50 overflow-y-auto custom-scrollbar">
          <!-- 头部 -->
          <div class="sticky top-0 bg-slate-800/95 backdrop-blur border-b border-gray-700/50 px-4 py-3 flex items-center justify-between">
            <h2 class="text-base font-bold text-indigo-300 flex items-center gap-2">
              <span>📜</span>
              {{ gameStore.worldInfo.dungeonName }}
            </h2>
            <button
              @click="gameStore.toggleWorldInfo()"
              class="text-gray-500 hover:text-gray-300 text-lg"
            >
              ✕
            </button>
          </div>

          <div class="px-4 py-4 space-y-5">
            <!-- 世界背景 -->
            <section v-if="gameStore.worldInfo.worldBackground">
              <h3 class="text-sm font-semibold text-amber-400 mb-2 flex items-center gap-1.5">
                <span>🌍</span> 世界背景
              </h3>
              <div class="text-sm text-gray-300 leading-relaxed whitespace-pre-line bg-slate-900/50 rounded-lg p-3 border border-gray-700/30">
                {{ gameStore.worldInfo.worldBackground }}
              </div>
            </section>

            <!-- 主线任务 -->
            <section v-if="gameStore.worldInfo.mainQuestObjective">
              <h3 class="text-sm font-semibold text-emerald-400 mb-2 flex items-center gap-1.5">
                <span>⚔️</span> 主线任务
              </h3>
              <div class="bg-slate-900/50 rounded-lg p-3 border border-gray-700/30">
                <p class="text-sm text-gray-200 font-medium mb-2">
                  {{ gameStore.worldInfo.mainQuestObjective }}
                </p>
                <!-- 关键节点 -->
                <div v-if="gameStore.worldInfo.mainQuestNodes.length > 0" class="mt-3">
                  <p class="text-xs text-gray-500 mb-1.5">关键节点:</p>
                  <ol class="space-y-1.5">
                    <li
                      v-for="(node, idx) in gameStore.worldInfo.mainQuestNodes"
                      :key="idx"
                      class="text-xs text-gray-400 flex items-start gap-2"
                    >
                      <span class="text-indigo-400 font-mono shrink-0 mt-px">{{ idx + 1 }}.</span>
                      <span>{{ node }}</span>
                    </li>
                  </ol>
                </div>
              </div>
            </section>

            <!-- 支线任务 -->
            <section v-if="gameStore.worldInfo.sideQuests && gameStore.worldInfo.sideQuests.length > 0">
              <h3 class="text-sm font-semibold text-yellow-400 mb-2 flex items-center gap-1.5">
                <span>📝</span> 支线任务
              </h3>
              <div class="space-y-2">
                <div
                  v-for="(sq, idx) in gameStore.worldInfo.sideQuests"
                  :key="idx"
                  class="bg-slate-900/50 rounded-lg px-3 py-2 border border-gray-700/30"
                  :class="{ 'border-emerald-600/40': sq.isCompleted }"
                >
                  <div class="flex items-center gap-2">
                    <span class="text-xs" :class="sq.isCompleted ? 'text-emerald-400' : 'text-gray-500'">
                      {{ sq.isCompleted ? '✅' : '○' }}
                    </span>
                    <span
                      class="text-xs font-medium"
                      :class="sq.isCompleted ? 'text-emerald-300 line-through' : 'text-gray-200'"
                    >
                      {{ sq.name }}
                    </span>
                  </div>
                  <p v-if="sq.description" class="text-xs text-gray-500 mt-1 ml-5">{{ sq.description }}</p>
                </div>
              </div>
            </section>

            <!-- 关键地点 -->
            <section v-if="gameStore.worldInfo.keyLocations.length > 0">
              <h3 class="text-sm font-semibold text-blue-400 mb-2 flex items-center gap-1.5">
                <span>📍</span> 关键地点
              </h3>
              <div class="space-y-2">
                <div
                  v-for="(loc, idx) in gameStore.worldInfo.keyLocations"
                  :key="idx"
                  class="text-xs text-gray-400 bg-slate-900/50 rounded-lg px-3 py-2 border border-gray-700/30"
                >
                  {{ loc }}
                </div>
              </div>
            </section>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.slide-enter-active,
.slide-leave-active {
  transition: opacity 0.3s ease;
}
.slide-enter-active > div:last-child,
.slide-leave-active > div:last-child {
  transition: transform 0.3s ease;
}
.slide-enter-from,
.slide-leave-to {
  opacity: 0;
}
.slide-enter-from > div:last-child,
.slide-leave-to > div:last-child {
  transform: translateX(100%);
}
</style>
