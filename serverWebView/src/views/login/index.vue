<template>
	<el-row class="login-container">
		<el-col :span="12">
			<div class="login-left-logo"></div>
		</el-col>
		<el-col :span="12" class="login-right">
			<div class="login-right-wrap">
				<el-row class="login-placeholder">
					<el-col>
					</el-col>
				</el-row>
				<el-row class="login-right-warp-main-span">
					<el-col>
						<!--公司名称英文-->
						DHY&nbsp;INTELLIGENT&nbsp;DECOCTION
					</el-col>
				</el-row>
				<el-row class="login-right-warp-main-title">
					<el-col>
						<!--主标题-->
						{{ getThemeConfig.globalTitle }}
					</el-col>
				</el-row>
				<el-row class="login-right-warp-main-title-line">
					<el-col>
						<!--副标题-->
						{{ getThemeConfig.globalViceTitle }}
					</el-col>
				</el-row>
				<el-row class="login-right-warp-main-form">
					<el-col>
						<!--登录框-->
						<Account />
					</el-col>
				</el-row>
			</div>
		</el-col>
	</el-row>
</template>

<script setup lang="ts" name="loginIndex">
import { defineAsyncComponent, onMounted, reactive, computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { NextLoading } from '/@/utils/loading';


// 引入组件
const Account = defineAsyncComponent(() => import('/@/views/login/component/account.vue'));


const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const state = reactive({
	tabsActiveName: 'account',
	isScan: false,
});
// 获取布局配置信息
const getThemeConfig = computed(() => {
	return themeConfig.value;
});
// 页面加载时
onMounted(() => {
	NextLoading.done();
});
</script>

<style scoped lang="scss">
.login-placeholder {
	height: 28%;
}

.login-container {
	height: 100%;
	background: var(--el-color-white);
	background-image: url('@/assets/bg_new.png');
	background-size: cover;

	.login-left-logo {
		position: absolute;
		background-image: url('@/assets/dhy_logo.png');
		background-size: contain;
		background-repeat: no-repeat;
		top: 50px;
		left: 80px;
		z-index: 1;
		animation: logoAnimation 0.3s ease;
		//img {
		width: 208px;
		height: 68px;
		//}
		min-width: 208px;
	}

	.login-right {
		overflow: hidden;
		padding: 20px;
	}

	.login-right-wrap {
		height: 100%;
		background-image: url('@/assets/information_bg.png');
		background-size: contain;
		background-repeat: no-repeat;
		background-position: center;
	}


	.login-right-warp-main-span {
		font-family: Ariel narrow;
		letter-spacing: 10px;
		color: white;
		height: 3%;
		text-align: center;
		font-size: 1rem;
		line-height: 2rem;
	}

	.login-right-warp-main-title {
		height: 6%;
		font-size: 4rem;
		font-weight: 300;
		font-family: '华文隶书';
		text-align: center;
		//letter-spacing: 3px;
		animation: logoAnimation 0.3s ease;
		animation-delay: 0.3s;
		color: #ffffff;
	}

	.login-right-warp-main-title-line {
		height: 3%;
		margin: 10px 20% 16px 20%;
		color: #ffffff;
		text-align: center;
		font-family: '黑体';
		font-size: 2rem;
		background-image: url('@/assets/3_line.png');
		background-position: center; // 水平垂直居中
		background-size: contain; // 覆盖整个元素区域
		background-repeat: no-repeat; // 不重复背景图片

	}

	.login-right-warp-main-form {
		height: 42%;
		margin: 0 21.5% 0 21.5%;
		background-image: url('@/assets/information_bg_box.png');
		background-size: 100% 100%;
		background-repeat: no-repeat;
	}

	@media screen and (min-width: 1366px) and (max-width: 1400px) {
		.login-placeholder {
			height: 28%;
		}

		.login-right-warp-main-span {
			height: 3%;
			font-size: .82rem;
			letter-spacing: .82rem;
		}

		.login-right-warp-main-title {
			height: 6%;
			font-size: 3.6rem;
			font-weight: 300;
		}

		.login-right-warp-main-title-line {
			height: 3%;
			margin: 10px 18% 16px 18%;
			font-size: 1.6rem;
		}

		.login-right-warp-main-form {
			height: 43%;
			margin: 0 17% 0 17%;
		}
	}

	@media screen and (min-width: 1440px) and (max-width: 1600px) {
		.login-placeholder {
			height: 29%;
		}

		.login-right-warp-main-span {
			height: 3%;
			font-size: .82rem;
			letter-spacing: .82rem;
		}

		.login-right-warp-main-title {
			height: 6%;
			font-size: 3.6rem;
			font-weight: 300;
		}

		.login-right-warp-main-title-line {
			height: 3%;
			margin: 10px 18% 16px 18%;
			font-size: 1.6rem;
		}

		.login-right-warp-main-form {
			height: 38%;
			margin: 0 17% 0 17%;
		}
	}

	@media screen and (min-width: 2560px) and (max-width: 3840px) {
		.login-placeholder {
			height: 29%;
		}

		.login-left-logo {
			width: 308px;
			height: 98px;
			min-width: 608px;
		}

		.login-right-warp-main-span {
			height: 2.2%;
			font-size: 2rem;
			letter-spacing: 1.3rem;
		}

		.login-right-warp-main-title {
			height: 7%;
			font-size: 7rem;
			font-weight: 300;
		}

		.login-right-warp-main-title-line {
			height: 5%;
			margin: 0 18% 0 18%;
			font-size: 3.1rem;
		}

		.login-right-warp-main-form {
			height: 42%;
			margin: 0 17% 0 17%;
		}
	}

	@media screen and (min-width: 3840px) and (max-width: 4096px) {
		.login-placeholder {
			height: 29%;
		}

		.login-left-logo {
			width: 408px;
			height: 168px;
			min-width: 608px;
		}

		.login-right-warp-main-span {
			height: 2%;
			letter-spacing: 2rem;
			font-size: 2.5rem;
		}

		.login-right-warp-main-title {
			height: 7%;
			font-size: 11rem;
			font-weight: 300;
		}

		.login-right-warp-main-title-line {
			height: 3%;
			margin: 2rem 17% 1.5rem 17%;
			font-size: 4rem;
		}

		.login-right-warp-main-form {
			height: 43%;
			margin: 0 17.2% 0 17.2%;
		}
	}
}
</style>
