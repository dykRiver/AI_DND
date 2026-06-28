<template>
	<div class="game-parameters-container" v-loading="state.loading">
		<el-card shadow="hover">
			<template #header>
				<div class="card-header">
					<span>游戏参数配置</span>
					<div>
						<el-button type="warning" icon="RefreshLeft" @click="handleReset">重置为默认</el-button>
						<el-button type="primary" icon="Check" :loading="state.saving" @click="handleSave">保存配置</el-button>
					</div>
				</div>
			</template>

			<el-form :model="state.form" label-width="180px" :rules="formRules" ref="formRef">
				<!-- HP相关 -->
				<el-divider content-position="left">HP相关参数</el-divider>
				<el-row :gutter="20">
					<el-col :span="12">
						<el-form-item label="初始HP" prop="initialHp">
							<el-input-number v-model="state.form.initialHp" :min="1" :max="1000" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="最大HP" prop="maxHp">
							<el-input-number v-model="state.form.maxHp" :min="1" :max="1000" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="HP恢复速率" prop="hpRecoveryRate">
							<el-input-number v-model="state.form.hpRecoveryRate" :min="0" :max="100" :precision="1" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="危险HP阈值" prop="dangerHpThreshold">
							<el-input-number v-model="state.form.dangerHpThreshold" :min="1" :max="100" style="width: 100%" />
						</el-form-item>
					</el-col>
				</el-row>

				<!-- 时段相关 -->
				<el-divider content-position="left">时段相关参数</el-divider>
				<el-row :gutter="20">
					<el-col :span="12">
						<el-form-item label="每日时段数" prop="periodsPerDay">
							<el-input-number v-model="state.form.periodsPerDay" :min="1" :max="12" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="每时段行动数" prop="actionsPerPeriod">
							<el-input-number v-model="state.form.actionsPerPeriod" :min="1" :max="20" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="休息恢复HP" prop="restHpRecovery">
							<el-input-number v-model="state.form.restHpRecovery" :min="0" :max="100" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="最大游戏天数" prop="maxGameDays">
							<el-input-number v-model="state.form.maxGameDays" :min="1" :max="365" style="width: 100%" />
						</el-form-item>
					</el-col>
				</el-row>

				<!-- 评分权重 -->
				<el-divider content-position="left">
					评分权重（总和必须为100）
					<el-tag :type="weightSum === 100 ? 'success' : 'danger'" class="weight-tag">
						当前总和: {{ weightSum }}
					</el-tag>
				</el-divider>
				<el-row :gutter="20">
					<el-col :span="12">
						<el-form-item label="策略得分权重" prop="strategyWeight">
							<el-input-number v-model="state.form.strategyWeight" :min="0" :max="100" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="角色扮演权重" prop="roleplayWeight">
							<el-input-number v-model="state.form.roleplayWeight" :min="0" :max="100" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="探索发现权重" prop="explorationWeight">
							<el-input-number v-model="state.form.explorationWeight" :min="0" :max="100" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="创意表现权重" prop="creativityWeight">
							<el-input-number v-model="state.form.creativityWeight" :min="0" :max="100" style="width: 100%" />
						</el-form-item>
					</el-col>
				</el-row>

				<!-- 其他参数 -->
				<el-divider content-position="left">其他参数</el-divider>
				<el-row :gutter="20">
					<el-col :span="12">
						<el-form-item label="最大技能数" prop="maxSkills">
							<el-input-number v-model="state.form.maxSkills" :min="1" :max="50" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="骰子面数" prop="diceSides">
							<el-input-number v-model="state.form.diceSides" :min="4" :max="100" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="难度修正基数" prop="difficultyModifier">
							<el-input-number v-model="state.form.difficultyModifier" :min="-10" :max="10" :precision="1" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item label="NPC最大关系值" prop="maxRelationship">
							<el-input-number v-model="state.form.maxRelationship" :min="1" :max="200" style="width: 100%" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="gameParameters">
import { reactive, ref, computed, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { GameParameterApi } from '/@/api-services/api';

const formRef = ref();

const state = reactive({
	loading: false,
	saving: false,
	form: {
		// HP相关
		initialHp: 100,
		maxHp: 100,
		hpRecoveryRate: 5,
		dangerHpThreshold: 20,
		// 时段相关
		periodsPerDay: 4,
		actionsPerPeriod: 3,
		restHpRecovery: 10,
		maxGameDays: 30,
		// 评分权重
		strategyWeight: 25,
		roleplayWeight: 25,
		explorationWeight: 25,
		creativityWeight: 25,
		// 其他
		maxSkills: 10,
		diceSides: 20,
		difficultyModifier: 0,
		maxRelationship: 100,
	},
});

const weightSum = computed(() => {
	return (state.form.strategyWeight || 0) + (state.form.roleplayWeight || 0) + (state.form.explorationWeight || 0) + (state.form.creativityWeight || 0);
});

const formRules = {
	initialHp: [{ required: true, message: '请输入初始HP', trigger: 'blur' }],
	maxHp: [{ required: true, message: '请输入最大HP', trigger: 'blur' }],
	periodsPerDay: [{ required: true, message: '请输入每日时段数', trigger: 'blur' }],
};

const loadData = async () => {
	state.loading = true;
	try {
		const res = await GameParameterApi.getGameOptions();
		const data = res.data?.result || res.data?.data;
		if (data) {
			Object.assign(state.form, data);
		}
	} catch (error: any) {
		ElMessage.error('加载配置失败: ' + (error.message || '未知错误'));
	} finally {
		state.loading = false;
	}
};

const handleSave = async () => {
	if (!formRef.value) return;
	await formRef.value.validate();

	if (weightSum.value !== 100) {
		ElMessage.error('评分权重总和必须为100，当前为: ' + weightSum.value);
		return;
	}

	state.saving = true;
	try {
		await GameParameterApi.updateGameOptions(state.form);
		ElMessage.success('保存成功');
	} catch (error: any) {
		ElMessage.error('保存失败: ' + (error.message || '未知错误'));
	} finally {
		state.saving = false;
	}
};

const handleReset = () => {
	ElMessageBox.confirm('确定要重置所有参数为默认值吗？此操作不可撤销。', '提示', { type: 'warning' }).then(async () => {
		state.loading = true;
		try {
			await GameParameterApi.resetToDefault();
			ElMessage.success('已重置为默认值');
			await loadData();
		} catch (error: any) {
			ElMessage.error('重置失败: ' + (error.message || '未知错误'));
		} finally {
			state.loading = false;
		}
	}).catch(() => {});
};

onMounted(() => {
	loadData();
});
</script>

<style scoped lang="scss">
.game-parameters-container {
	padding: 20px;

	.card-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		font-size: 16px;
		font-weight: bold;
	}

	.weight-tag {
		margin-left: 10px;
	}
}
</style>
