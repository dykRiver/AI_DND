<template>
	<div class="ddbot-token-stats" v-loading="state.loading">
		<!-- 统计卡片 -->
		<el-row :gutter="20" class="summary-cards">
			<el-col :span="6">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-icon" style="background: #409eff">
						<el-icon :size="40"><Connection /></el-icon>
					</div>
					<div class="stat-content">
						<div class="stat-value">{{ state.summary.totalCalls || 0 }}</div>
						<div class="stat-label">总调用次数</div>
					</div>
				</el-card>
			</el-col>
			<el-col :span="6">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-icon" style="background: #67c23a">
						<el-icon :size="40"><Coin /></el-icon>
					</div>
					<div class="stat-content">
						<div class="stat-value">{{ formatNumber(state.summary.totalTokens) || 0 }}</div>
						<div class="stat-label">总Token数</div>
					</div>
				</el-card>
			</el-col>
			<el-col :span="6">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-icon" style="background: #e6a23c">
						<el-icon :size="40"><Money /></el-icon>
					</div>
					<div class="stat-content">
						<div class="stat-value">¥{{ (state.summary.totalCost || 0).toFixed(4) }}</div>
						<div class="stat-label">预估成本</div>
					</div>
				</el-card>
			</el-col>
			<el-col :span="6">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-icon" style="background: #f56c6c">
						<el-icon :size="40"><SuccessFilled /></el-icon>
					</div>
					<div class="stat-content">
						<div class="stat-value">{{ (state.summary.successRate || 0).toFixed(1) }}%</div>
						<div class="stat-label">成功率</div>
					</div>
				</el-card>
			</el-col>
		</el-row>

		<!-- 筛选条件 -->
		<el-card shadow="hover" class="filter-card">
			<el-form :model="state.queryParams" inline>
				<el-form-item label="日期范围">
					<el-date-picker
						v-model="state.dateRange"
						type="daterange"
						range-separator="至"
						start-placeholder="开始日期"
						end-placeholder="结束日期"
						value-format="YYYY-MM-DD"
						:shortcuts="dateShortcuts"
					/>
				</el-form-item>
				<el-form-item label="统计粒度">
					<el-radio-group v-model="state.queryParams.granularity">
						<el-radio-button label="day">按天</el-radio-button>
						<el-radio-button label="hour">按小时</el-radio-button>
					</el-radio-group>
				</el-form-item>
				<el-form-item label="模型">
					<el-select v-model="state.queryParams.modelName" placeholder="全部模型" clearable>
						<el-option label="全部模型" value="" />
						<el-option v-for="model in state.modelList" :key="model" :label="model" :value="model" />
					</el-select>
				</el-form-item>
				<el-form-item label="接口类型">
					<el-select v-model="state.queryParams.apiType" placeholder="全部接口" clearable>
						<el-option label="全部接口" value="" />
						<el-option label="会话列表识别" value="recognize" />
						<el-option label="消息分析" value="analyze" />
					</el-select>
				</el-form-item>
				<el-form-item>
					<el-button type="primary" icon="Search" @click="handleQuery">查询</el-button>
					<el-button icon="Refresh" @click="resetQuery">重置</el-button>
				</el-form-item>
			</el-form>
		</el-card>

		<!-- 图表区域 -->
		<el-row :gutter="20" class="charts-row">
			<el-col :span="12">
				<el-card shadow="hover">
					<template #header>
						<div class="card-header">
							<span>Token使用趋势</span>
						</div>
					</template>
					<div ref="tokenTrendChartRef" style="height: 350px"></div>
				</el-card>
			</el-col>
			<el-col :span="12">
				<el-card shadow="hover">
					<template #header>
						<div class="card-header">
							<span>成本分布</span>
						</div>
					</template>
					<div ref="costChartRef" style="height: 350px"></div>
				</el-card>
			</el-col>
		</el-row>

		<!-- 详细数据表格 -->
		<el-card shadow="hover" class="table-card">
			<template #header>
				<div class="card-header">
					<span>详细统计数据</span>
					<el-button type="primary" icon="Download" size="small" @click="exportData">导出数据</el-button>
				</div>
			</template>
			<el-table :data="paginatedData" border stripe>
				<el-table-column prop="dateTime" label="日期时间" width="180" align="center" />
				<el-table-column prop="modelName" label="模型" width="180" align="center" />
				<el-table-column prop="apiType" label="接口类型" width="120" align="center">
					<template #default="scope">
						<el-tag v-if="scope.row.apiType === 'recognize'" type="success">会话识别</el-tag>
						<el-tag v-else-if="scope.row.apiType === 'analyze'" type="warning">消息分析</el-tag>
						<el-tag v-else>{{ scope.row.apiType }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="callCount" label="调用次数" width="100" align="center" />
				<el-table-column prop="promptTokens" label="输入Token" width="120" align="center">
					<template #default="scope">
						{{ formatNumber(scope.row.promptTokens) }}
					</template>
				</el-table-column>
				<el-table-column prop="completionTokens" label="输出Token" width="120" align="center">
					<template #default="scope">
						{{ formatNumber(scope.row.completionTokens) }}
					</template>
				</el-table-column>
				<el-table-column prop="totalTokens" label="总Token" width="120" align="center">
					<template #default="scope">
						{{ formatNumber(scope.row.totalTokens) }}
					</template>
				</el-table-column>
				<el-table-column prop="cost" label="成本(元)" width="120" align="center">
					<template #default="scope">
						¥{{ (scope.row.cost || 0).toFixed(4) }}
					</template>
				</el-table-column>
				<el-table-column prop="avgTimeMs" label="平均耗时" width="120" align="center">
					<template #default="scope">
						{{ scope.row.avgTimeMs }}ms
					</template>
				</el-table-column>
			</el-table>
			
			<!-- 分页组件 -->
			<el-pagination
				:current-page="state.pagination.currentPage"
				:page-size="state.pagination.pageSize"
				:page-sizes="[10, 20, 50, 100]"
				:total="state.statsData.length"
				layout="total, sizes, prev, pager, next, jumper"
				@size-change="handleSizeChange"
				@current-change="handleCurrentChange"
				class="pagination-container"
			/>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="ddbotTokenStats">
import { ref, reactive, onMounted, onUnmounted, computed } from 'vue';
import { ElMessage } from 'element-plus';
import * as echarts from 'echarts';
import { Connection, Coin, Money, SuccessFilled, Search, Refresh, Download } from '@element-plus/icons-vue';
import { DDBotTokenApi } from '/@/api-services/api';

const tokenTrendChartRef = ref<HTMLElement>();
const costChartRef = ref<HTMLElement>();
let tokenTrendChart: echarts.ECharts | null = null;
let costChart: echarts.ECharts | null = null;

const state = reactive({
	loading: false,
	dateRange: [] as string[],
	queryParams: {
		startDate: '',
		endDate: '',
		granularity: 'day',
		modelName: '',
		apiType: '',
	},
	summary: {
		totalCalls: 0,
		totalTokens: 0,
		totalPromptTokens: 0,
		totalCompletionTokens: 0,
		totalCost: 0,
		successCount: 0,
		failedCount: 0,
		successRate: 0,
	},
	statsData: [] as any[],
	modelList: ['qwen-turbo', 'qwen-plus', 'qwen3.5-plus', 'qwen-vl-ocr-latest'],
	pagination: {
		currentPage: 1,
		pageSize: 20,
	},
});

// 计算分页数据
const paginatedData = computed(() => {
	const start = (state.pagination.currentPage - 1) * state.pagination.pageSize;
	const end = start + state.pagination.pageSize;
	return state.statsData.slice(start, end);
});

// 日期快捷选项
const dateShortcuts = [
	{
		text: '最近7天',
		value: () => {
			const end = new Date();
			const start = new Date();
			start.setTime(start.getTime() - 3600 * 1000 * 24 * 7);
			return [start, end];
		},
	},
	{
		text: '最近30天',
		value: () => {
			const end = new Date();
			const start = new Date();
			start.setTime(start.getTime() - 3600 * 1000 * 24 * 30);
			return [start, end];
		},
	},
	{
		text: '本月',
		value: () => {
			const end = new Date();
			const start = new Date();
			start.setDate(1);
			return [start, end];
		},
	},
];

// 初始化图表
const initCharts = () => {
	if (tokenTrendChartRef.value) {
		tokenTrendChart = echarts.init(tokenTrendChartRef.value);
	}
	if (costChartRef.value) {
		costChart = echarts.init(costChartRef.value);
	}
};

// 渲染Token趋势图
const renderTokenTrendChart = (data: any[]) => {
	if (!tokenTrendChart) return;

	const dates = data.map((item) => item.dateTime);
	const tokens = data.map((item) => item.totalTokens);
	const calls = data.map((item) => item.callCount);

	const option = {
		tooltip: {
			trigger: 'axis',
			axisPointer: {
				type: 'shadow',
			},
		},
		legend: {
			data: ['Token数', '调用次数'],
		},
		grid: {
			left: '3%',
			right: '4%',
			bottom: '3%',
			containLabel: true,
		},
		xAxis: {
			type: 'category',
			data: dates,
			axisLabel: {
				rotate: 45,
			},
		},
		yAxis: [
			{
				type: 'value',
				name: 'Token数',
			},
			{
				type: 'value',
				name: '调用次数',
			},
		],
		series: [
			{
				name: 'Token数',
				type: 'bar',
				data: tokens,
				itemStyle: {
					color: '#409eff',
				},
			},
			{
				name: '调用次数',
				type: 'line',
				yAxisIndex: 1,
				data: calls,
				itemStyle: {
					color: '#67c23a',
				},
			},
		],
	};

	tokenTrendChart.setOption(option);
};

// 渲染成本分布图
const renderCostChart = (data: any[]) => {
	if (!costChart) return;

	// 按模型聚合成本
	const modelCostMap = new Map<string, number>();
	data.forEach((item) => {
		const cost = item.cost || 0;
		modelCostMap.set(item.modelName, (modelCostMap.get(item.modelName) || 0) + cost);
	});

	const pieData = Array.from(modelCostMap.entries()).map(([name, value]) => ({
		name,
		value: value.toFixed(4),
	}));

	const option = {
		tooltip: {
			trigger: 'item',
			formatter: '{b}: ¥{c} ({d}%)',
		},
		legend: {
			orient: 'vertical',
			left: 'left',
		},
		series: [
			{
				name: '成本分布',
				type: 'pie',
				radius: '50%',
				data: pieData,
				emphasis: {
					itemStyle: {
						shadowBlur: 10,
						shadowOffsetX: 0,
						shadowColor: 'rgba(0, 0, 0, 0.5)',
					},
				},
			},
		],
	};

	costChart.setOption(option);
};

// 查询统计数据
const handleQuery = async () => {
	state.loading = true;
	try {
		// 设置日期范围
		if (state.dateRange && state.dateRange.length === 2) {
			state.queryParams.startDate = state.dateRange[0];
			state.queryParams.endDate = state.dateRange[1];
		} else {
			// 默认查询最近7天
			const end = new Date();
			const start = new Date();
			start.setTime(start.getTime() - 3600 * 1000 * 24 * 7);
			state.queryParams.startDate = start.toISOString().split('T')[0];
			state.queryParams.endDate = end.toISOString().split('T')[0];
		}

		// 调用后端API
		const res: any = await DDBotTokenApi.queryTokenStats(state.queryParams);
		
		if (res.code === 200 && res.data) {
			state.statsData = res.data.data || [];
			state.summary = res.data.summary || {};
			
			// 提取模型列表
			const models = new Set<string>();
			state.statsData.forEach((item: any) => {
				if (item.modelName) {
					models.add(item.modelName);
				}
			});
			if (models.size > 0) {
				state.modelList = Array.from(models);
			}
		} else {
			ElMessage.warning(res.message || '查询失败');
			state.statsData = [];
			state.summary = {
				totalCalls: 0,
				totalTokens: 0,
				totalPromptTokens: 0,
				totalCompletionTokens: 0,
				totalCost: 0,
				successCount: 0,
				failedCount: 0,
				successRate: 0,
			};
		}

		// 渲染图表
		renderTokenTrendChart(state.statsData);
		renderCostChart(state.statsData);
	} catch (error: any) {
		ElMessage.error('查询失败: ' + (error.message || '未知错误'));
		state.statsData = [];
		state.summary = {
			totalCalls: 0,
			totalTokens: 0,
			totalPromptTokens: 0,
			totalCompletionTokens: 0,
			totalCost: 0,
			successCount: 0,
			failedCount: 0,
			successRate: 0,
		};
	} finally {
		state.loading = false;
	}
};

// 重置查询
const resetQuery = () => {
	state.dateRange = [];
	state.queryParams = {
		startDate: '',
		endDate: '',
		granularity: 'day',
		modelName: '',
		apiType: '',
	};
	state.pagination.currentPage = 1; // 重置分页
	handleQuery();
};

// 分页大小变化
const handleSizeChange = (val: number) => {
	state.pagination.pageSize = val;
	state.pagination.currentPage = 1; // 重置到第一页
};

// 当前页变化
const handleCurrentChange = (val: number) => {
	state.pagination.currentPage = val;
};

// 导出数据
const exportData = () => {
	ElMessage.success('导出功能开发中...');
	// TODO: 实现导出Excel功能
};

// 格式化数字
const formatNumber = (num: number) => {
	if (!num) return '0';
	return num.toLocaleString();
};

// 窗口大小变化时重新渲染图表
const handleResize = () => {
	tokenTrendChart?.resize();
	costChart?.resize();
};

onMounted(() => {
	initCharts();
	handleQuery();
	window.addEventListener('resize', handleResize);
});

onUnmounted(() => {
	tokenTrendChart?.dispose();
	costChart?.dispose();
	window.removeEventListener('resize', handleResize);
});
</script>

<style scoped lang="scss">
.ddbot-token-stats {
	padding: 20px;

	.summary-cards {
		margin-bottom: 20px;

		.stat-card {
			display: flex;
			align-items: center;
			padding: 20px;

			.stat-icon {
				width: 80px;
				height: 80px;
				border-radius: 10px;
				display: flex;
				align-items: center;
				justify-content: center;
				color: #fff;
				margin-right: 20px;
			}

			.stat-content {
				flex: 1;

				.stat-value {
					font-size: 28px;
					font-weight: bold;
					color: #303133;
					margin-bottom: 5px;
				}

				.stat-label {
					font-size: 14px;
					color: #909399;
				}
			}
		}
	}

	.filter-card {
		margin-bottom: 20px;
	}

	.charts-row {
		margin-bottom: 20px;

		.card-header {
			display: flex;
			align-items: center;
			justify-content: space-between;
			font-size: 16px;
			font-weight: bold;
		}
	}

	.table-card {
		.card-header {
			display: flex;
			align-items: center;
			justify-content: space-between;
			font-size: 16px;
			font-weight: bold;
		}
		
		.pagination-container {
			margin-top: 20px;
			display: flex;
			justify-content: flex-end;
		}
	}
}
</style>
