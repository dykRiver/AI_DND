<template>
  <div class="sys-formDes-container">
    <v-form-render :form-json="formJson" :form-data="formData" :option-data="optionData" ref="vFormRef">
    </v-form-render>
  </div>
</template>

<script lang="ts" setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { useRouter } from 'vue-router'

import { getAPI } from '/@/utils/axios-utils';
import { PageTemplateApi } from '/@/api-services/api';
import { PageTemplate } from '/@/api-services/models';
import { signalR } from '/@/views/system/onlineUser/signalR';
import mittBus from '/@/utils/mitt';
const router = useRouter();
const vFormRef: any = ref(null)
var formJson = reactive({})
var formData = reactive({})
var optionData = reactive({})

const state = reactive({
  code: undefined,
  formJson: { "widgetList": [{ "type": "grid", "category": "container", "icon": "grid", "cols": [{ "type": "grid-col", "category": "container", "icon": "grid-col", "internal": true, "widgetList": [{ "type": "radio", "icon": "radio-field", "formItemFlag": true, "options": { "name": "drinkRadio", "label": "喜欢喝酒还是饮料？", "labelAlign": "", "defaultValue": null, "columnWidth": "200px", "size": "", "displayStyle": "inline", "labelWidth": null, "labelHidden": false, "disabled": false, "hidden": false, "optionItems": [{ "label": "喝酒", "value": 1 }, { "label": "喝饮料", "value": 2 }], "required": false, "validation": "", "validationHint": "", "customClass": [], "labelIconClass": null, "labelIconPosition": "rear", "labelTooltip": null, "onCreated": "", "onMounted": "", "onChange": "var alcoholChkWidget = this.getWidgetRef('alcoholChk')\nvar drinkChkWidget = this.getWidgetRef('drinkChk')\n\nif (value === 1) {\n  alcoholChkWidget.setHidden(false)\n  drinkChkWidget.setHidden(true)\n} else {\n  alcoholChkWidget.setHidden(true)\n  drinkChkWidget.setHidden(false)\n}", "onValidate": "" }, "displayName": "单选项", "id": "radio98420" }], "options": { "name": "gridCol89539", "hidden": false, "span": 24 }, "id": "grid-col-89539" }, { "type": "grid-col", "category": "container", "icon": "grid-col", "internal": true, "widgetList": [{ "type": "checkbox", "icon": "checkbox-field", "formItemFlag": true, "options": { "name": "alcoholChk", "label": "喝什么酒", "labelAlign": "", "defaultValue": [], "columnWidth": "200px", "size": "", "displayStyle": "inline", "labelWidth": null, "labelHidden": false, "readonly": false, "disabled": false, "hidden": true, "optionItems": [{ "label": "茅台", "value": 1 }, { "label": "二锅头", "value": 2 }, { "label": "伏尔加", "value": 3 }], "required": false, "validation": "", "validationHint": "", "customClass": [], "labelIconClass": null, "labelIconPosition": "rear", "labelTooltip": null, "onCreated": "", "onMounted": "", "onChange": "", "onValidate": "" }, "displayName": "多选项", "id": "checkbox46135" }], "options": { "name": "gridCol76644", "hidden": false, "span": 24, "customClass": [] }, "id": "grid-col-76644" }, { "type": "grid-col", "category": "container", "icon": "grid-col", "internal": true, "widgetList": [{ "type": "checkbox", "icon": "checkbox-field", "formItemFlag": true, "options": { "name": "drinkChk", "label": "喝啥子饮料", "labelAlign": "", "defaultValue": [], "columnWidth": "200px", "size": "", "displayStyle": "inline", "labelWidth": null, "labelHidden": false, "readonly": false, "disabled": false, "hidden": true, "optionItems": [{ "label": "肥宅水", "value": 1 }, { "label": "白花蛇草水", "value": 2 }, { "label": "农夫山泉有点田", "value": 3 }], "required": false, "validation": "", "validationHint": "", "customClass": "", "labelIconClass": null, "labelIconPosition": "rear", "labelTooltip": null, "onCreated": "", "onMounted": "", "onChange": "", "onValidate": "" }, "displayName": "多选项", "id": "checkbox48765" }], "options": { "name": "gridCol17019", "hidden": false, "span": 24 }, "id": "grid-col-17019" }], "options": { "name": "grid85701", "hidden": false, "gutter": 12, "customClass": [] }, "displayName": "栅格", "id": "grid85701" }], "formConfig": { "modelName": "formData", "refName": "vForm", "rulesName": "rules", "labelWidth": 200, "labelPosition": "left", "size": "", "labelAlign": "label-left-align", "cssCode": "", "customClass": "", "functions": "", "layoutType": "PC", "onFormCreated": "", "onFormMounted": "", "onFormDataChange": "", "jsonVersion": 2, "dataSources": [], "onFormValidate": "" } },

})

// 查询操作
const handleQuery = async () => {
  let path = router.currentRoute.value.path
  let pathcode = path.split('/')[path.split('/').length - 1]

  let params = { code: pathcode }
  var res = await getAPI(PageTemplateApi).apiPageTemplateGetByCodePost(params);
  vFormRef.value.setFormJson(JSON.parse(String(res.data.data?.templateConfig)));
  vFormRef.value.globalDsv.mittBus = mittBus;
  vFormRef.value.globalDsv.signalR=signalR;
  vFormRef.value.globalDsv.evetHub=evetHub;
  vFormRef.value.globalDsv.baseUrl = import.meta.env.VITE_API_URL;
  //console.log(import.meta.env.VITE_API_URL)
};

const evetHub =(evetName:string,callBackFunc:Function) =>{
  signalR.off(evetName);
  signalR.on(evetName, (user, message) => {
    Function(message)
  });

  // mittBus.on('layoutMobileResize', (res) => {
  // 	Function(res)
  // });
}

onMounted(async () => {

  handleQuery();

});
</script>

<style lang="scss" scoped>
body {
  margin: 0; // 去除页面垂直滚动条
}

.form-designer {
  overflow: unset !important;
}
</style>
