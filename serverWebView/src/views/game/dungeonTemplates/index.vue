<template>
	<div class="game-dungeon-templates-container" v-loading="state.loading">
		<!-- 难度统计卡片 -->
		<el-row :gutter="20" class="stats-cards">
			<el-col :span="6" v-for="item in state.difficultyStats" :key="item.difficulty">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-content">
						<div class="stat-value">{{ item.count }}</div>
						<div class="stat-label">{{ item.difficulty }}</div>
					</div>
				</el-card>
			</el-col>
		</el-row>

		<!-- 搜索和操作栏 -->
		<el-card shadow="hover" class="search-card">
			<el-form :model="state.queryParams" inline>
				<el-form-item label="关键词">
					<el-input v-model="state.queryParams.keyword" placeholder="搜索副本名称" clearable @keyup.enter="handleSearch" />
				</el-form-item>
				<el-form-item>
					<el-button type="primary" icon="Search" @click="handleSearch">搜索</el-button>
					<el-button icon="Refresh" @click="resetSearch">重置</el-button>
					<el-button type="success" icon="Plus" @click="handleAdd">新建模板</el-button>
				</el-form-item>
			</el-form>
		</el-card>

		<!-- 数据表格 -->
		<el-card shadow="hover">
			<el-table :data="state.tableData" border stripe>
				<el-table-column prop="name" label="名称" min-width="150" />
				<el-table-column prop="worldTheme" label="世界观主题" min-width="120" align="center" />
				<el-table-column prop="difficulty" label="难度" width="100" align="center">
					<template #default="scope">
						<el-tag :type="getDifficultyType(scope.row.difficulty)">{{ scope.row.difficulty }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="difficultyModifier" label="难度修正" width="100" align="center">
					<template #default="scope">
						<span>{{ formatModifier(scope.row.difficultyModifier) }}</span>
					</template>
				</el-table-column>
				<el-table-column prop="timeLimitDays" label="时限(天)" width="100" align="center" />
				<el-table-column prop="maxLevel" label="最大等级" width="100" align="center" />
				<el-table-column label="操作" width="180" align="center" fixed="right">
					<template #default="scope">
						<el-button type="primary" size="small" icon="Edit" @click="handleEdit(scope.row)">编辑</el-button>
						<el-button type="danger" size="small" icon="Delete" @click="handleDelete(scope.row)">删除</el-button>
					</template>
				</el-table-column>
			</el-table>
			<el-pagination
				:current-page="state.queryParams.pageIndex"
				:page-size="state.queryParams.pageSize"
				:page-sizes="[10, 20, 50]"
				:total="state.total"
				layout="total, sizes, prev, pager, next, jumper"
				@size-change="handleSizeChange"
				@current-change="handlePageChange"
				class="pagination-container"
			/>
		</el-card>

		<!-- 新建/编辑弹窗 -->
		<el-dialog v-model="state.formVisible" :title="state.isEdit ? '编辑副本模板' : '新建副本模板'" width="700px" destroy-on-close>
			<el-form :model="state.form" :rules="formRules" ref="formRef" label-width="120px">
				<el-form-item label="名称" prop="name">
					<el-input v-model="state.form.name" placeholder="请输入副本名称" />
				</el-form-item>
				<el-form-item label="世界观主题" prop="worldTheme">
					<el-input v-model="state.form.worldTheme" placeholder="请输入世界观主题" />
				</el-form-item>
				<el-form-item label="难度" prop="difficulty">
					<el-select v-model="state.form.difficulty" placeholder="请选择难度" style="width: 100%" @change="handleDifficultyChange">
						<el-option label="E - 入门" value="E" />
						<el-option label="D - 简单" value="D" />
						<el-option label="C - 普通" value="C" />
						<el-option label="B - 困难" value="B" />
						<el-option label="A - 极难" value="A" />
					</el-select>
				</el-form-item>
				<el-form-item label="难度修正值" prop="difficultyModifier">
					<el-input-number v-model="state.form.difficultyModifier" :min="-5" :max="5" style="width: 100%" />
					<div class="form-tip">选择难度会自动带出推荐值（E=-3 / D=-2 / C=0 / B=+2 / A=+3），可手动覆盖。该值参与骰子判定：有效DC = 原始DC + 修正值。</div>
				</el-form-item>
				<el-form-item label="时限天数" prop="timeLimitDays">
					<el-input-number v-model="state.form.timeLimitDays" :min="1" :max="365" style="width: 100%" />
				</el-form-item>
				<el-form-item label="升级上限" prop="maxLevel">
					<el-input-number v-model="state.form.maxLevel" :min="0" :max="100" style="width: 100%" />
					<div class="form-tip">副本内允许升级的等级上限，0 表示副本内不升级。</div>
				</el-form-item>
				<el-form-item label="标签" prop="tags">
					<el-select v-model="state.form.tags" multiple filterable allow-create default-first-option placeholder="输入标签后回车" style="width: 100%">
					</el-select>
				</el-form-item>
				<el-form-item label="描述" prop="description">
					<el-input v-model="state.form.description" type="textarea" :rows="3" placeholder="请输入副本描述" />
				</el-form-item>
				<el-form-item label="世界补充设定" prop="basePrompt">
					<el-input v-model="state.form.basePrompt" type="textarea" :rows="6" placeholder="给世界构建大师的补充设定（可选），如特定世界观细节、关键势力、开局情境等" />
				</el-form-item>

				<!-- 文风引导区 -->
				<el-divider content-position="left">文风引导（可选，通俗小说方向预设，可自定义）</el-divider>
				<div class="style-guide-tip">以下作为“世界构建大师”生成文风圣经与意象的基准，不填则由 AI 根据副本主题自行生成。</div>
				<el-form-item label="语调基调" prop="tone">
					<el-select v-model="state.form.tone" filterable allow-create default-first-option clearable placeholder="选择或输入整体语调" style="width: 100%">
						<el-option v-for="item in tonePresets" :key="item" :label="item" :value="item" />
					</el-select>
				</el-form-item>
				<el-form-item label="感官印象种子" prop="sensorySeeds">
					<el-select v-model="state.form.sensorySeeds" multiple filterable allow-create default-first-option placeholder="选择或输入后回车" style="width: 100%">
						<el-option v-for="item in sensoryPresets" :key="item" :label="item" :value="item" />
					</el-select>
				</el-form-item>
				<el-form-item label="禁用陈词" prop="forbiddenCliches">
					<el-select v-model="state.form.forbiddenCliches" multiple filterable allow-create default-first-option placeholder="选择或输入后回车" style="width: 100%">
						<el-option v-for="item in clichePresets" :key="item" :label="item" :value="item" />
					</el-select>
				</el-form-item>
				<el-form-item label="意象种子" prop="motifSeeds">
					<el-select v-model="state.form.motifSeeds" multiple filterable allow-create default-first-option placeholder="选择或输入后回车" style="width: 100%">
						<el-option v-for="item in motifPresets" :key="item" :label="item" :value="item" />
					</el-select>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.formVisible = false">取消</el-button>
				<el-button type="primary" :loading="state.saving" @click="handleSubmit">确定</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="gameDungeonTemplates">
import { reactive, ref, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { DungeonTemplateApi } from '/@/api-services/api';

const formRef = ref();

// 难度 → 世界难度修正值推荐映射
const DIFFICULTY_MODIFIER_MAP: Record<string, number> = { E: -3, D: -2, C: 0, B: 2, A: 3 };

// 文风引导预设（通俗小说方向）
const tonePresets = [
	'接地气的市井故事，直白不绕弯',
	'轻快的冒险故事，带点幽默',
	'压抑的求生故事，没有绝对的好人',
	'紧张刺激的悬疑探案故事',
	'热血爽快的升级打斗故事',
	'温馨舒缓的日常生活故事',
	'阴森诡谲的恐怖求生故事',
	'快意恩仇的武侠江湖故事',
];
const sensoryPresets = [
	'消毒水味',
	'灯管嗡嗡响',
	'瓷砖冰冷',
	'霉味',
	'铁锈味',
	'油烟味',
	'雨声',
	'尘土味',
	'血腥味',
	'劣质香水味',
	'海风咸味',
	'篝火烟味',
	'旧书页味',
	'潮湿的木头味',
	'机器轰鸣',
	'街边叫卖声',
	'劣质烟草味',
];
const clichePresets = [
	'不寒而栗',
	'毛骨悚然',
	'脊背发凉',
	'空气仿佛凝固',
	'时间仿佛静止',
	'心如死灰',
	'令人窒息',
	'汗毛倒竖',
	'诡异的笑声',
	'瞳孔骤缩',
	'倒吸一口凉气',
];
const motifPresets = [
	'滴水声',
	'破碎的镜子',
	'停摆的钟',
	'褪色的照片',
	'生锈的钥匙',
	'空荡的走廊',
	'反复出现的陌生人',
	'熄灭的路灯',
	'旧收音机',
	'墙上的抓痕',
	'缺页的日记',
	'一只流浪猫',
];

const state = reactive({
	loading: false,
	saving: false,
	tableData: [] as any[],
	total: 0,
	queryParams: {
		pageIndex: 1,
		pageSize: 20,
		keyword: '',
	},
	difficultyStats: [] as any[],
	formVisible: false,
	isEdit: false,
	editId: '',
	form: {
		name: '',
		worldTheme: '',
		difficulty: '',
		timeLimitDays: 7,
		maxLevel: 0,
		tags: [] as string[],
		description: '',
		basePrompt: '',
		difficultyModifier: 0,
		tone: '',
		sensorySeeds: [] as string[],
		forbiddenCliches: [] as string[],
		motifSeeds: [] as string[],
	},
});

const formRules = {
	name: [{ required: true, message: '请输入副本名称', trigger: 'blur' }],
	worldTheme: [{ required: true, message: '请输入世界观主题', trigger: 'blur' }],
	difficulty: [{ required: true, message: '请选择难度', trigger: 'change' }],
	timeLimitDays: [{ required: true, message: '请输入时限天数', trigger: 'blur' }],
};

const getDifficultyType = (difficulty: string) => {
	const map: Record<string, string> = { E: 'success', D: '', C: 'warning', B: 'danger', A: 'danger' };
	return map[difficulty] || 'info';
};

const formatModifier = (val: number) => {
	const n = val || 0;
	return n > 0 ? `+${n}` : `${n}`;
};

// 兼容后端返回的 List<string> 可能为数组或 JSON 字符串
const parseStringArray = (val: any): string[] => {
	if (Array.isArray(val)) return val;
	if (typeof val === 'string' && val) {
		try {
			const arr = JSON.parse(val);
			return Array.isArray(arr) ? arr : [];
		} catch {
			return [];
		}
	}
	return [];
};

// 难度变更时自动带出推荐修正值（管理员可手动覆盖）
const handleDifficultyChange = (val: string) => {
	if (val in DIFFICULTY_MODIFIER_MAP) {
		state.form.difficultyModifier = DIFFICULTY_MODIFIER_MAP[val];
	}
};

const loadData = async () => {
	state.loading = true;
	try {
		const res = await DungeonTemplateApi.getTemplateList(state.queryParams);
		const data = res.data?.result || res.data?.data || {};
		state.tableData = data.items || data.rows || [];
		state.total = data.total || 0;
	} catch (error: any) {
		ElMessage.error('加载数据失败: ' + (error.message || '未知错误'));
	} finally {
		state.loading = false;
	}
};

const loadStats = async () => {
	try {
		const res = await DungeonTemplateApi.getDifficultyStats();
		state.difficultyStats = res.data?.result || res.data?.data || [];
	} catch {}
};

const handleSearch = () => {
	state.queryParams.pageIndex = 1;
	loadData();
};

const resetSearch = () => {
	state.queryParams.keyword = '';
	state.queryParams.pageIndex = 1;
	loadData();
};

const handleSizeChange = (val: number) => {
	state.queryParams.pageSize = val;
	state.queryParams.pageIndex = 1;
	loadData();
};

const handlePageChange = (val: number) => {
	state.queryParams.pageIndex = val;
	loadData();
};

const handleAdd = () => {
	state.isEdit = false;
	state.editId = '';
	state.form = { name: '', worldTheme: '', difficulty: '', timeLimitDays: 7, maxLevel: 0, tags: [], description: '', basePrompt: '', difficultyModifier: 0, tone: '', sensorySeeds: [], forbiddenCliches: [], motifSeeds: [] };
	state.formVisible = true;
};

const handleEdit = async (row: any) => {
	state.isEdit = true;
	state.editId = row.id;
	try {
		const res = await DungeonTemplateApi.getTemplateDetail(row.id);
		const detail = res.data?.result || res.data?.data || row;
		state.form = {
			name: detail.name || '',
			worldTheme: detail.worldTheme || '',
			difficulty: detail.difficulty || '',
			timeLimitDays: detail.timeLimitDays || 7,
			maxLevel: detail.maxLevel || 0,
			tags: typeof detail.tags === 'string' ? JSON.parse(detail.tags || '[]') : (detail.tags || []),
			description: detail.description || '',
			basePrompt: detail.basePrompt || '',
			difficultyModifier: detail.difficultyModifier ?? (DIFFICULTY_MODIFIER_MAP[detail.difficulty] || 0),
			tone: detail.tone || '',
			sensorySeeds: parseStringArray(detail.sensorySeeds),
			forbiddenCliches: parseStringArray(detail.forbiddenCliches),
			motifSeeds: parseStringArray(detail.motifSeeds),
		};
		state.formVisible = true;
	} catch (error: any) {
		ElMessage.error('获取详情失败: ' + (error.message || '未知错误'));
	}
};

const handleSubmit = async () => {
	if (!formRef.value) return;
	await formRef.value.validate();
	state.saving = true;
	try {
		if (state.isEdit) {
			await DungeonTemplateApi.updateTemplate(state.editId, state.form);
			ElMessage.success('更新成功');
		} else {
			await DungeonTemplateApi.createTemplate(state.form);
			ElMessage.success('创建成功');
		}
		state.formVisible = false;
		loadData();
		loadStats();
	} catch (error: any) {
		ElMessage.error('操作失败: ' + (error.message || '未知错误'));
	} finally {
		state.saving = false;
	}
};

const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定要删除副本模板「${row.name}」吗？`, '提示', { type: 'warning' }).then(async () => {
		try {
			await DungeonTemplateApi.deleteTemplate(row.id);
			ElMessage.success('删除成功');
			loadData();
			loadStats();
		} catch (error: any) {
			ElMessage.error('删除失败: ' + (error.message || '未知错误'));
		}
	}).catch(() => {});
};

onMounted(() => {
	loadData();
	loadStats();
});
</script>

<style scoped lang="scss">
.game-dungeon-templates-container {
	padding: 20px;

	.stats-cards {
		margin-bottom: 20px;

		.stat-card {
			text-align: center;

			.stat-content {
				.stat-value {
					font-size: 28px;
					font-weight: bold;
					color: #303133;
				}

				.stat-label {
					font-size: 14px;
					color: #909399;
					margin-top: 5px;
				}
			}
		}
	}

	.search-card {
		margin-bottom: 20px;
	}

	.pagination-container {
		margin-top: 20px;
		display: flex;
		justify-content: flex-end;
	}
}

.form-tip {
	font-size: 12px;
	color: #909399;
	line-height: 1.5;
	margin-top: 4px;
}

.style-guide-tip {
	font-size: 12px;
	color: #909399;
	line-height: 1.5;
	margin: -8px 0 16px;
}
</style>
