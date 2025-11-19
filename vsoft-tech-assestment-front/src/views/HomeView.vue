<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useTasksStore } from '@/stores/tasks'
import { useUsersStore } from '@/stores/users'
import TaskColumn from '@/components/tasks/TaskColumn.vue'
import TaskModal from '@/components/tasks/TaskModal.vue'
import { toast } from 'vue-sonner'
import type { TaskStatus, CreateTaskRequest, UpdateTaskRequest, TaskResponse } from '@/lib/api/types.gen'

const tasksStore = useTasksStore()
const usersStore = useUsersStore()

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
})

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

// Abrir modal para editar task
const handleEditTask = (task: TaskResponse) => {
  editingTask.value = task
  modalStatus.value = task.status || 0
  modalOpen.value = true
}

// Salvar task (criar ou atualizar)
const handleSaveTask = async (data: CreateTaskRequest | UpdateTaskRequest) => {
  try {
    if (editingTask.value) {
      // Atualizar
      await tasksStore.updateTask(editingTask.value.id!, data as UpdateTaskRequest)
      toast.success('Tarefa atualizada com sucesso!')
    } else {
      // Criar
      await tasksStore.createTask(data as CreateTaskRequest)
      toast.success('Tarefa criada com sucesso!')
    }
    
    // Recarregar tasks
    await tasksStore.fetchTasks()
    syncLocalTasks()
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : 'Erro desconhecido'
    if (editingTask.value) {
      toast.error('Erro ao atualizar tarefa')
    } else {
      toast.error('Erro ao criar tarefa')
    }
    console.error('Erro ao salvar task:', error)
  }
}

// Quando um card é movido entre colunas
const handleTaskMove = async (taskId: string, newStatus: TaskStatus, oldStatus: TaskStatus) => {
  if (newStatus === oldStatus) return
  
  try {
    const success = await tasksStore.moveTask(taskId, newStatus)
    if (success) {
      // Recarregar tasks
      await tasksStore.fetchTasks()
      syncLocalTasks()
    }
  } catch (error) {
    // Reverter mudança local se falhar
    syncLocalTasks()
    const errorMessage = error instanceof Error ? error.message : 'Erro desconhecido'
    if (errorMessage.includes('não encontrada')) {
      toast.error('Tarefa não encontrada')
    } else {
      toast.error('Erro ao mover tarefa')
    }
    console.error('Erro ao mover task:', error)
  }
}
</script>

<template>
  <div class="flex flex-col h-full">
    <!-- Header -->
    <div class="mb-6">
      <h1 class="text-3xl font-bold tracking-tight">
        Board de Tarefas
      </h1>
      <p class="text-muted-foreground mt-2">
        Gerencie suas tarefas de forma visual
      </p>
    </div>

    <!-- Board com colunas -->
    <div class="flex-1 overflow-x-auto pb-4">
      <div class="flex gap-4 min-w-max h-full">
        <div
          v-for="column in columns"
          :key="column.status"
          :data-column-status="column.status"
          class="flex flex-col h-full min-w-[320px] max-w-[320px]"
        >
          <TaskColumn
            :title="column.title"
            :status="column.status"
            :tasks="localTasks[column.status]"
            :users="usersStore.users"
            :loading="tasksStore.loading"
            @add-task="handleAddTask(column.status)"
            @filter="(filter) => tasksStore.setFilter(column.status, filter)"
            @clear-filter="() => tasksStore.clearFilter(column.status)"
            @task-click="handleEditTask"
            @task-move="handleTaskMove"
          />
        </div>
      </div>
    </div>

    <!-- Modal de Task -->
    <TaskModal
      v-model:open="modalOpen"
      :task="editingTask"
      :status="modalStatus"
      :users="usersStore.users"
      @save="handleSaveTask"
    />
  </div>
</template>

<style scoped>
/* Scroll horizontal customizado */
.overflow-x-auto::-webkit-scrollbar {
  height: 8px;
}

.overflow-x-auto::-webkit-scrollbar-track {
  background: hsl(var(--muted));
  border-radius: 4px;
}

.overflow-x-auto::-webkit-scrollbar-thumb {
  background: hsl(var(--muted-foreground) / 0.3);
  border-radius: 4px;
}

.overflow-x-auto::-webkit-scrollbar-thumb:hover {
  background: hsl(var(--muted-foreground) / 0.5);
}
</style>
