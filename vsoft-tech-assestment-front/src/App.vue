<script setup lang="ts">
import BaseLayout from '@/layouts/BaseLayout.vue'
import CookieConsent from '@/components/CookieConsent.vue'
</script>

<template>
  <BaseLayout>
    <router-view />
    <CookieConsent />
  </BaseLayout>
</template>

<style scoped></style>
