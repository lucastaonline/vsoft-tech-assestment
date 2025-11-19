import {
    getApiTasks,
    postApiTasks,
    putApiTasksById,
    deleteApiTasksById
} from '@/lib/api/sdk.gen'
import type {
    TaskResponse,
    TaskStatus,
    CreateTaskRequest,
    UpdateTaskRequest,
    ProblemDetails
} from '@/lib/api/types.gen'

// Tipo para resposta paginada
export interface PaginatedTasksResponse {
    tasks: TaskResponse[]
    nextCursor: string | null
    hasMore: boolean
}

// Tipo para resposta da API (pode ser lista simples ou paginada)
type TasksApiResponse = TaskResponse[] | PaginatedTasksResponse

// Parâmetros para listar tasks
export interface ListTasksParams {
    cursor?: string | null
    pageSize?: number
}

// Função auxiliar para extrair mensagem de erro
function getErrorMessage(error: unknown): string {
    if (typeof error === 'string') {
        return error
    }
    if (error && typeof error === 'object') {
        const problemDetails = error as ProblemDetails
        return problemDetails.detail || problemDetails.title || 'Erro desconhecido'
    }
    return 'Erro desconhecido'
}

/**
 * Lista tasks com suporte a paginação
 */
export async function listTasks(params?: ListTasksParams): Promise<TasksApiResponse> {
    const queryParams: Record<string, string> = {}

    if (params?.cursor) {
        queryParams.cursor = params.cursor
    }

    if (params?.pageSize) {
        queryParams.pageSize = params.pageSize.toString()
    }

    const response = await getApiTasks({
        query: queryParams as any,
    })

    if (response.error) {
        throw new Error(getErrorMessage(response.error))
    }

    if (!response.data) {
        throw new Error('Resposta vazia do servidor')
    }

    return response.data as TasksApiResponse
}

/**
 * Cria uma nova task
 */
export async function createTask(data: CreateTaskRequest): Promise<TaskResponse> {
    const response = await postApiTasks({
        body: data,
    })

    if (response.error) {
        throw new Error(getErrorMessage(response.error))
    }

    if (!response.data) {
        throw new Error('Resposta vazia do servidor')
    }

    return response.data as TaskResponse
}

/**
 * Atualiza uma task existente
 */
export async function updateTask(id: string, data: UpdateTaskRequest): Promise<void> {
    const response = await putApiTasksById({
        path: { id },
        body: data,
    })

    if (response.error) {
        throw new Error(getErrorMessage(response.error))
    }
}

/**
 * Move uma task para outro status (atualiza apenas o status)
 */
export async function moveTask(id: string, newStatus: TaskStatus, currentTask: TaskResponse): Promise<void> {
    const updateData: UpdateTaskRequest = {
        title: currentTask.title || '',
        description: currentTask.description || '',
        dueDate: currentTask.dueDate || new Date().toISOString(),
        status: newStatus,
    }

    return updateTask(id, updateData)
}

/**
 * Deleta uma task
 */
export async function deleteTask(id: string): Promise<void> {
    const response = await deleteApiTasksById({
        path: { id },
    })

    if (response.error) {
        throw new Error(getErrorMessage(response.error))
    }
}

