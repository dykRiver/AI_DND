// ========== 游戏状态 ==========
export interface GameState {
  currentHp: number
  maxHp: number
  hpPercent: number
  status: string // 正常/轻伤/重伤/濒死
  currentDay: number
  currentSegment: string
  tensionLevel: number
  isFatigued: boolean
  isInCombat: boolean
}

// ========== 叙事chunk ==========
export interface NarrativeChunk {
  text: string
  chunkType: 'narrative' | 'action_result' | 'scene_transition'
  isLast: boolean
  timestamp: string
}

// ========== 骰子结果 ==========
export interface DiceResult {
  skillName: string
  d20Roll: number
  modifier: number
  total: number
  dc: number
  worldDifficultyModifier: number
  effectiveDC: number
  isSuccess: boolean
  isNatural20: boolean
  isNatural1: boolean
  narrativeHint: string
}

// ========== 时段转换 ==========
export interface TimeTransition {
  narrative: string
  newSegment: string
  newDay: number
}

// ========== 副本模板 ==========
export interface DungeonTemplate {
  id: number
  name: string
  worldTheme: string
  difficulty: string
  difficultyModifier: number
  timeLimitDays: number
  tags: string[]
  description: string
}

// ========== Meta档案 ==========
export interface PlayerMeta {
  id: number
  metaLevel: number
  experience: number
  bonusStrength: number
  bonusDexterity: number
  bonusConstitution: number
  bonusIntelligence: number
  bonusWisdom: number
  bonusCharisma: number
  talentPoints: number
  dungeonCount: number
}

// ========== 天赋节点 ==========
export interface TalentNode {
  nodePath: string
  nodeName: string
  nodeEffect: string
  routeName: string
  position: number
  isUnlocked: boolean
  isBridge: boolean
  canUnlock: boolean
}

// ========== 段位 ==========
export interface PlayerRank {
  rankTier: number
  rankName: string
  canPromote: boolean
  dungeonCountToNext: number
  isInPromotion: boolean
}

// ========== 结算 ==========
export interface SettlementData {
  exitNarrative: string
  epilogue: string
  comment: string
  scoreLevel: string
  rewards: {
    attributePoints: number
    skillPoints: number
    metaExp: number
    talentFragments: number
  }
}

// ========== 角色创建 ==========
export interface CharacterCreateInput {
  dungeonTemplateId: number
  characterName: string
  strength: number
  dexterity: number
  constitution: number
  intelligence: number
  wisdom: number
  charisma: number
}

// ========== 选择项 ==========
export interface PlayerChoice {
  prompt: string
  choices: string[]
  isRequired: boolean
}

// ========== 危险行动确认 ==========
export interface DangerousActionConfirm {
  actionId: string
  description: string
  warning: string
}

// ========== 副本生成进度 ==========
export interface GeneratingProgress {
  phase: string
  progressPercent: number
  message?: string
}

// ========== 副本世界信息 ==========
export interface WorldInfo {
  dungeonName: string
  worldBackground: string
  mainQuestObjective: string
  mainQuestNodes: string[]
  keyLocations: string[]
  sideQuests: SideQuestInfo[]
}

// ========== 支线任务信息 ==========
export interface SideQuestInfo {
  name: string
  description: string
  isCompleted: boolean
}

// ========== 支线任务状态更新 ==========
export interface SideQuestUpdate {
  completedSideQuests: string[]
  unlockedSideQuests: SideQuestInfo[]
}

// ========== 副本结束数据 ==========
export interface SessionEndData {
  sessionId: number
  endReason: 'completed' | 'died' | 'abandoned'
  scoreLevel: string | null
  exitNarrative: string | null
  epilogue: string | null
  comment: string | null
}

// ========== 副本就绪通知 ==========
export interface DungeonReady {
  sessionId: number
  dungeonName: string
  worldInfo: WorldInfo
  gameState: GameState
}

// ========== 系统消息 ==========
export interface SystemMessage {
  type: 'info' | 'warning' | 'error' | 'success'
  content: string
}

// ========== 天赋树 ==========
export interface TalentTreeData {
  nodes: TalentNode[]
  availablePoints: number
}

// ========== 登录 ==========
export interface LoginRequest {
  account: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
}

// ========== 属性分配 ==========
export interface AttributeAllocation {
  attribute: string
  points: number
}

// ========== 活跃会话检查（断线续玩） ==========
export interface ActiveSessionResult {
  sessionId: number
  templateId: number
  dungeonName: string
  worldInfo: WorldInfo
  gameState: GameState
  recentNarratives: ActiveSessionNarrative[]
}

export interface ActiveSessionNarrative {
  text: string
  chunkType: 'narrative' | 'action_result' | 'scene_transition'
}

// ========== 背包道具 ==========
export interface InventoryItem {
  id: number
  itemName: string
  itemType: string
  description?: string
  weight: number
  attributeBonus: number
  linkedAttribute?: string
  maxUses: number
  currentUses: number
  isUnlimited: boolean
  isEquipped: boolean
  isKeyItem: boolean
  quantity: number
}

// ========== 背包状态 ==========
export interface BackpackStatus {
  items: InventoryItem[]
  currentWeight: number
  maxWeight: number
  weightPercent: number
  isOverloaded: boolean
  isEncumbered: boolean
  equippedWeapon?: InventoryItem
  equippedArmor?: InventoryItem
}

// ========== 背包更新推送 ==========
export interface InventoryUpdate {
  items: InventoryItem[]
  currentWeight: number
  maxWeight: number
  weightPercent: number
  isOverloaded: boolean
  isEncumbered: boolean
  equippedWeaponId?: number
  equippedArmorId?: number
}

// ========== 已知情报/无形资产 ==========
export interface KnownAsset {
  id: number
  assetType: string
  name: string
  content?: string
  source?: string
  acquiredRound: number
}

// ========== 已知情报更新推送 ==========
export interface KnownAssetsUpdate {
  assets: KnownAsset[]
}

// ========== 建议行动选项（预计算快速选择） ==========
export interface SuggestedAction {
  index: number
  actionText: string
  hint: string
  isFeasible?: boolean
}

export interface SuggestedActionsData {
  options: SuggestedAction[]
  isComputing: boolean
}
