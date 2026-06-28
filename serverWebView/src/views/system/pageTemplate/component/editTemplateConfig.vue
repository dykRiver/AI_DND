<template>
	<div>
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="100%"
			v-if="state.isShowDialog">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit />
					</el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<div class="sys-formDes-container">
				<v-form-designer ref="vfDesigner" :designer-config="designerConfig"></v-form-designer>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">关 闭</el-button>
					<el-button type="primary" @click="save">保 存</el-button>
					<el-button type="primary" @click="submit">保存并关闭</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysTemplateConfig">
import { reactive, ref, onMounted, onUnmounted, nextTick, getCurrentInstance } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { PageTemplateApi } from '/@/api-services/api';
import { UpdateTemplateInput } from '/@/api-services/models';
import { Local, Session } from '/@/utils/storage';
import { ElMessage } from 'element-plus';
//import { fa } from 'element-plus/es/locale';

const vfDesigner: any = ref(null);
const accessTokenKey = 'access-token';
const refreshAccessTokenKey = `x-${accessTokenKey}`;
const props = defineProps({
	title: String,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const state = reactive({
	productName: 'cc',
	templateConfig: '',
	isShowDialog: false,
	rendered: false,
	rowData: {},
	ruleForm: {} as UpdateTemplateInput,
	formJson: {},
});

const designerConfig = reactive({
	productName: 'vform666',
	productTitle: '表单设计器',
	// logoHeader: false,
	formVersion: ''
})


// 打开弹窗
const openDialog = (row: any) => {
	state.rowData = row;
	state.isShowDialog = true;
	// state.ruleForm = JSON.parse(JSON.stringify(row.templateConfig));
	state.templateConfig = row.templateConfig

	nextTick(() => {
		//debugger;
		vfDesigner.value.clearDesigner();
		const accessToken = Local.get(accessTokenKey);
		// 获取刷新 token
		const refreshAccessToken = Local.get(refreshAccessTokenKey);


		//console.log(import.meta.env.VITE_API_URL);
		vfDesigner.value.globalDsv.baseUrl = import.meta.env.VITE_API_URL;
		//vfDesigner.value.globalDsv.axios=getCurrentInstance;
		vfDesigner.value.globalDsv.token = `Bearer ${accessToken}`;
		vfDesigner.value.globalDsv.xtoken = `Bearer ${refreshAccessToken}`;
		state.ruleForm = row
		vfDesigner.value.setFormJson(state.templateConfig);
	})

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
	save().then(() => { closeDialog(); });
};

const save = async () => {
	let templatejson = vfDesigner.value.getFormJson();
	state.ruleForm.templateConfig = JSON.stringify(templatejson);

	await getAPI(PageTemplateApi).apiPageTemplateUpdatePost(state.ruleForm).then(() => { ElMessage.success('已保存！'); });
	
};

// 导出对象
defineExpose({ openDialog });
</script>
