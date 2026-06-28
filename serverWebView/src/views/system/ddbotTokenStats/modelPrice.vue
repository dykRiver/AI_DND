<template>
	<div class="ddbot-model-price" v-loading="state.loading">
		<el-card shadow="hover">
			<template #header>
				<div class="card-header">
					<span>AI模型单价配置</span>
					<el-button type="primary" icon="Plus" @click="handleAdd">新增模型</el-button>
				</div>
			</template>

			<el-table :data="state.priceList" border stripe>
				<el-table-column prop="modelName" label="模型名称" width="200" align="center" />
				<el-table-column prop="displayName" label="显示名称" width="200" align="center" />
				<el-table-column prop="inputPricePerThousand" label="输入单价(元/千token)" width="180" align="center">
					<template #default="scope">
						¥{{ (scope.row.inputPricePerThousand || 0).toFixed(4) }}
					</template>
				</el-table-column>
				<el-table-column prop="outputPricePerThousand" label="输出单价(元/千token)" width="180" align="center">
					<template #default="scope">
						¥{{ (scope.row.outputPricePerThousand || 0).toFixed(4) }}
					</template>
				</el-table-column>
				<el-table-column prop="isEnabled" label="状态" width="100" align="center">
					<template #default="scope">
						<el-tag v-if="scope.row.isEnabled" type="success">启用</el-tag>
						<el-tag v-else type="danger">禁用</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="remark" label="备注" min-width="200" show-overflow-tooltip />
				<el-table-column label="操作" width="150" align="center" fixed="right">
					<template #default="scope">
						<el-button type="primary" icon="Edit" size="small" text @click="handleEdit(scope.row)">编辑</el-button>
						<el-button type="danger" icon="Delete" size="small" text @click="handleDelete(scope.row)">删除</el-button>
					</template>
				</el-table-column>
			</el-table>
		</el-card>

		<!-- 编辑对话框 -->
		<el-dialog v-model="state.dialogVisible" :title="state.dialogTitle" width="600px" draggable>
			<el-form ref="formRef" :model="state.form" :rules="state.rules" label-width="160px">
				<el-form-item label="模型名称" prop="modelName">
					<el-input v-model="state.form.modelName" placeholder="如: qwen-turbo" :disabled="state.isEdit" />
				</el-form-item>
				<el-form-item label="显示名称" prop="displayName">
					<el-input v-model="state.form.displayName" placeholder="如: 通义千问-Turbo" />
				</el-form-item>
				<el-form-item label="输入单价(元/千token)" prop="inputPricePerThousand">
					<el-input-number v-model="state.form.inputPricePerThousand" :precision="4" :min="0" :step="0.001" style="width: 100%" />
				</el-form-item>
				<el-form-item label="输出单价(元/千token)" prop="outputPricePerThousand">
					<el-input-number v-model="state.form.outputPricePerThousand" :precision="4" :min="0" :step="0.001" style="width: 100%" />
				</el-form-item>
				<el-form-item label="是否启用" prop="isEnabled">
					<el-switch v-model="state.form.isEnabled" />
				</el-form-item>
				<el-form-item label="备注" prop="remark">
					<el-input v-model="state.form.remark" type="textarea" :rows="3" placeholder="备注信息" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.dialogVisible = false">取消</el-button>
				<el-button type="primary" @click="handleSubmit" :loading="state.submitLoading">确定</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="ddbotModelPrice">
import { ref, reactive, onMounted } from 'vue';
import { ElMessage, ElMessageBox, FormInstance, FormRules } from 'element-plus';
import { Plus, Edit, Delete } from '@element-plus/icons-vue';

// import { getAPI } from '/@/utils/axios-utils';
// import { DDBotTokenUsageApi } from '/@/api-services/api';

const formRef = ref<FormInstance>();

const state = reactive({
	loading: false,
	submitLoading: false,
	priceList: [] as any[],
	dialogVisible: false,
	dialogTitle: '',
	isEdit: false,
	form: {
		id: undefined as number | undefined,
		modelName: '',
		displayName: '',
		inputPricePerThousand: 0,
		outputPricePerThousand: 0,
		isEnabled: true,
		remark: '',
	},
	rules: {
		modelName: [{ required: true, message: '请输入模型名称', trigger: 'blur' }],
		displayName: [{ required: true, message: '请输入显示名称', trigger: 'blur' }],
		inputPricePerThousand: [{ required: true, message: '请输入输入单价', trigger: 'blur' }],
		outputPricePerThousand: [{ required: true, message: '请输入输出单价', trigger: 'blur' }],
	} as FormRules,
});

// 查询模型单价列表
const loadData = async () => {
	state.loading = true;
	try {
		// TODO: 调用后端API
		// const res = await getAPI(DDBotTokenUsageApi).apiDdbotTokenUsageGetModelPricesPost();
		// state.priceList = res.data.result || [];

		// 模拟数据
		state.priceList = [
			{
				id: 1,
				modelName: 'qwen-turbo',
				displayName: '通义千问-Turbo',
				inputPricePerThousand: 0.002,
				outputPricePerThousand: 0.006,
				isEnabled: true,
				remark: '便宜快速的模型',
			},
			{
				id: 2,
				modelName: 'qwen-plus',
				displayName: '通义千问-Plus',
				inputPricePerThousand: 0.004,
				outputPricePerThousand: 0.012,
				isEnabled: true,
				remark: '均衡模型',
			},
			{
				id: 3,
				modelName: 'qwen3.5-plus',
				displayName: '通义千问3.5-Plus',
				inputPricePerThousand: 0.008,
				outputPricePerThousand: 0.024,
				isEnabled: true,
				remark: '强大模型(支持思考模式)',
			},
			{
				id: 4,
				modelName: 'qwen-vl-ocr-latest',
				displayName: '通义千问视觉OCR',
				inputPricePerThousand: 0.008,
				outputPricePerThousand: 0.024,
				isEnabled: true,
				remark: '视觉识别模型',
			},
		];
	} catch (error: any) {
		ElMessage.error('加载失败: ' + error.message);
	} finally {
		state.loading = false;
	}
};

// 新增
const handleAdd = () => {
	state.dialogTitle = '新增模型单价';
	state.isEdit = false;
	state.form = {
		id: undefined,
		modelName: '',
		displayName: '',
		inputPricePerThousand: 0,
		outputPricePerThousand: 0,
		isEnabled: true,
		remark: '',
	};
	state.dialogVisible = true;
};

// 编辑
const handleEdit = (row: any) => {
	state.dialogTitle = '编辑模型单价';
	state.isEdit = true;
	state.form = {
		id: row.id,
		modelName: row.modelName,
		displayName: row.displayName,
		inputPricePerThousand: row.inputPricePerThousand,
		outputPricePerThousand: row.outputPricePerThousand,
		isEnabled: row.isEnabled,
		remark: row.remark,
	};
	state.dialogVisible = true;
};

// 删除
const handleDelete = async (row: any) => {
	try {
		await ElMessageBox.confirm(`确定要删除模型 "${row.displayName}" 吗?`, '提示', {
			confirmButtonText: '确定',
			cancelButtonText: '取消',
			type: 'warning',
		});

		// TODO: 调用后端API
		// await getAPI(DDBotTokenUsageApi).apiDdbotTokenUsageDeleteModelPricePost({ id: row.id });

		ElMessage.success('删除成功');
		loadData();
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error('删除失败: ' + error.message);
		}
	}
};

// 提交表单
const handleSubmit = async () => {
	if (!formRef.value) return;

	await formRef.value.validate(async (valid) => {
		if (!valid) return;

		state.submitLoading = true;
		try {
			// TODO: 调用后端API
			// await getAPI(DDBotTokenUsageApi).apiDdbotTokenUsageSaveModelPricePost(state.form);

			ElMessage.success(state.isEdit ? '更新成功' : '新增成功');
			state.dialogVisible = false;
			loadData();
		} catch (error: any) {
			ElMessage.error('保存失败: ' + error.message);
		} finally {
			state.submitLoading = false;
		}
	});
};

onMounted(() => {
	loadData();
});
</script>

<style scoped lang="scss">
.ddbot-model-price {
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
