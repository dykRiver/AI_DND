<template>
	<div class="game-ai-config-container" v-loading="state.loading">
		<el-card shadow="hover">
			<template #header>
				<div class="card-header">
					<span>AI模型配置</span>
					<el-button type="primary" icon="Refresh" @click="loadData">刷新</el-button>
				</div>
			</template>
			<el-table :data="state.configList" border stripe>
				<el-table-column prop="aiRole" label="AI角色" width="150" align="center">
					<template #default="scope">
						<el-tag :type="getRoleTagType(scope.row.aiRole)">{{ getRoleName(scope.row.aiRole) }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="modelId" label="当前模型" min-width="200" align="center" />
				<el-table-column prop="maxTokens" label="MaxTokens" width="120" align="center" />
				<el-table-column prop="temperature" label="Temperature" width="130" align="center">
					<template #default="scope">
						{{ scope.row.temperature?.toFixed(2) }}
					</template>
				</el-table-column>
				<el-table-column label="思考模式" width="120" align="center">
					<template #default="scope">
						<el-tag :type="scope.row.enableThinking ? 'success' : 'info'" size="small">
							{{ scope.row.enableThinking ? '已开启' : '未开启' }}
						</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="thinkingBudget" label="思考预算" width="120" align="center">
					<template #default="scope">
						{{ scope.row.enableThinking ? scope.row.thinkingBudget : '-' }}
					</template>
				</el-table-column>
				<el-table-column label="操作" width="200" align="center" fixed="right">
					<template #default="scope">
						<el-button type="primary" size="small" icon="Edit" @click="handleEdit(scope.row)">编辑</el-button>
						<el-button type="warning" size="small" icon="Connection" :loading="scope.row._testing" @click="handleTest(scope.row)">测试</el-button>
					</template>
				</el-table-column>
			</el-table>
		</el-card>

		<!-- 编辑弹窗 -->
		<el-dialog v-model="state.editVisible" title="编辑模型配置" width="500px" destroy-on-close>
			<el-form :model="state.editForm" label-width="120px">
				<el-form-item label="AI角色">
					<el-tag :type="getRoleTagType(state.editForm.aiRole)">{{ getRoleName(state.editForm.aiRole) }}</el-tag>
				</el-form-item>
				<el-form-item label="模型选择">
					<el-select v-model="state.editForm.modelId" placeholder="请选择模型" style="width: 100%">
						<el-option v-for="model in state.availableModels" :key="model" :label="model" :value="model" />
					</el-select>
				</el-form-item>
				<el-form-item label="MaxTokens">
					<el-input-number v-model="state.editForm.maxTokens" :min="100" :max="128000" :step="100" style="width: 100%" />
				</el-form-item>
				<el-form-item label="Temperature">
					<el-slider v-model="state.editForm.temperature" :min="0" :max="2" :step="0.01" show-input />
				</el-form-item>
				<el-form-item label="思考模式">
					<el-switch v-model="state.editForm.enableThinking" active-text="开启" inactive-text="关闭" />
					<span v-if="state.editForm.enableThinking" style="margin-left: 12px; color: #909399; font-size: 12px;">AI先深度推理再输出，质量更高但响应更慢</span>
				</el-form-item>
				<el-form-item v-if="state.editForm.enableThinking" label="思考预算">
					<el-input-number v-model="state.editForm.thinkingBudget" :min="512" :max="32768" :step="512" style="width: 100%" />
					<span style="color: #909399; font-size: 12px;">思考过程最大Token数，建议1024-16384</span>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="handleSave">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="gameAiConfig">
import { reactive, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { AiModelConfigApi } from '/@/api-services/api';

const state = reactive({
	loading: false,
	saving: false,
	configList: [] as any[],
	availableModels: [] as string[],
	editVisible: false,
	editForm: {
		aiRole: '',
		modelId: '',
		maxTokens: 4096,
		temperature: 0.7,
		enableThinking: false,
		thinkingBudget: 4096,
	},
});

const roleNameMap: Record<string, string> = {
	Classifier: '分类器',
	Director: '导演',
	Narrator: '叙事者',
	Architect: '建筑师',
};

const getRoleName = (role: string) => roleNameMap[role] || role;

const getRoleTagType = (role: string) => {
	const map: Record<string, string> = { Classifier: '', Director: 'success', Narrator: 'warning', Architect: 'danger' };
	return map[role] || 'info';
};

const loadData = async () => {
	state.loading = true;
	try {
		const [configRes, modelsRes] = await Promise.all([AiModelConfigApi.getModelConfigs(), AiModelConfigApi.getAvailableModels()]);
		state.configList = (configRes.data?.result || configRes.data?.data || []).map((item: any) => ({ ...item, _testing: false }));
		state.availableModels = modelsRes.data?.result || modelsRes.data?.data || [];
	} catch (error: any) {
		ElMessage.error('加载配置失败: ' + (error.message || '未知错误'));
	} finally {
		state.loading = false;
	}
};

const handleEdit = (row: any) => {
	state.editForm = {
		aiRole: row.aiRole,
		modelId: row.modelId,
		maxTokens: row.maxTokens,
		temperature: row.temperature,
		enableThinking: row.enableThinking || false,
		thinkingBudget: row.thinkingBudget || 4096,
	};
	state.editVisible = true;
};

const handleSave = async () => {
	state.saving = true;
	try {
		await AiModelConfigApi.updateModelConfig(state.editForm.aiRole, {
			modelId: state.editForm.modelId,
			maxTokens: state.editForm.maxTokens,
			temperature: state.editForm.temperature,
			enableThinking: state.editForm.enableThinking,
			thinkingBudget: state.editForm.thinkingBudget,
		});
		ElMessage.success('保存成功');
		state.editVisible = false;
		await loadData();
	} catch (error: any) {
		ElMessage.error('保存失败: ' + (error.message || '未知错误'));
	} finally {
		state.saving = false;
	}
};

const handleTest = async (row: any) => {
	row._testing = true;
	try {
		const res = await AiModelConfigApi.testConnection(row.aiRole);
		const data = res.data?.result || res.data?.data;
		if (data?.success) {
			ElMessage.success(`连通性测试成功，延迟: ${data.latencyMs}ms`);
		} else {
			ElMessage.error(`连通性测试失败: ${data?.error || '未知错误'}`);
		}
	} catch (error: any) {
		ElMessage.error('测试连通性失败: ' + (error.message || '未知错误'));
	} finally {
		row._testing = false;
	}
};

onMounted(() => {
	loadData();
});
</script>

<style scoped lang="scss">
.game-ai-config-container {
	padding: 20px;

	.card-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		font-size: 16px;
		font-weight: bold;
	}
}
</style>
