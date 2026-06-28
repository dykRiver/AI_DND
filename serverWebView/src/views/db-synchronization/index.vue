<template>
	<div class="db-synv-container">
		<el-row :gutter="5" style="width: 100%;">
			<el-col :xs="24" style="display: flex; flex-direction: column">
				<el-card shadow="hover" :body-style="{ paddingBottom: '0' }">
					<el-form :model="state.queryParams" ref="queryForm" :inline="true">
						<el-form-item label="表名">
							<el-input v-model="state.queryParams.name" placeholder="表名" clearable />
						</el-form-item>
						<el-form-item>
							<el-button-group>
								<el-button type="primary" icon="ele-Search" @click="handleQuery"> 查询 </el-button>
								<el-button icon="ele-Refresh" @click="resetQuery"> 重置 </el-button>
							</el-button-group>
						</el-form-item>
						<el-form-item>
							<el-button type="primary" icon="ele-Plus" @click="openAddDbSync" v-auth="'dbSync:add'"> 新增
							</el-button>
						</el-form-item>
					</el-form>
				</el-card>

				<el-card class="full-table" shadow="hover" style="margin-top: 5px">
					<el-table :data="state.dbSyncData" style="width: 100%" v-loading="state.loading" row-key="id"
						default-expand-all :tree-props="{ children: 'children', hasChildren: 'hasChildren' }" border>
						<el-table-column prop="tableName" label="表名" min-width="50" header-align="center"
							show-overflow-tooltip />
						<el-table-column prop="lastSynTime" label="最近同步时间" align="center" show-overflow-tooltip />
						<el-table-column prop="lastFullTableSynTime" label="全量同步时间" align="center"
							show-overflow-tooltip />
						<el-table-column prop="initSynTime" label="初始化时间" align="center" show-overflow-tooltip />
						<el-table-column prop="structUpdateTime" label="表结构同步时间" align="center" show-overflow-tooltip />
						<el-table-column label="操作" width="140" fixed="right" align="center" show-overflow-tooltip>
							<template #default="scope">
								<el-button icon="ele-Edit" size="small" text type="primary"
									@click="openEditDbSync(scope.row)"  v-auth="'dbSync:update'"> 编辑 </el-button>
								<el-button icon="ele-Delete" size="small" text type="danger"
									@click="delDbSync(scope.row)"  v-auth="'dbSync:delete'"> 删除 </el-button>
							</template>
						</el-table-column>
					</el-table>
				</el-card>
			</el-col>
		</el-row>
		<EditDbSync ref="editDbSyncRef" :title="state.editDbSyncTitle" @handleQuery="handleQuery"/>
	</div>
</template>

<script lang="ts" setup name="databaseSync">
import { onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';

import { getAPI } from '/@/utils/axios-utils';
import { DatabaseSyncApi } from '/@/api-services/apis/database-sync-api';
import { DatabaseSync } from '/@/api-services/models/database-sync';
import EditDbSync from '/@/views/db-synchronization/component/editDbSync.vue';
const editDbSyncRef = ref<InstanceType<typeof EditDbSync>>();

const state = reactive({
	loading: false,
	dbSyncData: [] as Array<DatabaseSync>, //列表数据
	queryParams: {
		id: 0,
		name: undefined,
		code: undefined,
		type: undefined,
	},
	editDbSyncTitle: '',
});

onMounted(async () => {
	handleQuery();
});

// 查询操作
const handleQuery = async () => {
	state.loading = true;
	var res = await getAPI(DatabaseSyncApi).apiDatabaseSyncListGet(state.queryParams.id, state.queryParams.name, state.queryParams.code, state.queryParams.type);
	state.dbSyncData = res.data.result ?? [];
	state.loading = false;
};

// 重置操作
const resetQuery = () => {
	state.queryParams.id = 0;
	state.queryParams.name = undefined;
	state.queryParams.code = undefined;
	state.queryParams.type = undefined;
	handleQuery();
};

// 打开新增页面
const openAddDbSync = () => {
	state.editDbSyncTitle = '添加同步表';
	editDbSyncRef.value?.openDialog({ enable: true, orderNo: 100 });
};

// 打开编辑页面
const openEditDbSync = (row: any) => {
	state.editDbSyncTitle = '编辑同步表';
	editDbSyncRef.value?.openDialog(row);
};

// 删除
const delDbSync = (row: any) => {
	ElMessageBox.confirm(`确定不再同步表：【${row.tableName}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(DatabaseSyncApi).apiDatabaseSyncDeletePost({ id: row.id });
			ElMessage.success('删除成功');
			handleQuery();
		})
		.catch(() => { });
};
</script>