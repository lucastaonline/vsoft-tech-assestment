<script setup lang="ts">
import { computed } from 'vue'
import { MoreVertical, Edit, Trash2, ArrowRight } from 'lucide-vue-next'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { TaskResponse, TaskStatus } from '@/lib/api/types.gen'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { useDateUtils } from '@/composables/useDateUtils'

const props = defineProps<{
  task: TaskResponse
  currentStatus: TaskStatus
}>()

const emit = defineEmits<{
  edit: [task: TaskResponse]
  delete: [taskId: string]
  move: [taskId: string, newStatus: TaskStatus]
}>()

const { formatDate, isOverdue: checkIsOverdue } = useDateUtils()

// Obter outras colunas disponíveis (excluindo a atual)
const availableStatuses = computed(() => {
  const allStatuses: TaskStatus[] = [0, 1, 2]
  return allStatuses.filter(status => status !== props.currentStatus)
})

// Nomes das colunas
const statusNames: Record<TaskStatus, string> = {
  0: 'To Do',
  1: 'In Progress',
  2: 'Done',
}

// Handlers
const handleEdit = () => {
  emit('edit', props.task)
}

const handleDelete = () => {
  if (!props.task.id) return
  
  if (confirm('Tem certeza que deseja deletar esta tarefa?')) {
    emit('delete', props.task.id)
  }
}

const handleMove = (newStatus: TaskStatus) => {
  if (props.task.id) {
    emit('move', props.task.id, newStatus)
  }
}

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
    class="cursor-grab active:cursor-grabbing hover:shadow-md transition-shadow mb-3 relative"
    :class="{ 'border-destructive': isOverdue }"
  >
    <!-- Menu de ações no canto superior direito -->
    <div class="absolute top-2 right-2 z-10" @click.stop>
      <DropdownMenu>
        <DropdownMenuTrigger as-child>
          <button
            class="rounded-md p-1 hover:bg-muted transition-colors"
            @click.stop
          >
            <MoreVertical class="h-4 w-4 text-muted-foreground" />
          </button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" class="w-48">
          <DropdownMenuItem @click="handleEdit" class="cursor-pointer">
            <Edit class="mr-2 h-4 w-4" />
            Editar
          </DropdownMenuItem>
          
          <DropdownMenuSeparator />
          
          <DropdownMenuSub v-if="availableStatuses.length > 0">
            <DropdownMenuSubTrigger class="cursor-pointer">
              <ArrowRight class="mr-2 h-4 w-4" />
              Mover para
            </DropdownMenuSubTrigger>
            <DropdownMenuSubContent>
              <DropdownMenuItem
                v-for="status in availableStatuses"
                :key="status"
                @click="handleMove(status)"
                class="cursor-pointer"
              >
                {{ statusNames[status] }}
              </DropdownMenuItem>
            </DropdownMenuSubContent>
          </DropdownMenuSub>
          
          <DropdownMenuSeparator />
          
          <DropdownMenuItem
            @click="handleDelete"
            class="cursor-pointer text-destructive focus:text-destructive"
          >
            <Trash2 class="mr-2 h-4 w-4" />
            Deletar
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>

    <CardHeader class="pb-3">
      <CardTitle class="text-base font-semibold line-clamp-2 pr-8">
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
