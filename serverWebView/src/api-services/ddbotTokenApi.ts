import service from '/@/utils/request';

/**
 * DDBot Token统计API服务
 */
export class DDBotTokenApi {
	/**
	 * 记录Token使用(内部接口,通常由后端服务调用)
	 */
	static recordTokenUsage(data: any) {
		return service.post('/api/dDBotTokenUsage/recordTokenUsage', data);
	}

	/**
	 * 查询Token统计数据
	 */
	static queryTokenStats(data: any) {
		return service.post('/api/dDBotTokenUsage/stats', data);
	}

	/**
	 * 查询Token使用明细(分页)
	 */
	static queryTokenDetails(data: any) {
		return service.post('/api/dDBotTokenUsage/details', data);
	}

	/**
	 * 获取模型单价列表
	 */
	static getModelPrices() {
		return service.post('/api/dDBotTokenUsage/modelPrices');
	}

	/**
	 * 保存模型单价
	 */
	static saveModelPrice(data: any) {
		return service.post('/api/dDBotTokenUsage/saveModelPrice', data);
	}

	/**
	 * 删除模型单价
	 */
	static deleteModelPrice(id: number) {
		return service.delete(`/api/dDBotTokenUsage/modelPrice/${id}`);
	}
}
