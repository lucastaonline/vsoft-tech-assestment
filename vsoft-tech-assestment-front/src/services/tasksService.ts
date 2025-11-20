import {
    getApiTasks,
    postApiTasks,
    putApiTasksById,
    deleteApiTasksById
} from '@/lib/api/sdk.gen'
import { client } from '@/lib/api/client.gen'
import type {
    TaskResponse,
    TaskStatus,
    CreateTaskRequest,
    UpdateTaskRequest,
    ProblemDetails
} from '@/lib/api/types.gen'

// Classe de erro customizada com status HTTP
export class ApiError extends Error {
    constructor(
        message: string,
        public readonly status: number | undefined,
        public readonly originalError?: unknown
    ) {
        super(message)
        this.name = 'ApiError'
    }

    get isForbidden(): boolean {
        return this.status === 403
    }

    get isNotFound(): boolean {
        return this.status === 404
    }

    get isUnauthorized(): boolean {
        return this.status === 401
    }
}

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
        const problemDetails = error as ProblemDetails & { error?: string }
        // Verificar campo 'error' primeiro (usado pelo backend para 403)
        if (problemDetails.error) {
            return problemDetails.error
        }
        // Depois verificar campos padrão do ProblemDetails
        return problemDetails.detail || problemDetails.title || 'Erro desconhecido'
    }
    return 'Erro desconhecido'
}

// Função auxiliar para criar ApiError a partir de response.error
function createApiError(error: unknown, status: number | undefined): ApiError {
    const message = getErrorMessage(error)
    return new ApiError(message, status, error)
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
        throw createApiError(response.error, response.response?.status)
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
        throw createApiError(response.error, response.response?.status)
    }

    if (!response.data) {
        throw new Error('Resposta vazia do servidor')
    }

    return response.data as TaskResponse
}

/**
 * Atualiza uma task existente
 * Nota: Usando client.request diretamente porque o SDK ainda está com 204
 * TODO: Regenerar SDK após backend retornar 200 OK com TaskResponse
 */
export async function updateTask(id: string, data: UpdateTaskRequest): Promise<TaskResponse> {
    // Usar client.request diretamente para obter a resposta completa
    const response = await client.request<TaskResponse>({
        url: `/api/Tasks/${id}`,
        method: 'PUT',
        body: data,
    })

    if (response.error) {
        throw createApiError(response.error, response.response?.status)
    }

    if (!response.data) {
        throw new Error('Resposta vazia do servidor')
    }

    return response.data as TaskResponse
}

/**
 * Move uma task para outro status (atualiza apenas o status)
 */
export async function moveTask(id: string, newStatus: TaskStatus, currentTask: TaskResponse): Promise<TaskResponse> {
    const updateData: UpdateTaskRequest = {
        title: currentTask.title || '',
        description: currentTask.description || '',
        dueDate: currentTask.dueDate || new Date().toISOString(),
        status: newStatus,
        userId: currentTask.userId || '',
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
        throw createApiError(response.error, response.response?.status)
    }
}

