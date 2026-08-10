<template>
  <div>
    <WorkType />
    <Department />
    <PaymentType />
    <LoadingType />
    <LoadStatusType />
    <VesselType />
    <AccountType />
    <TransportType />
    <MovementType />
    <LoadProductType />
    <LoadFinancialItemType />
    <LoadFinancialItem />
    <TransitDeclarationDocumentType />
    <AccountGroupCode />
    <AccountPlan />
    <Destination />
  </div>
</template>

<script setup>
import { defineAsyncComponent } from "vue";
const WorkType = defineAsyncComponent(() => import("@/components/FeatureModals/WorkType.vue"));
const Department = defineAsyncComponent(() => import("@/components/FeatureModals/Department.vue"));
const PaymentType = defineAsyncComponent(() => import("@/components/FeatureModals/PaymentType.vue"));
const LoadingType = defineAsyncComponent(() => import("@/components/FeatureModals/LoadingType.vue"));
const LoadStatusType = defineAsyncComponent(() => import("@/components/FeatureModals/LoadStatusType.vue"));
const VesselType = defineAsyncComponent(() => import("@/components/FeatureModals/VesselType.vue"));
const AccountType = defineAsyncComponent(() => import("@/components/FeatureModals/AccountType.vue"));
const TransportType = defineAsyncComponent(() => import("@/components/FeatureModals/TransportType.vue"));
const MovementType = defineAsyncComponent(() => import("@/components/FeatureModals/MovementType.vue"));
const LoadProductType = defineAsyncComponent(() => import("@/components/FeatureModals/LoadProductType.vue"));
const LoadFinancialItemType = defineAsyncComponent(() => import("@/components/FeatureModals/LoadFinancialItemType.vue"));
const LoadFinancialItem = defineAsyncComponent(() => import("@/components/FeatureModals/LoadFinancialItem.vue"));
const TransitDeclarationDocumentType = defineAsyncComponent(() => import("@/components/FeatureModals/TransitDeclarationDocumentType.vue"));
const AccountGroupCode = defineAsyncComponent(() => import("@/components/FeatureModals/AccountGroupCode.vue"));
const AccountPlan = defineAsyncComponent(() => import("@/components/FeatureModals/AccountPlan.vue"));
const Destination = defineAsyncComponent(() => import("@/components/FeatureModals/Destination.vue"));
</script>
