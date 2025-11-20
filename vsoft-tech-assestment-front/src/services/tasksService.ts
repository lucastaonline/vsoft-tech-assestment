import {
    getApiTasks,
    getApiTasksById,
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
        public readonly originalError?: unknown,
        public readonly validationErrors?: Record<string, string[]>
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

    get isValidationError(): boolean {
        return this.status === 400 && !!this.validationErrors && Object.keys(this.validationErrors).length > 0
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
        const problemDetails = error as ProblemDetails & { error?: string; errors?: Record<string, string[]> }
        // Verificar campo 'error' primeiro (usado pelo backend para 403)
        if (problemDetails.error) {
            return problemDetails.error
        }
        // Se houver erros de validação, criar mensagem resumida
        if (problemDetails.errors && Object.keys(problemDetails.errors).length > 0) {
            const errorCount = Object.keys(problemDetails.errors).length
            return `Erro de validação em ${errorCount} campo(s)`
        }
        // Depois verificar campos padrão do ProblemDetails
        return problemDetails.detail || problemDetails.title || 'Erro desconhecido'
    }
    return 'Erro desconhecido'
}

// Função auxiliar para extrair erros de validação
function getValidationErrors(error: unknown): Record<string, string[]> | undefined {
    if (!error || typeof error !== 'object') {
        return undefined
    }

    const errorObj = error as Record<string, unknown>
    const validationErrors: Record<string, string[]> = {}
    let hasErrors = false

    // ASP.NET Core retorna ModelState como BadRequest(ModelState)
    // O formato pode ser:
    // 1. { "errors": { "FieldName": ["error1", "error2"] } } (ProblemDetails com errors)
    // 2. { "FieldName": ["error1", "error2"] } (ModelState direto)
    // 3. { "FieldName": { "Errors": [{ "ErrorMessage": "error1" }] } } (ModelStateEntry)

    // Primeiro, verificar se há campo 'errors' (formato ProblemDetails)
    if ('errors' in errorObj && errorObj.errors && typeof errorObj.errors === 'object') {
        const errorsField = errorObj.errors as Record<string, unknown>
        for (const [key, value] of Object.entries(errorsField)) {
            const messages = extractErrorMessages(value)
            if (messages.length > 0) {
                validationErrors[key] = messages
                hasErrors = true
            }
        }
    }

    // Se não encontrou no campo 'errors', verificar o objeto diretamente
    if (!hasErrors) {
        for (const [key, value] of Object.entries(errorObj)) {
            // Ignorar campos padrão do ProblemDetails
            if (['type', 'title', 'status', 'detail', 'instance', 'errors'].includes(key)) {
                continue
            }

            const messages = extractErrorMessages(value)
            if (messages.length > 0) {
                validationErrors[key] = messages
                hasErrors = true
            }
        }
    }

    return hasErrors ? validationErrors : undefined
}

// Função auxiliar para extrair mensagens de erro de diferentes formatos
function extractErrorMessages(value: unknown): string[] {
    if (Array.isArray(value)) {
        // Formato: ["error1", "error2"]
        return value
            .map(err => {
                if (typeof err === 'string') {
                    return err
                }
                if (err && typeof err === 'object') {
                    // Formato: { ErrorMessage: "..." }
                    return (err as any)?.ErrorMessage || (err as any)?.errorMessage || String(err)
                }
                return String(err)
            })
            .filter(msg => msg && msg.trim())
    }

    if (typeof value === 'string' && value.trim()) {
        // Formato: "error"
        return [value]
    }

    if (value && typeof value === 'object') {
        // Formato: { Errors: [{ ErrorMessage: "..." }] } (ModelStateEntry)
        const obj = value as Record<string, unknown>

        // Verificar campo Errors (array)
        if (Array.isArray(obj.Errors)) {
            return obj.Errors
                .map((err: unknown) => {
                    if (typeof err === 'string') {
                        return err
                    }
                    if (err && typeof err === 'object') {
                        return (err as any)?.ErrorMessage || (err as any)?.errorMessage || String(err)
                    }
                    return String(err)
                })
                .filter(msg => msg && msg.trim())
        }

        // Verificar se o próprio objeto tem ErrorMessage
        if ((obj as any)?.ErrorMessage) {
            return [(obj as any).ErrorMessage]
        }
    }

    return []
}

// Função auxiliar para criar ApiError a partir de response.error
function createApiError(error: unknown, status: number | undefined): ApiError {
    const message = getErrorMessage(error)
    const validationErrors = getValidationErrors(error)

    // Log para debug (remover em produção se necessário)
    if (status === 400 && validationErrors) {
        console.log('Erros de validação detectados:', validationErrors)
    } else if (status === 400) {
        console.log('Erro 400 sem erros de validação detectados. Erro completo:', error)
    }

    return new ApiError(message, status, error, validationErrors)
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
 * Busca uma task por ID
 */
export async function getTaskById(id: string): Promise<TaskResponse> {
    const response = await getApiTasksById({
        path: {
            id,
        },
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

