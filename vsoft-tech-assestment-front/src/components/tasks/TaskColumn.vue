<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-vue-next'
import TaskCard from './TaskCard.vue'
import TaskFilter from './TaskFilter.vue'
import { VueDraggable } from 'vue-draggable-plus'
import type { TaskResponse, TaskStatus } from '@/lib/api/types.gen'
import type { UserListItemResponse } from '@/stores/users'
import type { ColumnFilter } from './TaskFilter.vue'
import { useDateUtils } from '@/composables/useDateUtils'

const props = defineProps<{
  title: string
  status: TaskStatus
  tasks: TaskResponse[]
  users?: UserListItemResponse[]
  loading?: boolean
}>()

const emit = defineEmits<{
  'add-task': []
  'filter': [filter: ColumnFilter]
  'clear-filter': []
  'scroll': [event: Event, status: TaskStatus]
  'task-click': [task: TaskResponse]
  'task-move': [taskId: string, newStatus: TaskStatus, oldStatus: TaskStatus]
  'task-edit': [task: TaskResponse]
  'task-delete': [taskId: string]
}>()

// Tasks locais para drag and drop (sincronizadas com props)
const localTasks = ref<TaskResponse[]>([...props.tasks])

// Sincronizar localTasks quando props.tasks mudar
watch(() => props.tasks, (newTasks) => {
  localTasks.value = [...newTasks]
  console.log(`[TaskColumn ${props.status}] Tasks atualizadas:`, localTasks.value.length, localTasks.value.map(t => ({ id: t.id, title: t.title })))
}, { deep: true, immediate: true })

const filter = ref<ColumnFilter>({})
const columnRef = ref<HTMLElement>()

// Usar composable compartilhado para manipulação de datas
const { normalizeDate } = useDateUtils()

// Função para verificar se uma task passa no filtro
const taskPassesFilter = (task: TaskResponse): boolean => {
  if (filter.value.title) {
    const searchTerm = filter.value.title.toLowerCase()
    if (!task.title?.toLowerCase().includes(searchTerm)) {
      return false
    }
  }

  if (filter.value.dueDateFrom) {
    if (!task.dueDate) {
      return false
    }
    // Usar Luxon para comparar datas corretamente
    const fromDate = normalizeDate(filter.value.dueDateFrom)
    const taskDate = normalizeDate(task.dueDate)
    if (!taskDate.isValid || taskDate < fromDate) {
      return false
    }
  }

  if (filter.value.dueDateTo) {
    if (!task.dueDate) {
      return false
    }
    // Usar Luxon para comparar datas corretamente
    const toDate = normalizeDate(filter.value.dueDateTo)
    const taskDate = normalizeDate(task.dueDate)
    if (!taskDate.isValid || taskDate > toDate) {
      return false
    }
  }

  if (filter.value.userId && task.userId !== filter.value.userId) {
    return false
  }

  return true
}

const filteredTasks = computed(() => {
  return localTasks.value.filter(taskPassesFilter)
})

// Handler de drag end - detectar mudança de coluna
const handleDragEnd = async (evt: any) => {
  try {
    const { item, to, from } = evt
    
    if (!to || !from) return
    
    // Encontrar colunas de origem e destino
    const fromColumn = from.closest('[data-column-status]')
    const toColumn = to.closest('[data-column-status]')
    
    if (!fromColumn || !toColumn) return
    
    const fromStatus = parseInt(fromColumn.dataset.columnStatus || '0') as TaskStatus
    const toStatus = parseInt(toColumn.dataset.columnStatus || '0') as TaskStatus
    
    // Se não mudou de coluna, não fazer nada
    if (fromStatus === toStatus) return
    
    // Encontrar task pelo elemento
    const taskElement = item.querySelector('[data-task-id]') || item
    const taskId = taskElement.dataset?.taskId || taskElement.getAttribute('data-task-id')
    
    if (!taskId) {
      // Tentar encontrar pela estrutura do vue-draggable
      const context = (item as any).__draggable_context__
      if (context?.element?.id) {
        emit('task-move', context.element.id, toStatus, fromStatus)
      }
      return
    }
    
    // Emitir evento de mudança de coluna
    emit('task-move', taskId, toStatus, fromStatus)
  } catch (error) {
    console.error('Erro ao processar drag end:', error)
  }
}

const handleFilter = (newFilter: ColumnFilter) => {
  filter.value = newFilter
  emit('filter', newFilter)
}

const handleClearFilter = () => {
  filter.value = {}
  emit('clear-filter')
}
</script>

<template>
  <div class="flex flex-col h-full min-w-[320px] max-w-[320px] bg-muted/30 rounded-lg border border-border/50">
    <!-- Header -->
    <div class="flex items-center justify-between p-4 border-b border-border/50">
      <h3 class="font-semibold text-sm">{{ title }}</h3>
      <div class="flex items-center gap-2">
        <TaskFilter
          :status="status"
          :users="users"
          @filter="handleFilter"
          @clear="handleClearFilter"
        />
        <Button
          variant="ghost"
          size="icon"
          class="h-8 w-8"
          @click="emit('add-task')"
        >
          <Plus class="h-4 w-4" />
        </Button>
      </div>
    </div>

    <!-- Tasks Container with Scroll -->
    <div
      ref="columnRef"
      :data-column-status="status"
      class="flex-1 overflow-y-auto p-4"
      style="max-height: calc(100vh - 200px);"
      @scroll="(e) => emit('scroll', e, status)"
    >
      <div v-if="loading && localTasks.length === 0" class="text-center text-sm text-muted-foreground py-8">
        Carregando...
      </div>
      
      <VueDraggable
        v-else
        :key="`draggable-${props.status}-${localTasks.length}`"
        v-model="localTasks"
        :group="{ name: 'tasks', pull: true, put: true }"
        :animation="200"
        handle=".cursor-grab"
        class="space-y-3"
        @end="handleDragEnd"
      >
        <template v-if="localTasks.length === 0">
          <div class="text-center text-sm text-muted-foreground py-8">
            <p>Nenhuma tarefa</p>
          </div>
        </template>
        <template v-else-if="filteredTasks.length === 0">
          <!-- Tasks escondidas para manter no DOM para drag and drop -->
          <div
            v-for="task in localTasks"
            :key="task.id || `temp-${task.title}`"
            :data-task-id="task.id"
            :data-task-status="task.status"
            class="hidden"
          >
            <TaskCard
              :task="task"
              :current-status="props.status"
              @edit="emit('task-edit', $event)"
              @delete="emit('task-delete', $event)"
              @move="(taskId, newStatus) => emit('task-move', taskId, newStatus, props.status)"
            />
          </div>
          <div class="text-center text-sm text-muted-foreground py-8 mt-4">
            <p>Nenhuma tarefa corresponde aos filtros</p>
          </div>
        </template>
        <template v-else>
          <template v-for="task in localTasks" :key="task.id || `temp-${task.title}`">
            <div
              v-if="taskPassesFilter(task)"
              :data-task-id="task.id"
              :data-task-status="task.status"
              @click="emit('task-click', task)"
              class="cursor-pointer"
            >
              <TaskCard
                :task="task"
                :current-status="props.status"
                @edit="emit('task-edit', $event)"
                @delete="emit('task-delete', $event)"
                @move="(taskId, newStatus) => emit('task-move', taskId, newStatus, props.status)"
              />
            </div>
          </template>
        </template>
      </VueDraggable>
    </div>
  </div>
</template>

<style scoped>
/* Custom scrollbar */
.overflow-y-auto::-webkit-scrollbar {
  width: 6px;
}

.overflow-y-auto::-webkit-scrollbar-track {
  background: transparent;
}

.overflow-y-auto::-webkit-scrollbar-thumb {
  background: hsl(var(--muted-foreground) / 0.3);
  border-radius: 3px;
}

.overflow-y-auto::-webkit-scrollbar-thumb:hover {
  background: hsl(var(--muted-foreground) / 0.5);
}
</style>

