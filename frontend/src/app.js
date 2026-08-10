// olsold'da bu CSS Blade tarafından @vite('resources/css/app.css') ile ayrıca
// yükleniyordu. Bağımsız Vite projesinde tek giriş noktası burası.
import "@/css/app.css";

import { createApp } from "vue";
import { createPinia, PiniaVuePlugin } from "pinia";
import { VueQueryPlugin } from "@tanstack/vue-query";
import axios from "axios";
import Cookies from "js-cookie";
import router from "@/router/index.js";
import Vue3Lottie from "vue3-lottie";
import PrimeVueGlobalPT from "@/config/globalpt.js";
import { useGeneralStore } from "@/stores/general_store.js";

let token = Cookies.get("token");
if (token) {
  axios.defaults.headers.common["Authorization"] = `Bearer ${token}`;
}

// Layout
import App from "@/App.vue";

// Components
import Header from "@/components/Header/Header.vue";

// PrimeVue
import PrimeVue from "primevue/config";
import PrimeVueThemeConfig from "./config/primevue-theme";
import locale_tr from "@/config/tr.json";
import KeyFilter from "primevue/keyfilter";
import Ripple from "primevue/ripple";
import Tooltip from "primevue/tooltip";
import ButtonGroup from "primevue/buttongroup";
import Button from "primevue/button";
import Select from "primevue/select";
import MultiSelect from "primevue/multiselect";
import AutoComplete from "primevue/autocomplete";
import Dialog from "primevue/dialog";
import Drawer from "primevue/drawer";
import Popover from "primevue/popover";
import FloatLabel from "primevue/floatlabel";
import Textarea from "primevue/textarea";
import InputText from "primevue/inputtext";
import InputNumber from "primevue/inputnumber";
import InputMask from "primevue/inputmask";
import InputOtp from "primevue/inputotp";
import InputGroup from "primevue/inputgroup";
import InputGroupAddon from "primevue/inputgroupaddon";
import ProgressBar from "primevue/progressbar";
import ToggleButton from "primevue/togglebutton";
import SelectButton from "primevue/selectbutton";
import DataTable from "primevue/datatable";
import Column from "primevue/column";
import ColumnGroup from "primevue/columngroup";
import Row from "primevue/row";
import Stepper from "primevue/stepper";
import StepList from "primevue/steplist";
import StepPanels from "primevue/steppanels";
import StepItem from "primevue/stepitem";
import Step from "primevue/step";
import StepPanel from "primevue/steppanel";
import ToggleSwitch from "primevue/toggleswitch";
import ProgressSpinner from "primevue/progressspinner";
import Paginator from "primevue/paginator";
import Tabs from "primevue/tabs";
import TabList from "primevue/tablist";
import Tab from "primevue/tab";
import TabPanels from "primevue/tabpanels";
import TabPanel from "primevue/tabpanel";
import DatePicker from "primevue/datepicker";
import Password from "primevue/password";
import Checkbox from "primevue/checkbox";
import Accordion from "primevue/accordion";
import AccordionPanel from "primevue/accordionpanel";
import AccordionHeader from "primevue/accordionheader";
import AccordionContent from "primevue/accordioncontent";
import Message from "primevue/message";
import Skeleton from "primevue/skeleton";
import FileUpload from "primevue/fileupload";
import Tag from "primevue/tag";
import CascadeSelect from "primevue/cascadeselect";
import Card from "primevue/card";
import Fieldset from "primevue/fieldset";
import Panel from "primevue/panel";
import Image from "primevue/image";
import ColorPicker from "primevue/colorpicker";
import IconField from "primevue/iconfield";
import InputIcon from "primevue/inputicon";
import ConfirmPopup from "primevue/confirmpopup";
import ConfirmDialog from "primevue/confirmdialog";
import ConfirmationService from "primevue/confirmationservice";

import Badge from "@/components/Badge.vue";
import DatatableAjax from "@/components/DatatableAjax.vue";
import SelectAjax from "@/components/SelectAjax.vue";
import InputErrorMessage from "@/components/InputErrorMessage.vue";
import AvatarFile from "@/components/AvatarFile.vue";
import TriggerPopover from "@/components/TriggerPopover.vue";
import ProgressSpinnerMini from "@/components/ProgressSpinnerMini.vue";
import CopyText from "@/components/CopyText.vue";

import vTooltip from "@/plugins/tooltip.js";

const pinia = createPinia();
const app = createApp(App);
app.use(pinia);
app.use(router);
app.use(VueQueryPlugin);
app.use(Vue3Lottie);
app.use(PrimeVue, {
  ripple: false,
  locale: locale_tr,
  theme: {
    preset: PrimeVueThemeConfig,
    options: {
      darkModeSelector: ".my-app-dark",
    },
  },
  pt: PrimeVueGlobalPT,
});

app.use(ConfirmationService);
app.component("Header", Header);

app.component("ButtonGroup", ButtonGroup);
app.component("Button", Button);
app.component("Select", Select);
app.component("MultiSelect", MultiSelect);
app.component("AutoComplete", AutoComplete);
app.component("Popover", Popover);
app.component("Dialog", Dialog);
app.component("Drawer", Drawer);
app.component("FloatLabel", FloatLabel);
app.component("Textarea", Textarea);
app.component("InputText", InputText);
app.component("InputNumber", InputNumber);
app.component("InputMask", InputMask);
app.component("InputOtp", InputOtp);
app.component("InputGroup", InputGroup);
app.component("InputGroupAddon", InputGroupAddon);
app.component("ProgressBar", ProgressBar);
app.component("ToggleButton", ToggleButton);
app.component("SelectButton", SelectButton);
app.component("ToggleSwitch", ToggleSwitch);
app.component("ProgressSpinner", ProgressSpinner);
app.component("Paginator", Paginator);
app.component("DatePicker", DatePicker);
app.component("Password", Password);
app.component("Checkbox", Checkbox);
app.component("Message", Message);
app.component("Skeleton", Skeleton);
app.component("FileUpload", FileUpload);
app.component("Tag", Tag);
app.component("CascadeSelect", CascadeSelect);
app.component("Card", Card);
app.component("Fieldset", Fieldset);
app.component("Panel", Panel);
app.component("Image", Image);
app.component("ColorPicker", ColorPicker);
app.component("IconField", IconField);
app.component("InputIcon", InputIcon);
app.component("ConfirmPopup", ConfirmPopup);
app.component("ConfirmDialog", ConfirmDialog);
app.component("DataTable", DataTable);
app.component("Column", Column);
app.component("ColumnGroup", ColumnGroup);
app.component("Row", Row);

app.component("Tabs", Tabs);
app.component("TabList", TabList);
app.component("Tab", Tab);
app.component("TabPanels", TabPanels);
app.component("TabPanel", TabPanel);

app.component("Stepper", Stepper);
app.component("StepList", StepList);
app.component("StepPanels", StepPanels);
app.component("StepItem", StepItem);
app.component("Step", Step);
app.component("StepPanel", StepPanel);

app.component("Accordion", Accordion);
app.component("AccordionPanel", AccordionPanel);
app.component("AccordionHeader", AccordionHeader);
app.component("AccordionContent", AccordionContent);

app.component("DatatableAjax", DatatableAjax);
app.component("SelectAjax", SelectAjax);
app.component("InputErrorMessage", InputErrorMessage);
app.component("AvatarFile", AvatarFile);
app.component("Badge", Badge);
app.component("TriggerPopover", TriggerPopover);
app.component("ProgressSpinnerMini", ProgressSpinnerMini);
app.component("CopyText", CopyText);

app.directive("ripple", Ripple);
app.directive("tooltip", vTooltip);
app.directive("pv-tooltip", Tooltip);
app.directive("keyfilter", KeyFilter);

Drawer.methods.onKeydown = function () {};
Dialog.extends.props.draggable.default = false;

app.mount("#app");
