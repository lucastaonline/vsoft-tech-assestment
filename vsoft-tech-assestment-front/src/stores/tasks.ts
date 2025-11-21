import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as tasksService from '@/services/tasksService'
import type {
    TaskResponse,
    TaskStatus,
    CreateTaskRequest,
    UpdateTaskRequest
} from '@/lib/api/types.gen'
import type { ColumnFilter } from '@/components/tasks/TaskFilter.vue'

export const useTasksStore = defineStore('tasks', () => {
    const tasks = ref<TaskResponse[]>([])
    const loading = ref(false)

    const filters = ref<Record<TaskStatus, ColumnFilter>>({
        0: {},
        1: {},
        2: {},
    })

    // Computed: tasks agrupadas por status
    const tasksByStatus = computed(() => {
        const result: Record<TaskStatus, TaskResponse[]> = {
            0: [],
            1: [],
            2: [],
        }

        tasks.value.forEach(task => {
            if (task.status !== undefined) {
                result[task.status].push(task)
            }
        })

        return result
    })


    const fetchTasks = async () => {
        loading.value = true
        try {
            const data = await tasksService.listTasks()
            tasks.value = data
        } catch (error) {
            console.error('Erro ao carregar tasks:', error)
            throw error
        } finally {
            loading.value = false
        }
    }

    // Criar task
    const createTask = async (data: CreateTaskRequest): Promise<TaskResponse | null> => {
        try {
            const newTask = await tasksService.createTask(data)
            tasks.value.push(newTask)
            return newTask
        } catch (error) {
            console.error('Erro ao criar task:', error)
            throw error
        }
    }

    // Atualizar task (com atualização otimista)
    const updateTask = async (id: string, data: UpdateTaskRequest): Promise<TaskResponse> => {
        const taskIndex = tasks.value.findIndex(t => t.id === id)
        if (taskIndex === -1) {
            throw new Error('Tarefa não encontrada')
        }

        // Salvar estado anterior para possível reversão
        const previousTask = { ...tasks.value[taskIndex] }

        // Atualização otimista: atualizar localmente primeiro
        tasks.value[taskIndex] = {
            ...tasks.value[taskIndex],
            ...data,
            id,
            updatedAt: new Date().toISOString(),
        }

        try {
            // Confirmar no servidor
            const updatedTask = await tasksService.updateTask(id, data)

            // Atualizar com a resposta do servidor (pode ter campos calculados)
            tasks.value[taskIndex] = updatedTask

            return updatedTask
        } catch (error) {
            // Reverter em caso de erro
            tasks.value[taskIndex] = previousTask
            console.error('Erro ao atualizar task:', error)
            throw error
        }
    }

    // Mover task (mudar status) - com atualização otimista
    const moveTask = async (taskId: string, newStatus: TaskStatus): Promise<TaskResponse> => {
        const taskIndex = tasks.value.findIndex(t => t.id === taskId)
        if (taskIndex === -1) {
            throw new Error('Tarefa não encontrada')
        }

        const task = tasks.value[taskIndex]
        if (!task || !task.id) {
            throw new Error('Tarefa inválida')
        }

        // Salvar estado anterior para possível reversão
        const previousTask: TaskResponse = { ...task } as TaskResponse

        // Atualização otimista: atualizar localmente primeiro (feedback imediato)
        tasks.value[taskIndex] = {
            ...task,
            status: newStatus,
            updatedAt: new Date().toISOString(),
        } as TaskResponse

        try {
            // Confirmar no servidor e obter task atualizada
            // Garantir que task tem todos os campos necessários
            const taskForService: TaskResponse = {
                id: task.id,
                title: task.title || '',
                description: task.description || '',
                dueDate: task.dueDate || new Date().toISOString(),
                status: task.status ?? 0,
                userId: task.userId || '',
                userName: task.userName,
                createdAt: task.createdAt || new Date().toISOString(),
                updatedAt: task.updatedAt,
            }

            const updatedTask = await tasksService.moveTask(taskId, newStatus, taskForService)

            // Atualizar com a resposta do servidor (pode ter campos calculados como UpdatedAt)
            tasks.value[taskIndex] = updatedTask

            return updatedTask
        } catch (error) {
            // Reverter em caso de erro
            tasks.value[taskIndex] = previousTask
            console.error('Erro ao mover task:', error)
            throw error
        }
    }

    // Deletar task (com atualização otimista)
    const deleteTask = async (id: string): Promise<void> => {
        const taskIndex = tasks.value.findIndex(t => t.id === id)
        if (taskIndex === -1) {
            throw new Error('Tarefa não encontrada')
        }

        // Salvar task para possível reversão
        const deletedTask = tasks.value[taskIndex]
        if (!deletedTask) {
            throw new Error('Tarefa inválida')
        }

        // Atualização otimista: remover localmente primeiro
        tasks.value = tasks.value.filter(t => t.id !== id)

        try {
            // Confirmar no servidor
            await tasksService.deleteTask(id)
            // Se chegou aqui, a deleção foi confirmada
        } catch (error) {
            // Reverter em caso de erro: re-inserir a task na posição original
            tasks.value.splice(taskIndex, 0, deletedTask as TaskResponse)
            console.error('Erro ao deletar task:', error)
            throw error
        }
    }

    // Definir filtro para uma coluna
    const setFilter = (status: TaskStatus, filter: ColumnFilter) => {
        filters.value[status] = filter
    }

    // Limpar filtro de uma coluna
    const clearFilter = (status: TaskStatus) => {
        filters.value[status] = {}
    }

    // Buscar e adicionar/atualizar uma tarefa específica
    const fetchAndUpdateTask = async (taskId: string) => {
        try {
            const task = await tasksService.getTaskById(taskId)

            // Verificar se a tarefa já existe
            const existingIndex = tasks.value.findIndex(t => t.id === taskId)

            if (existingIndex !== -1) {
                // Atualizar tarefa existente
                tasks.value[existingIndex] = task
            } else {
                // Adicionar nova tarefa
                tasks.value.push(task)
            }

            return task
        } catch (error) {
            console.error('Erro ao buscar tarefa:', error)
            throw error
        }
    }

    return {
        // State
        tasks,
        loading,
        filters,

        // Computed
        tasksByStatus,

        // Actions
        fetchTasks,
        createTask,
        updateTask,
        moveTask,
        deleteTask,
        setFilter,
        clearFilter,
        fetchAndUpdateTask,
    }
})

