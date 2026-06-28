<template>
	<div class="game-monitor-container" v-loading="state.loading">
		<!-- 总览卡片 -->
		<el-row :gutter="20" class="overview-cards">
			<el-col :span="4" v-for="(item, index) in overviewCards" :key="index">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-content">
						<div class="stat-value">{{ formatNumber(item.value) }}</div>
						<div class="stat-label">{{ item.label }}</div>
					</div>
				</el-card>
			</el-col>
		</el-row>

		<!-- 今日统计 -->
		<el-row :gutter="20" class="daily-stats">
			<el-col :span="6">
				<el-card shadow="hover">
					<el-statistic title="今日新建会话" :value="state.dailyStats.newSessions || 0" />
				</el-card>
			</el-col>
			<el-col :span="6">
				<el-card shadow="hover">
					<el-statistic title="今日完成会话" :value="state.dailyStats.completedSessions || 0" />
				</el-card>
			</el-col>
			<el-col :span="6">
				<el-card shadow="hover">
					<el-statistic title="今日放弃会话" :value="state.dailyStats.abandonedSessions || 0" />
				</el-card>
			</el-col>
			<el-col :span="6">
				<el-card shadow="hover">
					<el-statistic title="平均会话时长(分)" :value="state.dailyStats.avgDurationMinutes || 0" />
				</el-card>
			</el-col>
		</el-row>

		<!-- 活跃会话列表 -->
		<el-card shadow="hover" class="sessions-card">
			<template #header>
				<div class="card-header">
					<span>活跃会话列表</span>
					<el-button type="primary" icon="Refresh" size="small" @click="loadActiveSessions">刷新</el-button>
				</div>
			</template>
			<el-table :data="state.activeSessions" border stripe @row-click="handleRowClick" style="cursor: pointer">
				<el-table-column prop="sessionId" label="SessionId" width="180" show-overflow-tooltip />
				<el-table-column prop="userName" label="用户" width="120" align="center" />
				<el-table-column prop="dungeonName" label="副本名称" min-width="150" />
				<el-table-column prop="startTime" label="开始时间" width="170" align="center" />
				<el-table-column prop="interactionCount" label="交互次数" width="100" align="center" />
				<el-table-column prop="status" label="状态" width="100" align="center">
					<template #default="scope">
						<el-tag :type="getStatusType(scope.row.status)">{{ getStatusText(scope.row.status) }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column label="操作" width="100" align="center">
					<template #default="scope">
						<el-button type="primary" size="small" link @click.stop="showDetail(scope.row)">详情</el-button>
					</template>
				</el-table-column>
			</el-table>
		</el-card>

		<!-- 会话详情抽屉 -->
		<el-drawer v-model="state.drawerVisible" title="会话详情" size="500px">
			<div v-loading="state.detailLoading" class="session-detail">
				<el-descriptions :column="1" border>
					<el-descriptions-item label="SessionId">{{ state.sessionDetail.sessionId }}</el-descriptions-item>
					<el-descriptions-item label="用户">{{ state.sessionDetail.userName }}</el-descriptions-item>
					<el-descriptions-item label="副本名称">{{ state.sessionDetail.dungeonName }}</el-descriptions-item>
					<el-descriptions-item label="开始时间">{{ state.sessionDetail.startTime }}</el-descriptions-item>
					<el-descriptions-item label="当前天数">{{ state.sessionDetail.currentDay }}</el-descriptions-item>
					<el-descriptions-item label="当前HP">{{ state.sessionDetail.currentHp }} / {{ state.sessionDetail.maxHp }}</el-descriptions-item>
					<el-descriptions-item label="交互次数">{{ state.sessionDetail.interactionCount }}</el-descriptions-item>
					<el-descriptions-item label="总Token消耗">{{ formatNumber(state.sessionDetail.totalTokens) }}</el-descriptions-item>
					<el-descriptions-item label="状态">
						<el-tag :type="getStatusType(state.sessionDetail.status)">{{ getStatusText(state.sessionDetail.status) }}</el-tag>
					</el-descriptions-item>
				</el-descriptions>

				<el-divider>最近交互记录</el-divider>
				<el-timeline v-if="state.sessionDetail.recentMessages?.length">
					<el-timeline-item
						v-for="(msg, idx) in state.sessionDetail.recentMessages"
						:key="idx"
						:type="msg.role === 'user' ? 'primary' : 'success'"
						:timestamp="msg.timestamp"
					>
						<el-tag size="small" :type="msg.role === 'user' ? 'primary' : 'success'">{{ msg.role === 'user' ? '玩家' : 'AI' }}</el-tag>
						<p class="msg-content">{{ msg.content }}</p>
					</el-timeline-item>
				</el-timeline>
				<el-empty v-else description="暂无交互记录" />
			</div>
		</el-drawer>
	</div>
</template>

<script setup lang="ts" name="gameMonitor">
import { reactive, computed, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { GameMonitorApi } from '/@/api-services/api';

const state = reactive({
	loading: false,
	detailLoading: false,
	overview: {
		totalUsers: 0,
		totalSessions: 0,
		activeSessions: 0,
		totalAiCalls: 0,
		totalTokens: 0,
	},
	dailyStats: {
		newSessions: 0,
		completedSessions: 0,
		abandonedSessions: 0,
		avgDurationMinutes: 0,
	},
	activeSessions: [] as any[],
	drawerVisible: false,
	sessionDetail: {} as any,
});

const overviewCards = computed(() => [
	{ label: '总用户', value: state.overview.totalUsers },
	{ label: '总会话', value: state.overview.totalSessions },
	{ label: '活跃会话', value: state.overview.activeSessions },
	{ label: '总AI调用', value: state.overview.totalAiCalls },
	{ label: '总Token', value: state.overview.totalTokens },
]);

const formatNumber = (num: number) => {
	if (!num) return '0';
	return num.toLocaleString();
};

const getStatusType = (status: string) => {
	const map: Record<string, string> = { Active: 'success', Completed: '', Abandoned: 'danger', Paused: 'warning' };
	return map[status] || 'info';
};

const getStatusText = (status: string) => {
	const map: Record<string, string> = { Active: '进行中', Completed: '已完成', Abandoned: '已放弃', Paused: '已暂停' };
	return map[status] || status;
};

const loadOverview = async () => {
	try {
		const res = await GameMonitorApi.getOverview();
		state.overview = res.data?.result || res.data?.data || {};
	} catch {}
};

const loadDailyStats = async () => {
	try {
		const today = new Date().toISOString().split('T')[0];
		const res = await GameMonitorApi.getDailyStats(today);
		state.dailyStats = res.data?.result || res.data?.data || {};
	} catch {}
};

const loadActiveSessions = async () => {
	try {
		const res = await GameMonitorApi.getActiveSessions();
		state.activeSessions = res.data?.result || res.data?.data || [];
	} catch (error: any) {
		ElMessage.error('加载活跃会话失败');
	}
};

const handleRowClick = (row: any) => {
	showDetail(row);
};

const showDetail = async (row: any) => {
	state.drawerVisible = true;
	state.detailLoading = true;
	try {
		const res = await GameMonitorApi.getSessionDetail(row.sessionId);
		state.sessionDetail = res.data?.result || res.data?.data || {};
	} catch (error: any) {
		ElMessage.error('获取会话详情失败');
	} finally {
		state.detailLoading = false;
	}
};

const loadAllData = async () => {
	state.loading = true;
	await Promise.all([loadOverview(), loadDailyStats(), loadActiveSessions()]);
	state.loading = false;
};

onMounted(() => {
	loadAllData();
});
</script>

<style scoped lang="scss">
.game-monitor-container {
	padding: 20px;

	.overview-cards {
		margin-bottom: 20px;

		.stat-card {
			text-align: center;
			padding: 15px;

			.stat-content {
				.stat-value {
					font-size: 24px;
					font-weight: bold;
					color: #303133;
				}

				.stat-label {
					font-size: 13px;
					color: #909399;
					margin-top: 5px;
				}
			}
		}
	}

	.daily-stats {
		margin-bottom: 20px;
	}

	.sessions-card {
		.card-header {
			display: flex;
			align-items: center;
			justify-content: space-between;
			font-size: 16px;
			font-weight: bold;
		}
	}

	.session-detail {
		.msg-content {
			margin-top: 5px;
			color: #606266;
			font-size: 13px;
			line-height: 1.5;
			word-break: break-all;
		}
	}
}
</style>
