<script setup lang="ts">
import type { AlertDialogContentProps } from 'reka-ui'
import { AlertDialogContent } from 'reka-ui'
import { cn } from '@/lib/utils'

const props = withDefaults(defineProps<AlertDialogContentProps & { class?: string }>(), {
  as: 'div',
})
</script>

<template>
  <AlertDialogContent
    v-bind="props"
    :class="cn(
      'fixed left-1/2 top-1/2 z-50 grid w-full max-w-lg translate-x-[-50%] translate-y-[-50%] gap-4 border bg-background p-6 shadow-lg duration-200 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=open]:zoom-in-95 data-[state=closed]:zoom-out-95 data-[state=open]:fade-in-0 data-[state=closed]:fade-out-0 sm:rounded-lg',
      props.class,
    )"
  >
    <slot />
  </AlertDialogContent>
</template>


