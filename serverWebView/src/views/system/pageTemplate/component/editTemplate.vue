<template>
	<div>
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row>
					<el-col :xs="24" :sm="12" :md="12" :lg="24" :xl="24" class="mb20">
						<el-form-item label="状态">
							<el-radio-group v-model="state.ruleForm.enabled">
								<el-radio :label="true">启用</el-radio>
								<el-radio :label="false">禁用</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="功能名称" prop="name" :rules="[{ required: true, message: '功能名称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" placeholder="功能名称" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="功能编码" prop="code" :rules="[{ required: true, message: '功能编码不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.code" placeholder="功能编码" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="上级菜单" >
							<el-cascader
								:options="props.menuData"
								:props="{ checkStrictly: true, emitPath: false, value: 'id', label: 'title' }"
								placeholder="请选择上级菜单"
								clearable
								class="w100"
								v-model="state.ruleForm.parentMenuId"
							>
								<template #default="{ node, data }">
									<span>{{ data.title }}</span>
									<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
								</template>
							</el-cascader>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="菜单图标" :rules="[{ required: true, message: '菜单图标未选择', trigger: 'blur' }]">
							<IconSelector v-model="state.ruleForm.icon" :size="getGlobalComponentSize" placeholder="菜单图标" type="all" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="排序">
							<el-input-number v-model="state.ruleForm.orderNo" placeholder="排序" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="配置JSON">
							<el-input v-model="state.ruleForm.templateConfig" placeholder="请输入功能配置JSON内容" clearable type="textarea" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="备注">
							<el-input v-model="state.ruleForm.description" placeholder="请输入备注内容" clearable type="textarea" />
						</el-form-item>
					</el-col>
				</el-row>
				<el-row>
					<!-- <el-tag effect="dark" type="danger">
						注意：禁用页面后会删除对应的菜单项，【菜单图标】和【上级菜单】信息将会丢失，请谨慎操作。
					</el-tag> -->
					<el-text v-show="!state.ruleForm.enabled" class="mx-1" type="danger">注意：禁用页面后会删除对应的菜单项，【菜单图标】和【上级菜单】信息将会丢失，请谨慎操作。</el-text>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">取 消</el-button>
					<el-button type="primary" @click="submit">确 定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysEditDictType">
import {computed, reactive, ref } from 'vue';
import other from '/@/utils/other';
import { getAPI } from '/@/utils/axios-utils';
import { PageTemplateApi } from '/@/api-services/api';
import { UpdateTemplateInput } from '/@/api-services/models';
import { SysMenu } from '/@/api-services/models';
import IconSelector from '/@/components/iconSelector/index.vue';
const props = defineProps({
	title: String,
	menuData: Array<SysMenu>,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as UpdateTemplateInput,
});
// 获取全局组件大小
const getGlobalComponentSize = computed(() => {
	return other.globalComponentSize();
});
// 打开弹窗
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;
	ruleFormRef.value?.resetFields();
};

// 关闭弹窗
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// 取消
const cancel = () => {
	state.isShowDialog = false;
};

// 提交
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		if (state.ruleForm.id != undefined && state.ruleForm.id > 0) {
			await getAPI(PageTemplateApi).apiPageTemplateUpdatePost(state.ruleForm);
		} else {
			await getAPI(PageTemplateApi).apiPageTemplateAddPost(state.ruleForm);
		}
		closeDialog();
	});
};

// 导出对象
defineExpose({ openDialog });
</script>
