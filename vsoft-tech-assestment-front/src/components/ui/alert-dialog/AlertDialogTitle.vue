<script setup lang="ts">
import type { AlertDialogTitleProps } from 'reka-ui'
import { AlertDialogTitle } from 'reka-ui'
import { cn } from '@/lib/utils'

const props = withDefaults(defineProps<AlertDialogTitleProps & { class?: string }>(), {
  as: 'h2',
})
</script>

<template>
  <AlertDialogTitle
    v-bind="props"
    :class="cn('text-lg font-semibold leading-none tracking-tight', props.class)"
  >
    <slot />
  </AlertDialogTitle>
</template>


