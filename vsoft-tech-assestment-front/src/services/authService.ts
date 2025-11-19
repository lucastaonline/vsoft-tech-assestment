import { postApiAuthLogin, postApiAuthRegister } from '@/lib/api/sdk.gen'
import { client } from '@/lib/api/client.gen'
import type { LoginRequest, RegisterRequest, LoginResponse, RegisterResponse } from '@/lib/api/types.gen'

/**
 * Realiza login e retorna dados do usuário
 */
export async function login(credentials: LoginRequest): Promise<LoginResponse> {
    const apiResponse = await postApiAuthLogin({
        body: credentials,
    })

    // Tratar resposta do cliente gerado
    if (apiResponse.error) {
        const error = apiResponse.error as LoginResponse
        throw new Error(error.message || 'Erro ao fazer login')
    }

    if (!apiResponse.data) {
        throw new Error('Resposta vazia do servidor')
    }

    return apiResponse.data as LoginResponse
}

/**
 * Registra um novo usuário
 */
export async function register(data: RegisterRequest): Promise<RegisterResponse> {
    const apiResponse = await postApiAuthRegister({
        body: data,
    })

    // Tratar resposta do cliente gerado
    if (apiResponse.error) {
        const error = apiResponse.error as RegisterResponse
        const errorMessage = error.errors?.join(', ') || error.message || 'Erro ao registrar'
        throw new Error(errorMessage)
    }

    if (!apiResponse.data) {
        throw new Error('Resposta vazia do servidor')
    }

    return apiResponse.data as RegisterResponse
}

/**
 * Realiza logout
 */
export async function logout(): Promise<void> {
    await client.request({
        url: '/api/Auth/logout',
        method: 'POST',
    })
}

