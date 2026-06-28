import axios from 'axios'
import { sm2 } from 'sm-crypto-v2'
import type {
  LoginRequest,
  LoginResponse,
  DungeonTemplate,
  PlayerMeta,
  TalentNode,
  TalentTreeData,
  PlayerRank,
  SettlementData,
  AttributeAllocation,
  ActiveSessionResult,
  BackpackStatus,
} from '@/types/game'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 15000,
})

// 请求拦截器 - 添加JWT token
http.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// 响应拦截器 - 解包 AdminResult<T> + 处理401
http.interceptors.response.use(
  (response) => {
    // 后端所有响应均被 AdminResult<T> 包裹，统一提取 result 字段
    if (response.data && typeof response.data === 'object' && 'result' in response.data) {
      response.data = response.data.result
    }
    return response
  },
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

// SM2公钥（与后端Cryptogram:PublicKey对应）
const SM2_PUBLIC_KEY = '0484C7466D950E120E5ECE5DD85D0C90EAA85081A3A2BD7C57AE6DC822EFCCBD66620C67B0103FC8DD280E36C3B282977B722AAEC3C56518EDCEBAFB72C5A05312'

// ========== 用户信息 ==========
export interface UserInfo {
  id: number
  account: string
  realName: string
  avatar?: string
}

export async function getUserInfo(): Promise<UserInfo> {
  const res = await http.get<UserInfo>('/api/sysAuth/userInfo')
  return res.data
}

// ========== 认证 ==========
export async function login(data: LoginRequest): Promise<LoginResponse> {
  // 密码需SM2加密后再发送，后端登录时固定做SM2解密
  const encryptedPassword = sm2.doEncrypt(data.password, SM2_PUBLIC_KEY, 1)
  const res = await http.post<LoginResponse>('/api/sysAuth/login', {
    account: data.account,
    password: encryptedPassword,
  })
  return res.data
}

// ========== 副本 ==========
export async function getDungeonTemplates(): Promise<DungeonTemplate[]> {
  const res = await http.get<DungeonTemplate[]>('/api/dungeonExplore/getAllTemplates')
  return res.data
}

// ========== Meta ==========
export async function getPlayerMeta(userId: number): Promise<PlayerMeta> {
  const res = await http.get<PlayerMeta>('/api/metaProgression/getMeta', { params: { userId } })
  return res.data
}

// ========== 天赋树 ==========
export async function getTalentTree(metaId: number): Promise<TalentTreeData> {
  const res = await http.get<TalentTreeData>('/api/talentTree/getTalentTree', { params: { metaId } })
  return res.data
}

export async function unlockTalentNode(metaId: number, nodePath: string): Promise<TalentNode> {
  const res = await http.post<TalentNode>('/api/talentTree/unlockNode', { metaId, nodePath })
  return res.data
}

// ========== 段位 ==========
export async function getPlayerRank(userId: number): Promise<PlayerRank> {
  const res = await http.get<PlayerRank>('/api/rank/getRank', { params: { userId } })
  return res.data
}

// ========== 结算 ==========
export async function getSettlement(sessionId: string): Promise<SettlementData> {
  const res = await http.post<SettlementData>('/api/settlementNarrative/generateSettlement', { sessionId: Number(sessionId) })
  return res.data
}

// ========== 属性分配 ==========
export async function allocateAttributes(userId: number, allocations: AttributeAllocation[]): Promise<PlayerMeta> {
  // 将数组格式 [{ attribute, points }] 转换为后端期望的 Dictionary 格式 { attribute: points }
  const dict: Record<string, number> = {}
  for (const a of allocations) {
    dict[a.attribute] = a.points
  }
  const res = await http.post<PlayerMeta>('/api/metaProgression/allocateAttributePoints', { userId, allocations: dict })
  return res.data
}

// ========== 断线续玩 ==========
export async function checkActiveSession(userId: number): Promise<ActiveSessionResult | null> {
  const res = await http.get<ActiveSessionResult | null>('/api/dungeonExplore/checkActiveSession', { params: { userId } })
  return res.data
}

// ========== [测试] 重建角色信息（修复历史数据 + 重建技能） ==========
export async function reinitCharacter(sessionId: string): Promise<string> {
  const res = await http.post<string>('/api/character/reinitCharacter', { sessionId: Number(sessionId) })
  return res.data
}

// ========== 背包管理 ==========
export async function getBackpack(sessionId: string): Promise<BackpackStatus> {
  const res = await http.get<BackpackStatus>('/api/inventory/getBackpack', { params: { sessionId } })
  return res.data
}

export async function equipItem(sessionId: string, itemId: number) {
  const res = await http.post('/api/inventory/equipItem', { sessionId, itemId })
  return res.data
}

export async function unequipItem(sessionId: string, itemId: number) {
  const res = await http.post('/api/inventory/unequipItem', { sessionId, itemId })
  return res.data
}

export async function dropItem(sessionId: string, itemId: number, quantity = 1) {
  const res = await http.post('/api/inventory/dropItem', { sessionId, itemId, quantity })
  return res.data
}

export default http
