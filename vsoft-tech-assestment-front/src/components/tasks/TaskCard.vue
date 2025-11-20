<script setup lang="ts">
import { computed } from 'vue'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import type { TaskResponse } from '@/lib/api/types.gen'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { useDateUtils } from '@/composables/useDateUtils'

const props = defineProps<{
  task: TaskResponse
}>()

const { formatDate, isOverdue: checkIsOverdue } = useDateUtils()

// Preview da descrição (primeiras linhas, sem markdown)
const descriptionPreview = computed(() => {
  if (!props.task.description) return ''
  // Remover markdown básico e pegar primeiras 100 caracteres
  const text = props.task.description
    .replace(/#{1,6}\s+/g, '') // Headers
    .replace(/\*\*(.*?)\*\*/g, '$1') // Bold
    .replace(/\*(.*?)\*/g, '$1') // Italic
    .replace(/\[([^\]]+)\]\([^\)]+\)/g, '$1') // Links
    .replace(/`([^`]+)`/g, '$1') // Code
    .trim()
  
  return text.length > 100 ? text.substring(0, 100) + '...' : text
})

// Formatar data para exibição
const formattedDate = computed(() => {
  if (!props.task.dueDate) return ''
  return formatDate(props.task.dueDate)
})

// Verificar se está vencida
const isOverdue = computed(() => {
  if (!props.task.dueDate) return false
  return checkIsOverdue(props.task.dueDate)
})
</script>

<template>
  <Card
    class="cursor-grab active:cursor-grabbing hover:shadow-md transition-shadow mb-3"
    :class="{ 'border-destructive': isOverdue }"
  >
    <CardHeader class="pb-3">
      <CardTitle class="text-base font-semibold line-clamp-2">
        {{ task.title || 'Sem título' }}
      </CardTitle>
    </CardHeader>
    <CardContent class="pt-0 space-y-2">
      <p v-if="descriptionPreview" class="text-sm text-muted-foreground line-clamp-2">
        {{ descriptionPreview }}
      </p>
      
      <div class="flex items-center justify-between text-xs text-muted-foreground">
        <span v-if="task.userName" class="truncate">
          {{ task.userName }}
        </span>
        <span
          v-if="formattedDate"
          :class="{
            'text-destructive font-medium': isOverdue,
          }"
        >
          {{ formattedDate }}
        </span>
      </div>
    </CardContent>
  </Card>
</template>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
