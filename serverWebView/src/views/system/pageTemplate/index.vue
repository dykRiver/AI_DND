<template>
	<div class="sys-dict-container">
		<el-card shadow="hover" :body-style="{ paddingBottom: '0' }">
			<el-form :model="state.queryParams" ref="queryForm" :inline="true">
				<el-form-item label="功能名称">
					<el-input v-model="state.queryParams.name" placeholder="功能名称" clearable />
				</el-form-item>
				<el-form-item label="功能编码">
					<el-input v-model="state.queryParams.code" placeholder="功能编码" clearable />
				</el-form-item>
				<el-form-item>
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery" v-auth="'pageTemplate:page'">
							查询 </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery"> 重置 </el-button>
					</el-button-group>
				</el-form-item>
				<el-form-item>
					<el-button type="primary" icon="ele-Plus" @click="openAddTemplate"
						v-auth="'pageTemplate:add'">新增</el-button>
				</el-form-item>
			</el-form>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 8px">
			<el-table :data="state.pageTemplateDatas" style="width: 100%" v-loading="state.loading" border>
				<el-table-column type="index" label="序号" width="55" align="center" />
				<el-table-column prop="name" label="功能名称" header-align="center" show-overflow-tooltip />
				<el-table-column prop="code" label="功能编码" header-align="center" show-overflow-tooltip />
				<el-table-column prop="isEnable" label="状态" width="70" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-tag type="success" v-if="scope.row.enabled === true">启用</el-tag>
						<el-tag type="danger" v-else>禁用</el-tag>
					</template>
				</el-table-column>
				<!-- <el-table-column prop="templateConfig" label="功能配置" align="center" show-overflow-tooltip /> -->
				<el-table-column prop="createTime" label="创建时间" align="center" show-overflow-tooltip />
				<el-table-column prop="updateTime" label="修改时间" align="center" show-overflow-tooltip />
				<el-table-column prop="description" label="备注" header-align="center" show-overflow-tooltip />
				<el-table-column label="操作" width="200" fixed="right" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-button icon="ele-Edit" size="small" text type="primary"
							@click="openeditTemplateConfig(scope.row)" v-auth="'pageTemplate:design'">编辑配置</el-button>

						<el-dropdown>
							<el-button icon="ele-MoreFilled" size="small" text type="primary"
								style="padding-left: 12px" />
							<template #dropdown>
								<el-dropdown-menu>
									<el-dropdown-item icon="ele-OfficeBuilding" @click="openEditTemplate(scope.row)"
										v-auth="'pageTemplate:update'">编辑</el-dropdown-item>
									<el-dropdown-item icon="ele-Delete" @click="delTemplate(scope.row)"
										v-auth="'pageTemplate:delete'">删除</el-dropdown-item>
								</el-dropdown-menu>
							</template>
						</el-dropdown>

					</template>
				</el-table-column>
			</el-table>
			<el-pagination v-model:currentPage="state.tableParams.page" v-model:page-size="state.tableParams.pageSize"
				:total="state.tableParams.total" :page-sizes="[10, 15, 20, 50, 100]" small background 
				@size-change="handleSizeChange" @current-change="handleCurrentChange"
				layout="total, sizes, prev, pager, next, jumper" />
		</el-card>

		<EditTemplate ref="editTemplateRef" :menuData="state.menuData" :title="state.editTemplateTitle"
			@handleQuery="handleQuery" />

		<editTemplateConfig ref="editTemplateConfigRef" :title="state.editTemplateTitle" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="sysTemplate">
import { onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import EditTemplate from '/@/views/system/pageTemplate/component/editTemplate.vue';
import editTemplateConfig from '/@/views/system/pageTemplate/component/editTemplateConfig.vue';


import { getAPI } from '/@/utils/axios-utils';
import { PageTemplateApi } from '/@/api-services/api';
import { PageTemplate } from '/@/api-services/models';
import { SysMenuApi } from '/@/api-services/api';
import { SysMenu } from '/@/api-services/models';

const editTemplateRef = ref<InstanceType<typeof EditTemplate>>();
const editTemplateConfigRef = ref<InstanceType<typeof editTemplateConfig>>();
const state = reactive({
	loading: false,
	menuData: [] as Array<SysMenu>,
	pageTemplateDatas: [] as Array<PageTemplate>,
	queryParams: {
		name: undefined,
		code: undefined,
		baseUrl: 'http://localhost:5005/api',
	},
	tableParams: {
		page: 1,
		pageSize: 15,
		total: 0 as any,
	},
	editTemplateTitle: '',

});

onMounted(async () => {
	handleQuery();
});

// 查询操作
const handleQuery = async () => {
	state.loading = true;
	let params = Object.assign(state.queryParams, state.tableParams);
	var res = await getAPI(PageTemplateApi).apiPageTemplatePagePost(params);
	state.pageTemplateDatas = res.data.result?.items ?? [];
	state.tableParams.total = res.data.result?.total;//cey更改

	//获取菜单数据
	var resMenu = await getAPI(SysMenuApi).apiSysMenuListGet(undefined, undefined);
	state.menuData = resMenu.data.result ?? [];

	state.loading = false;
};

// 重置操作
const resetQuery = () => {
	state.queryParams.name = undefined;
	state.queryParams.code = undefined;
	handleQuery();
};

// 打开新增页面
const openAddTemplate = () => {
	state.editTemplateTitle = '添加功能';
	editTemplateRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// 打开编辑页面
const openEditTemplate = (row: any) => {
	state.editTemplateTitle = '编辑功能';
	editTemplateRef.value?.openDialog(row);
};

// 打开编辑配置页面
const openeditTemplateConfig = (row: any) => {
	state.editTemplateTitle = '编辑配置';
	editTemplateConfigRef.value?.openDialog(row);
};

// 删除
const delTemplate = (row: any) => {
	ElMessageBox.confirm(`确定删除功能：【${row.name}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(PageTemplateApi).apiPageTemplateDeletePost({ id: row.id });
			ElMessage.success('删除成功');
			handleQuery();
		})
		.catch(() => { });
};

// 改变页面容量
const handleSizeChange = (val: number) => {
	state.tableParams.pageSize = val;
	handleQuery();
};

// 改变页码序号
const handleCurrentChange = (val: number) => {
	state.tableParams.page = val;
	handleQuery();
};
</script>
