<template>
	<div class="game-token-stats-container" v-loading="state.loading">
		<!-- 顶部统计卡片 -->
		<el-row :gutter="20" class="summary-cards">
			<el-col :span="6">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-icon" style="background: #409eff">
						<el-icon :size="36"><Coin /></el-icon>
					</div>
					<div class="stat-content">
						<div class="stat-value">{{ formatNumber(state.summary.totalTokens) }}</div>
						<div class="stat-label">总Token数</div>
					</div>
				</el-card>
			</el-col>
			<el-col :span="6">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-icon" style="background: #e6a23c">
						<el-icon :size="36"><Money /></el-icon>
					</div>
					<div class="stat-content">
						<div class="stat-value">¥{{ (state.summary.totalCost || 0).toFixed(2) }}</div>
						<div class="stat-label">总费用</div>
					</div>
				</el-card>
			</el-col>
			<el-col :span="6">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-icon" style="background: #67c23a">
						<el-icon :size="36"><Connection /></el-icon>
					</div>
					<div class="stat-content">
						<div class="stat-value">{{ formatNumber(state.summary.totalCalls) }}</div>
						<div class="stat-label">总调用次数</div>
					</div>
				</el-card>
			</el-col>
			<el-col :span="6">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-icon" style="background: #f56c6c">
						<el-icon :size="36"><TrendCharts /></el-icon>
					</div>
					<div class="stat-content">
						<div class="stat-value">¥{{ (state.costEstimate.monthlyEstimate || 0).toFixed(2) }}</div>
						<div class="stat-label">月预估费用</div>
					</div>
				</el-card>
			</el-col>
		</el-row>

		<!-- 日期筛选 -->
		<el-card shadow="hover" class="filter-card">
			<el-form inline>
				<el-form-item label="日期范围">
					<el-date-picker
						v-model="state.dateRange"
						type="daterange"
						range-separator="至"
						start-placeholder="开始日期"
						end-placeholder="结束日期"
						value-format="YYYY-MM-DD"
						:shortcuts="dateShortcuts"
						@change="handleDateChange"
					/>
				</el-form-item>
				<el-form-item>
					<el-button type="primary" icon="Search" @click="loadAllData">查询</el-button>
				</el-form-item>
			</el-form>
		</el-card>

		<!-- 图表区域 -->
		<el-row :gutter="20" class="charts-row">
			<el-col :span="14">
				<el-card shadow="hover">
					<template #header>
						<span class="card-title">每日Token消耗趋势（最近30天）</span>
					</template>
					<div ref="trendChartRef" style="height: 350px"></div>
				</el-card>
			</el-col>
			<el-col :span="10">
				<el-card shadow="hover">
					<template #header>
						<span class="card-title">按AI角色Token分布</span>
					</template>
					<div ref="pieChartRef" style="height: 350px"></div>
				</el-card>
			</el-col>
		</el-row>

		<!-- 按模型明细统计表格 -->
		<el-card shadow="hover" class="table-card">
			<template #header>
				<span class="card-title">按模型明细统计</span>
			</template>
			<el-table :data="state.modelStats" border stripe>
				<el-table-column prop="modelId" label="模型" min-width="180" />
				<el-table-column prop="totalTokens" label="总Token" width="130" align="center">
					<template #default="scope">{{ formatNumber(scope.row.totalTokens) }}</template>
				</el-table-column>
				<el-table-column prop="promptTokens" label="输入Token" width="130" align="center">
					<template #default="scope">{{ formatNumber(scope.row.promptTokens) }}</template>
				</el-table-column>
				<el-table-column prop="completionTokens" label="输出Token" width="130" align="center">
					<template #default="scope">{{ formatNumber(scope.row.completionTokens) }}</template>
				</el-table-column>
				<el-table-column prop="callCount" label="调用次数" width="100" align="center" />
				<el-table-column prop="totalCost" label="费用(元)" width="120" align="center">
					<template #default="scope">¥{{ (scope.row.totalCost || 0).toFixed(4) }}</template>
				</el-table-column>
				<el-table-column prop="errorRate" label="错误率" width="100" align="center">
					<template #default="scope">
						<el-tag :type="scope.row.errorRate > 5 ? 'danger' : 'success'">{{ (scope.row.errorRate || 0).toFixed(1) }}%</el-tag>
					</template>
				</el-table-column>
			</el-table>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="gameTokenStats">
import { reactive, ref, onMounted, onUnmounted } from 'vue';
import { ElMessage } from 'element-plus';
import * as echarts from 'echarts';
import { Coin, Money, Connection, TrendCharts } from '@element-plus/icons-vue';
import { TokenUsageApi } from '/@/api-services/api';

const trendChartRef = ref<HTMLElement>();
const pieChartRef = ref<HTMLElement>();
let trendChart: echarts.ECharts | null = null;
let pieChart: echarts.ECharts | null = null;

const state = reactive({
	loading: false,
	dateRange: [] as string[],
	summary: {
		totalTokens: 0,
		totalCost: 0,
		totalCalls: 0,
	},
	costEstimate: {
		monthlyEstimate: 0,
	},
	modelStats: [] as any[],
	trendData: [] as any[],
	aiTypeData: [] as any[],
});

const dateShortcuts = [
	{ text: '最近7天', value: () => { const e = new Date(); const s = new Date(); s.setDate(s.getDate() - 7); return [s, e]; } },
	{ text: '最近30天', value: () => { const e = new Date(); const s = new Date(); s.setDate(s.getDate() - 30); return [s, e]; } },
	{ text: '本月', value: () => { const e = new Date(); const s = new Date(); s.setDate(1); return [s, e]; } },
];

const formatNumber = (num: number) => {
	if (!num) return '0';
	return num.toLocaleString();
};

const getDateParams = () => {
	if (state.dateRange && state.dateRange.length === 2) {
		return { startDate: state.dateRange[0], endDate: state.dateRange[1] };
	}
	return { startDate: '', endDate: '' };
};

const handleDateChange = () => {
	loadAllData();
};

const loadAllData = async () => {
	state.loading = true;
	try {
		const params = getDateParams();
		const [summaryRes, modelRes, aiTypeRes, trendRes, costRes] = await Promise.all([
			TokenUsageApi.getUsageSummary(params),
			TokenUsageApi.getUsageByModel(params),
			TokenUsageApi.getUsageByAiType(params),
			TokenUsageApi.getUsageTrend(30),
			TokenUsageApi.getCostEstimate(),
		]);

		state.summary = summaryRes.data?.result || summaryRes.data?.data || {};
		state.modelStats = modelRes.data?.result || modelRes.data?.data || [];
		state.aiTypeData = aiTypeRes.data?.result || aiTypeRes.data?.data || [];
		state.trendData = trendRes.data?.result || trendRes.data?.data || [];
		state.costEstimate = costRes.data?.result || costRes.data?.data || {};

		renderTrendChart();
		renderPieChart();
	} catch (error: any) {
		ElMessage.error('加载数据失败: ' + (error.message || '未知错误'));
	} finally {
		state.loading = false;
	}
};

const initCharts = () => {
	if (trendChartRef.value) trendChart = echarts.init(trendChartRef.value);
	if (pieChartRef.value) pieChart = echarts.init(pieChartRef.value);
};

const renderTrendChart = () => {
	if (!trendChart) return;
	const dates = state.trendData.map((item: any) => item.date);
	const tokens = state.trendData.map((item: any) => item.totalTokens);

	trendChart.setOption({
		tooltip: { trigger: 'axis' },
		grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
		xAxis: { type: 'category', data: dates, axisLabel: { rotate: 45 } },
		yAxis: { type: 'value', name: 'Token数' },
		series: [{ name: 'Token消耗', type: 'line', data: tokens, smooth: true, areaStyle: { opacity: 0.3 }, itemStyle: { color: '#409eff' } }],
	});
};

const renderPieChart = () => {
	if (!pieChart) return;
	const pieData = state.aiTypeData.map((item: any) => ({ name: item.aiType || item.aiRole, value: item.totalTokens }));

	pieChart.setOption({
		tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
		legend: { orient: 'vertical', left: 'left' },
		series: [{ name: 'Token分布', type: 'pie', radius: ['40%', '70%'], data: pieData, emphasis: { itemStyle: { shadowBlur: 10, shadowOffsetX: 0, shadowColor: 'rgba(0, 0, 0, 0.5)' } } }],
	});
};

const handleResize = () => {
	trendChart?.resize();
	pieChart?.resize();
};

onMounted(() => {
	initCharts();
	loadAllData();
	window.addEventListener('resize', handleResize);
});

onUnmounted(() => {
	trendChart?.dispose();
	pieChart?.dispose();
	window.removeEventListener('resize', handleResize);
});
</script>

<style scoped lang="scss">
.game-token-stats-container {
	padding: 20px;

	.summary-cards {
		margin-bottom: 20px;

		.stat-card {
			display: flex;
			align-items: center;
			padding: 20px;

			.stat-icon {
				width: 70px;
				height: 70px;
				border-radius: 10px;
				display: flex;
				align-items: center;
				justify-content: center;
				color: #fff;
				margin-right: 15px;
			}

			.stat-content {
				flex: 1;

				.stat-value {
					font-size: 24px;
					font-weight: bold;
					color: #303133;
				}

				.stat-label {
					font-size: 13px;
					color: #909399;
					margin-top: 4px;
				}
			}
		}
	}

	.filter-card {
		margin-bottom: 20px;
	}

	.charts-row {
		margin-bottom: 20px;
	}

	.card-title {
		font-size: 15px;
		font-weight: bold;
	}

	.table-card {
		margin-bottom: 20px;
	}
}
</style>
