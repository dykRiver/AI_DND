<script setup lang="ts">
import { computed, ref, onMounted, onBeforeUnmount, nextTick, watch } from 'vue'
import type { TalentNode } from '@/types/game'

const props = defineProps<{
  nodes: TalentNode[]
  availablePoints: number
}>()

const emit = defineEmits<{
  unlock: [nodePath: string]
}>()

// ===== 数据分组 =====
const mainRoutes = ['combat', 'stealth', 'social', 'survival'] as const

const bridgeNodes = computed(() =>
  props.nodes.filter(n => n.isBridge).sort((a, b) => a.nodePath.localeCompare(b.nodePath))
)

const mainNodesMap = computed(() => {
  const map = new Map<string, TalentNode[]>()
  props.nodes.forEach(node => {
    if (node.isBridge) return
    if (!map.has(node.routeName)) map.set(node.routeName, [])
    map.get(node.routeName)!.push(node)
  })
  map.forEach(arr => arr.sort((a, b) => a.position - b.position))
  return map
})

// 桥接 → 关联路线 position 3
const bridgeTargetMap: Record<string, string[]> = {
  bridge_combat_stealth: ['combat_3', 'stealth_3'],
  bridge_combat_social: ['combat_3', 'social_3'],
  bridge_stealth_social: ['stealth_3', 'social_3'],
  bridge_combat_survival: ['combat_3', 'survival_3'],
}

// ===== 路线颜色 =====
const routeSvgColors: Record<string, string> = {
  combat: '#f43f5e', stealth: '#10b981', social: '#3b82f6',
  survival: '#f59e0b', bridge: '#a855f7',
}
const routeBorderCls: Record<string, string> = {
  combat: 'border-rose-500', stealth: 'border-emerald-500',
  social: 'border-blue-500', survival: 'border-amber-500', bridge: 'border-purple-500',
}
const routeBgCls: Record<string, string> = {
  combat: 'bg-rose-500', stealth: 'bg-emerald-500',
  social: 'bg-blue-500', survival: 'bg-amber-500', bridge: 'bg-purple-500',
}
const routeLabels: Record<string, string> = {
  combat: '战斗', stealth: '潜行', social: '社交', survival: '生存',
}
const bridgeShortLabels: Record<string, string> = {
  bridge_combat_stealth: '战斗-潜行', bridge_combat_social: '战斗-社交',
  bridge_stealth_social: '潜行-社交', bridge_combat_survival: '战斗-生存',
}

// ===== 节点样式 =====
function getNodeClasses(node: TalentNode) {
  const border = routeBorderCls[node.routeName] || 'border-gray-500'
  const bg = routeBgCls[node.routeName] || 'bg-gray-500'
  const cls: string[] = [border, 'border-2', 'flex', 'items-center', 'justify-center', 'cursor-pointer', 'transition-all', 'relative']
  if (node.isBridge) {
    cls.push('w-9', 'h-9', 'rotate-45', 'rounded-md')
  } else {
    cls.push('w-9', 'h-9', 'rounded-full')
  }
  if (node.isUnlocked) cls.push(bg, 'opacity-100', 'shadow-lg')
  else if (node.canUnlock) cls.push(bg, 'opacity-60', 'animate-pulse')
  else cls.push('bg-gray-700/50', 'border-gray-600', 'opacity-50')
  return cls
}

// ===== SVG 连线 =====
const containerRef = ref<HTMLElement>()
const svgRef = ref<SVGElement>()
const nodeRefs = new Map<string, HTMLElement>()

function setNodeRef(el: any, path: string) {
  if (el) nodeRefs.set(path, el.$el || el)
  else nodeRefs.delete(path)
}

function getCenter(path: string, rect: DOMRect) {
  const el = nodeRefs.get(path)
  if (!el) return null
  const r = el.getBoundingClientRect()
  return { x: r.left + r.width / 2 - rect.left, y: r.top + r.height / 2 - rect.top }
}

interface SvgEdge { x1: number; y1: number; x2: number; y2: number; color: string; dash: boolean }
const svgEdges = ref<SvgEdge[]>([])

function measureAndDraw() {
  const container = containerRef.value
  const svg = svgRef.value
  if (!container || !svg) return

  const rect = container.getBoundingClientRect()
  svg.setAttribute('width', String(rect.width))
  svg.setAttribute('height', String(rect.height))

  const edges: SvgEdge[] = []

  // 同路线纵向连接: position N → N+1
  mainRoutes.forEach(route => {
    const nodes = mainNodesMap.value.get(route)
    if (!nodes) return
    for (let i = 0; i < nodes.length - 1; i++) {
      const a = getCenter(nodes[i].nodePath, rect)
      const b = getCenter(nodes[i + 1].nodePath, rect)
      if (!a || !b) continue
      const both = nodes[i].isUnlocked && nodes[i + 1].isUnlocked
      const any = nodes[i].isUnlocked || nodes[i + 1].isUnlocked
      edges.push({
        x1: a.x, y1: a.y, x2: b.x, y2: b.y,
        color: both ? (routeSvgColors[route] || '#6b7280') : any ? '#4b5563' : '#374151',
        dash: !both,
      })
    }
  })

  // 桥接连接: bridge → 关联路线 position 3
  bridgeNodes.value.forEach(bridge => {
    const targets = bridgeTargetMap[bridge.nodePath] || []
    const bc = getCenter(bridge.nodePath, rect)
    if (!bc) return
    targets.forEach(tp => {
      const tc = getCenter(tp, rect)
      if (!tc) return
      const routeName = tp.split('_')[0]
      const targetNode = mainNodesMap.value.get(routeName)?.find(n => n.position === 3)
      const both = bridge.isUnlocked && (targetNode?.isUnlocked ?? false)
      edges.push({
        x1: bc.x, y1: bc.y, x2: tc.x, y2: tc.y,
        color: both ? routeSvgColors.bridge : '#4b5563',
        dash: !both,
      })
    })
  })

  svgEdges.value = edges
}

let rafId = 0
function scheduleMeasure() {
  cancelAnimationFrame(rafId)
  rafId = requestAnimationFrame(() => nextTick(measureAndDraw))
}

let resizeObserver: ResizeObserver | null = null

onMounted(() => {
  scheduleMeasure()
  if (containerRef.value) {
    resizeObserver = new ResizeObserver(scheduleMeasure)
    resizeObserver.observe(containerRef.value)
  }
})

onBeforeUnmount(() => {
  resizeObserver?.disconnect()
  cancelAnimationFrame(rafId)
})

watch(() => props.nodes, scheduleMeasure, { deep: true })

// ===== 交互 =====
function handleUnlock(node: TalentNode) {
  if (!node.canUnlock || node.isUnlocked) return
  if (props.availablePoints <= 0) return
  emit('unlock', node.nodePath)
}
</script>

<template>
  <div class="space-y-4">
    <div class="flex items-center justify-between mb-4">
      <h3 class="text-sm font-medium text-gray-300">天赋树</h3>
      <span class="text-xs text-indigo-400">可用点数: {{ availablePoints }}</span>
    </div>

    <div v-if="nodes.length === 0" class="text-center py-8 text-gray-500 text-sm">
      暂无天赋数据，请先完成一次副本以激活天赋树
    </div>

    <div v-else ref="containerRef" class="relative">
      <!-- SVG 连接线 -->
      <svg
        ref="svgRef"
        class="absolute inset-0 pointer-events-none"
        style="z-index: 1; overflow: visible;"
      >
        <line
          v-for="(edge, i) in svgEdges"
          :key="i"
          :x1="edge.x1" :y1="edge.y1"
          :x2="edge.x2" :y2="edge.y2"
          :stroke="edge.color"
          stroke-width="2"
          :stroke-dasharray="edge.dash ? '4 3' : undefined"
          stroke-linecap="round"
        />
      </svg>

      <!-- 四条主路线 -->
      <div class="grid grid-cols-4 gap-2 relative" style="z-index: 2;">
        <div v-for="route in mainRoutes" :key="route" class="flex flex-col items-center">
          <div class="text-[10px] text-gray-500 font-medium mb-2">{{ routeLabels[route] }}</div>
          <div class="flex flex-col items-center gap-1">
            <div
              v-for="node in mainNodesMap.get(route)"
              :key="node.nodePath"
              :ref="(el) => setNodeRef(el, node.nodePath)"
              @click="handleUnlock(node)"
              :class="getNodeClasses(node)"
              :title="`${node.nodeName}\n${node.nodeEffect}`"
            >
              <span class="text-[8px] text-white font-bold leading-tight text-center select-none">
                {{ node.nodeName.slice(0, 2) }}
              </span>
              <div
                v-if="node.isUnlocked"
                class="absolute -top-1 -right-1 w-2.5 h-2.5 bg-emerald-400 rounded-full border border-slate-800"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- 桥接节点区域 -->
      <div v-if="bridgeNodes.length" class="mt-6 relative" style="z-index: 2;">
        <div class="text-[10px] text-gray-600 text-center mb-2">— 桥接 —</div>
        <div class="flex justify-center gap-3 flex-wrap">
          <div v-for="node in bridgeNodes" :key="node.nodePath" class="flex flex-col items-center gap-1">
            <div
              :ref="(el) => setNodeRef(el, node.nodePath)"
              @click="handleUnlock(node)"
              :class="getNodeClasses(node)"
              :title="`${node.nodeName}\n${node.nodeEffect}`"
            >
              <span class="text-[8px] text-white font-bold leading-tight text-center select-none -rotate-45">
                {{ node.nodeName.slice(0, 2) }}
              </span>
              <div
                v-if="node.isUnlocked"
                class="absolute -top-1 -right-1 w-2.5 h-2.5 bg-emerald-400 rounded-full border border-slate-800"
              />
            </div>
            <span class="text-[8px] text-gray-600">{{ bridgeShortLabels[node.nodePath] || '' }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
