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
import type { PaginatedTasksResponse } from '@/services/tasksService'

interface PaginationState {
    cursor: string | null
    hasMore: boolean
    loading: boolean
}

export const useTasksStore = defineStore('tasks', () => {
    // Estado
    const tasks = ref<TaskResponse[]>([])
    const loading = ref(false)

    // Paginação por status
    const pagination = ref<Record<TaskStatus, PaginationState>>({
        0: { cursor: null, hasMore: false, loading: false }, // Pending
        1: { cursor: null, hasMore: false, loading: false }, // InProgress
        2: { cursor: null, hasMore: false, loading: false }, // Completed
    })

    // Filtros por coluna
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


    // Carregar tasks com paginação
    const fetchTasks = async (status?: TaskStatus, reset = false) => {
        loading.value = true

        try {
            // Se reset, limpar tasks do status
            if (reset && status !== undefined) {
                tasks.value = tasks.value.filter(t => t.status !== status)
                pagination.value[status] = { cursor: null, hasMore: false, loading: false }
            }

            // Construir parâmetros para paginação
            const params: tasksService.ListTasksParams = {
                pageSize: 20,
            }

            if (status !== undefined && !reset && pagination.value[status].cursor) {
                params.cursor = pagination.value[status].cursor
            }

            const data = await tasksService.listTasks(params)

            // Quando há parâmetros de paginação, a API sempre retorna formato paginado
            // Quando não há parâmetros, pode retornar lista simples (compatibilidade)
            if (Array.isArray(data) && !params.cursor && !params.pageSize) {
                // Lista simples (sem parâmetros de paginação)
                if (reset || status === undefined) {
                    tasks.value = data
                } else {
                    // Adicionar tasks do status específico
                    const statusTasks = data.filter(t => t.status === status)
                    tasks.value = [
                        ...tasks.value.filter(t => t.status !== status),
                        ...statusTasks,
                    ]
                }
            } else {
                // Resposta paginada (quando há parâmetros ou formato paginado)
                const paginatedData = Array.isArray(data)
                    ? { tasks: data, nextCursor: null, hasMore: false }
                    : (data as PaginatedTasksResponse)

                if (status !== undefined) {
                    const statusTasks = paginatedData.tasks.filter(t => t.status === status)
                    tasks.value = [
                        ...tasks.value.filter(t => t.status !== status),
                        ...statusTasks,
                    ]

                    pagination.value[status] = {
                        cursor: paginatedData.nextCursor || null,
                        hasMore: paginatedData.hasMore || false,
                        loading: false,
                    }
                } else {
                    // Sem filtro de status, adicionar todas
                    if (reset) {
                        tasks.value = paginatedData.tasks
                    } else {
                        // Evitar duplicatas
                        const existingIds = new Set(tasks.value.map(t => t.id))
                        const newTasks = paginatedData.tasks.filter(t => t.id && !existingIds.has(t.id))
                        tasks.value = [...tasks.value, ...newTasks]
                    }
                }
            }
        } catch (error) {
            console.error('Erro ao carregar tasks:', error)
            throw error
        } finally {
            loading.value = false
        }
    }

    // Carregar mais tasks para um status (paginação)
    const loadMoreTasks = async (status: TaskStatus) => {
        const statusPagination = pagination.value[status]

        if (!statusPagination.hasMore || statusPagination.loading) {
            return
        }

        statusPagination.loading = true

        try {
            const params: tasksService.ListTasksParams = {
                cursor: statusPagination.cursor || undefined,
                pageSize: 20,
            }

            const data = await tasksService.listTasks(params)

            // Quando há parâmetros de paginação, a API sempre retorna formato paginado
            const paginatedData = Array.isArray(data)
                ? { tasks: data, nextCursor: null, hasMore: false }
                : (data as PaginatedTasksResponse)

            const statusTasks = paginatedData.tasks.filter(t => t.status === status)
            const existingIds = new Set(tasks.value.map(t => t.id))
            const newTasks = statusTasks.filter(t => t.id && !existingIds.has(t.id))
            tasks.value = [...tasks.value, ...newTasks]

            statusPagination.cursor = paginatedData.nextCursor || null
            statusPagination.hasMore = paginatedData.hasMore || false
        } catch (error) {
            console.error('Erro ao carregar mais tasks:', error)
            throw error
        } finally {
            statusPagination.loading = false
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
        pagination,
        filters,

        // Computed
        tasksByStatus,

        // Actions
        fetchTasks,
        loadMoreTasks,
        createTask,
        updateTask,
        moveTask,
        deleteTask,
        setFilter,
        clearFilter,
        fetchAndUpdateTask,
    }
})

