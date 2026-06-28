import service from '/@/utils/request';

/**
 * 游戏管理后台 API 服务
 */

// ==================== AI模型配置 ====================
export class AiModelConfigApi {
	/** 获取所有AI角色模型配置列表 */
	static getModelConfigs() {
		return service.get('/api/aiModelConfig/getModelConfigs');
	}

	/** 更新指定AI角色的模型配置 */
	static updateModelConfig(aiRole: string, data: any) {
		return service.post('/api/aiModelConfig/updateModelConfig', { ...data, aiRole });
	}

	/** 测试指定AI角色的连通性 */
	static testConnection(aiRole: string) {
		return service.post('/api/aiModelConfig/testConnection', { aiRole });
	}

	/** 获取可用模型列表 */
	static getAvailableModels() {
		return service.get('/api/aiModelConfig/getAvailableModels');
	}
}

// ==================== 副本模板管理 ====================
export class DungeonTemplateApi {
	/** 获取副本模板分页列表 */
	static getTemplateList(params: { pageIndex: number; pageSize: number; keyword?: string }) {
		return service.get('/api/dungeonTemplate/templateList', { params });
	}

	/** 获取副本模板详情 */
	static getTemplateDetail(id: string) {
		return service.get('/api/dungeonTemplate/templateDetail', { params: { id } });
	}

	/** 创建副本模板 */
	static createTemplate(data: any) {
		return service.post('/api/dungeonTemplate/createTemplate', data);
	}

	/** 更新副本模板 */
	static updateTemplate(id: string, data: any) {
		return service.post('/api/dungeonTemplate/updateTemplate', { ...data, id });
	}

	/** 删除副本模板 */
	static deleteTemplate(id: string) {
		return service.post('/api/dungeonTemplate/deleteTemplate', { id });
	}

	/** 获取难度统计 */
	static getDifficultyStats() {
		return service.get('/api/dungeonTemplate/getDifficultyStats');
	}
}

// ==================== 游戏参数配置 ====================
export class GameParameterApi {
	/** 获取当前游戏参数配置 */
	static getGameOptions() {
		return service.get('/api/gameParameter/getGameOptions');
	}

	/** 更新游戏参数配置 */
	static updateGameOptions(data: any) {
		return service.post('/api/gameParameter/updateGameOptions', data);
	}

	/** 重置为默认配置 */
	static resetToDefault() {
		return service.post('/api/gameParameter/resetToDefault');
	}
}

// ==================== Token消耗统计 ====================
export class TokenUsageApi {
	/** 获取使用量摘要 */
	static getUsageSummary(params: { startDate?: string; endDate?: string }) {
		return service.get('/api/tokenUsage/usageSummary', { params });
	}

	/** 按模型统计 */
	static getUsageByModel(params: { startDate?: string; endDate?: string }) {
		return service.get('/api/tokenUsage/usageByModel', { params });
	}

	/** 按AI类型统计 */
	static getUsageByAiType(params: { startDate?: string; endDate?: string }) {
		return service.get('/api/tokenUsage/usageByAiType', { params });
	}

	/** 获取使用趋势 */
	static getUsageTrend(days: number = 30) {
		return service.get(`/api/tokenUsage/usageTrend?days=${days}`);
	}

	/** 获取费用预估 */
	static getCostEstimate() {
		return service.get('/api/tokenUsage/costEstimate');
	}

	/** 获取错误率 */
	static getErrorRate(params: { startDate?: string; endDate?: string }) {
		return service.get('/api/tokenUsage/errorRate', { params });
	}
}

// ==================== 游戏监控 ====================
export class GameMonitorApi {
	/** 获取总览数据 */
	static getOverview() {
		return service.get('/api/gameMonitor/overview');
	}

	/** 获取活跃会话列表 */
	static getActiveSessions() {
		return service.get('/api/gameMonitor/activeSessions');
	}

	/** 获取会话详情 */
	static getSessionDetail(sessionId: string) {
		return service.get(`/api/gameMonitor/sessionDetail?sessionId=${sessionId}`);
	}

	/** 获取每日统计 */
	static getDailyStats(date?: string) {
		return service.get('/api/gameMonitor/dailyStats', { params: { date } });
	}
}
