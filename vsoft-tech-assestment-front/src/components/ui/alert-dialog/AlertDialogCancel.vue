<script setup lang="ts">
import type { AlertDialogCancelProps } from 'reka-ui'
import { AlertDialogCancel } from 'reka-ui'
import { cn } from '@/lib/utils'

const props = withDefaults(defineProps<AlertDialogCancelProps & { class?: string }>(), {
  as: 'button',
})
</script>

<template>
  <AlertDialogCancel
    v-bind="props"
    :class="cn('inline-flex h-10 items-center justify-center rounded-md border border-input bg-background px-4 py-2 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 hover:bg-accent hover:text-accent-foreground', props.class)"
  >
    <slot />
  </AlertDialogCancel>
</template>


