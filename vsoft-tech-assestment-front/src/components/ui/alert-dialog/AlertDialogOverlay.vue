<script setup lang="ts">
import type { AlertDialogOverlayProps } from 'reka-ui'
import { AlertDialogOverlay } from 'reka-ui'
import { cn } from '@/lib/utils'

const props = withDefaults(defineProps<AlertDialogOverlayProps & { class?: string }>(), {
  as: 'div',
})
</script>

<template>
  <AlertDialogOverlay
    v-bind="props"
    :class="cn('fixed inset-0 z-50 bg-background/80 backdrop-blur-sm data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=open]:fade-in-0 data-[state=closed]:fade-out-0', props.class)"
  />
</template>
