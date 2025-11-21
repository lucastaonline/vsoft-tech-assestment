<script setup lang="ts">
import { onMounted, onUnmounted, ref, watch } from 'vue'
import { useTasksStore } from '@/stores/tasks'
import { useUsersStore } from '@/stores/users'
import { useAuthStore } from '@/stores/auth'
import { useSignalR } from '@/composables/useSignalR'
import TaskColumn from '@/components/tasks/TaskColumn.vue'
import TaskModal from '@/components/tasks/TaskModal.vue'
import { toast } from 'vue-sonner'
import type { TaskStatus, CreateTaskRequest, UpdateTaskRequest, TaskResponse } from '@/lib/api/types.gen'
import { ApiError } from '@/services/tasksService'
import type { NotificationResponse } from '@/services/notificationsService'

const tasksStore = useTasksStore()
const usersStore = useUsersStore()
const authStore = useAuthStore()
const signalR = useSignalR()

// Status das colunas
const columns = [
  { title: 'To Do', status: 0 as TaskStatus },
  { title: 'In Progress', status: 1 as TaskStatus },
  { title: 'Done', status: 2 as TaskStatus },
]

// Modal state
const modalOpen = ref(false)
const modalStatus = ref<TaskStatus>(0)
const editingTask = ref<TaskResponse | null>(null)

// Tasks locais por status (para drag and drop)
const localTasks = ref<Record<TaskStatus, TaskResponse[]>>({
  0: [],
  1: [],
  2: [],
})

// Sincronizar tasks locais com store
const syncLocalTasks = () => {
  const tasksByStatus = tasksStore.tasksByStatus
  localTasks.value = {
    0: [...tasksByStatus[0]],
    1: [...tasksByStatus[1]],
    2: [...tasksByStatus[2]],
  }
}

// Handler para notificações de tarefas atribuídas
const handleTaskNotification = async (notification: NotificationResponse) => {
  // Se a notificação tem um TaskId, buscar e atualizar a tarefa
  if (notification.taskId) {
    try {
      await tasksStore.fetchAndUpdateTask(notification.taskId)
      // syncLocalTasks será chamado automaticamente pelo watch
    } catch (error) {
      console.error('Erro ao atualizar tarefa da notificação:', error)
      // Não mostrar toast de erro para não poluir a interface
    }
  }
}

// Carregar dados iniciais
onMounted(async () => {
  try {
    await usersStore.fetchUsers()
  } catch (error) {
    toast.error('Erro ao carregar lista de usuários')
  }

  try {
    await tasksStore.fetchTasks()
    syncLocalTasks()
  } catch (error) {
    toast.error('Erro ao carregar tarefas')
  }

  if (authStore.isAuthenticated) {
    await signalR.connect()
    signalR.onNotification(handleTaskNotification)
  }
})

onUnmounted(async () => {
  await signalR.disconnect()
})

// Reconectar quando autenticação mudar
watch(
  () => authStore.isAuthenticated,
  async (isAuthenticated) => {
    if (isAuthenticated) {
      await signalR.connect()
      signalR.onNotification(handleTaskNotification)
    } else {
      await signalR.disconnect()
    }
  }
)

// Observar mudanças na store e sincronizar
watch(() => tasksStore.tasksByStatus, () => {
  syncLocalTasks()
}, { deep: true, immediate: true })

// Abrir modal para criar task
const handleAddTask = (status: TaskStatus) => {
  modalStatus.value = status
  editingTask.value = null
  modalOpen.value = true
}

// Verificar se o usuário é o dono da tarefa
const isTaskOwner = (task: TaskResponse): boolean => {
  return authStore.user?.id === task.userId
}

// Abrir modal para editar task (apenas se for o dono)
const handleEditTask = (task: TaskResponse) => {
  if (!isTaskOwner(task)) {
    toast.error('Você não tem permissão para editar esta tarefa')
    return
  }
  editingTask.value = task
  modalStatus.value = task.status || 0
  modalOpen.value = true
}

// Salvar task (criar ou atualizar)
const handleSaveTask = async (data: CreateTaskRequest | UpdateTaskRequest) => {
  try {
    if (editingTask.value) {
      // Verificar novamente se é o dono antes de atualizar
      if (!isTaskOwner(editingTask.value)) {
        toast.error('Você não tem permissão para editar esta tarefa')
        return
      }
      // Atualizar (já atualiza localmente via store)
      await tasksStore.updateTask(editingTask.value.id!, data as UpdateTaskRequest)
      toast.success('Tarefa atualizada com sucesso!')
    } else {
      // Criar (já adiciona localmente via store)
      await tasksStore.createTask(data as CreateTaskRequest)
      toast.success('Tarefa criada com sucesso!')
    }

    // Não precisa recarregar - o store já atualizou localmente
    // O watch sincroniza automaticamente
  } catch (error) {
    console.error('Erro ao salvar task:', error)

    if (error instanceof ApiError) {
      // Log detalhado para debug
      console.log('ApiError - status:', error.status)
      console.log('ApiError - message:', error.message)
      console.log('ApiError - isForbidden:', error.isForbidden)
      console.log('ApiError - isNotFound:', error.isNotFound)
      console.log('ApiError - isValidationError:', error.isValidationError)
      console.log('ApiError - validationErrors:', error.validationErrors)
      console.log('ApiError - originalError:', error.originalError)

      if (error.isForbidden) {
        toast.error('Você não tem permissão para realizar esta ação')
      } else if (error.isNotFound) {
        toast.error('Tarefa não encontrada')
      } else if (error.status === 400 && error.validationErrors && Object.keys(error.validationErrors).length > 0) {
        // Exibir erros de validação específicos
        const errorMessages: string[] = []
        for (const [field, messages] of Object.entries(error.validationErrors)) {
          // Formatar nome do campo: "Title" -> "Title", "DueDate" -> "Due Date"
          const fieldName = field.charAt(0).toUpperCase() + field.slice(1).replace(/([A-Z])/g, ' $1').trim()
          messages.forEach(msg => {
            errorMessages.push(`${fieldName}: ${msg}`)
          })
        }
        if (errorMessages.length > 0) {
          // Exibir todos os erros em um único toast com título genérico
          toast.error('Erro de validação', {
            description: errorMessages.join('\n')
          })
        } else {
          // Se não conseguiu extrair mensagens específicas, mostrar a mensagem do erro
          const errorMsg = error.message || 'Erro de validação'
          toast.error(errorMsg)
        }
      } else {
        // Para outros erros, sempre exibir a mensagem específica se disponível
        const errorMsg = error.message || (editingTask.value ? 'Erro ao atualizar tarefa' : 'Erro ao criar tarefa')
        toast.error(errorMsg)
      }
    } else {
      // Para erros não-ApiError, tentar extrair mensagem
      const errorMsg = error instanceof Error ? error.message : String(error)
      toast.error(errorMsg || (editingTask.value ? 'Erro ao atualizar tarefa' : 'Erro ao criar tarefa'))
    }
  }
}

// Deletar task
const handleDeleteTask = async (taskId: string) => {
  try {
    const task = tasksStore.tasks.find(t => t.id === taskId)
    if (!task) {
      toast.error('Tarefa não encontrada')
      return
    }

    // Verificar se é o dono
    if (!isTaskOwner(task)) {
      toast.error('Você não tem permissão para deletar esta tarefa')
      return
    }

    // Deletar (já remove localmente via store com atualização otimista)
    await tasksStore.deleteTask(taskId)
    toast.success('Tarefa deletada com sucesso!')

    // Não precisa recarregar - o store já removeu localmente
    // O watch sincroniza automaticamente
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.isForbidden) {
        toast.error('Você não tem permissão para deletar esta tarefa')
      } else if (error.isNotFound) {
        toast.error('Tarefa não encontrada')
      } else {
        toast.error(error.message || 'Erro ao deletar tarefa')
      }
    } else {
      toast.error('Erro ao deletar tarefa')
    }
    console.error('Erro ao deletar task:', error)
  }
}

// Quando um card é movido entre colunas
const handleTaskMove = async (taskId: string, newStatus: TaskStatus, oldStatus: TaskStatus) => {
  if (newStatus === oldStatus) return

  try {
    // Mover (já atualiza localmente via store com atualização otimista)
    await tasksStore.moveTask(taskId, newStatus)
    // Não precisa recarregar - o store já atualizou localmente
    // O watch sincroniza automaticamente
  } catch (error) {
    // O store já reverte automaticamente em caso de erro
    if (error instanceof ApiError) {
      if (error.isForbidden) {
        toast.error('Você não tem permissão para mover esta tarefa')
      } else if (error.isNotFound) {
        toast.error('Tarefa não encontrada')
      } else {
        toast.error(error.message || 'Erro ao mover tarefa')
      }
    } else {
      const errorMessage = error instanceof Error ? error.message : 'Erro desconhecido'
      toast.error('Erro ao mover tarefa')
    }
    console.error('Erro ao mover task:', error)
  }
}
</script>

<template>
  <div class="flex h-[calc(100vh-4rem-1rem)] flex-col overflow-hidden">
    <div class="mb-6 shrink-0">
      <h1 class="text-3xl font-bold tracking-tight">
        Board de Tarefas
      </h1>
      <p class="text-muted-foreground mt-2">
        Gerencie suas tarefas de forma visual
      </p>
    </div>

    <div class="flex-1 min-h-0 overflow-hidden">
      <div class="h-full min-h-0 overflow-x-auto overflow-y-hidden px-3 pb-4">
        <div class="flex h-full min-h-0 gap-4 min-w-max">
          <div v-for="column in columns" :key="column.status" :data-column-status="column.status"
            class="flex h-full min-h-0 max-h-full flex-col min-w-[320px] max-w-[320px]">
            <TaskColumn :title="column.title" :status="column.status" :tasks="localTasks[column.status]"
              :users="usersStore.users" :loading="tasksStore.loading" @add-task="handleAddTask(column.status)"
              @filter="(filter) => tasksStore.setFilter(column.status, filter)"
              @clear-filter="() => tasksStore.clearFilter(column.status)" @task-click="handleEditTask"
              @task-move="handleTaskMove" @task-edit="handleEditTask" @task-delete="handleDeleteTask" />
          </div>
        </div>
      </div>
    </div>

    <TaskModal v-model:open="modalOpen" :task="editingTask" :status="modalStatus" :users="usersStore.users"
      :can-edit="editingTask ? isTaskOwner(editingTask) : true" @save="handleSaveTask" @delete="handleDeleteTask" />
  </div>
</template>

<style scoped></style>
