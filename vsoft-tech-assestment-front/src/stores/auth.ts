import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { createClient } from '@/lib/api/client'
import { postApiAuthLogin, postApiAuthRegister } from '@/lib/api/sdk.gen'
import type { LoginRequest, RegisterRequest, LoginResponse, RegisterResponse } from '@/lib/api/types.gen'

// URL da API - Docker expõe na porta 8080, desenvolvimento local na 5001
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'

// Criar cliente com configuração base
const apiClient = createClient({
    baseUrl: API_BASE_URL,
})

/**
 * Configurar token de autenticação no cliente
 */
function setAuthToken(token: string | null) {
    if (token) {
        apiClient.setConfig({
            headers: {
                Authorization: `Bearer ${token}`,
            } as Record<string, string>,
        })
    } else {
        // Remover token
        const config = apiClient.getConfig()
        const headers = { ...(config.headers as Record<string, string>) }
        delete headers.Authorization
        apiClient.setConfig({ headers })
    }
}

interface User {
    id: string
    userName: string
    email: string
}

export const useAuthStore = defineStore('auth', () => {
    const token = ref<string | null>(localStorage.getItem('auth_token'))
    const user = ref<User | null>(null)
    const loading = ref(false)
    const error = ref<string | null>(null)

    const isAuthenticated = computed(() => !!token.value && !!user.value)

    // Carregar usuário do token (se necessário)
    const loadUser = () => {
        const savedUser = localStorage.getItem('auth_user')
        if (savedUser) {
            try {
                user.value = JSON.parse(savedUser)
            } catch {
                user.value = null
            }
        }
    }

    // Inicializar ao criar a store
    loadUser()

    const loginUser = async (credentials: LoginRequest) => {
        loading.value = true
        error.value = null

        try {
            // Chamar API diretamente
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

            const response = apiResponse.data as LoginResponse

            if (response.success && response.token) {
                token.value = response.token
                localStorage.setItem('auth_token', response.token)

                // Configurar token no cliente API
                setAuthToken(response.token)

                // Salvar informações do usuário
                if (response.userId && response.userName && response.email) {
                    user.value = {
                        id: response.userId,
                        userName: response.userName,
                        email: response.email,
                    }
                    localStorage.setItem('auth_user', JSON.stringify(user.value))
                }

                if (response.refreshToken) {
                    localStorage.setItem('auth_refresh_token', response.refreshToken)
                }

                return { success: true }
            } else {
                error.value = response.message || 'Erro ao fazer login'
                return { success: false, error: error.value }
            }
        } catch (err) {
            const message = err instanceof Error ? err.message : 'Erro ao fazer login'
            error.value = message
            return { success: false, error: message }
        } finally {
            loading.value = false
        }
    }

    const registerUser = async (data: RegisterRequest) => {
        loading.value = true
        error.value = null

        try {
            // Chamar API diretamente
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

            const response = apiResponse.data as RegisterResponse

            if (response.success) {
                return { success: true, message: response.message || 'Registro realizado com sucesso' }
            } else {
                const errorMessage = response.errors?.join(', ') || response.message || 'Erro ao registrar'
                error.value = errorMessage
                return { success: false, error: errorMessage }
            }
        } catch (err) {
            const message = err instanceof Error ? err.message : 'Erro ao registrar'
            error.value = message
            return { success: false, error: message }
        } finally {
            loading.value = false
        }
    }

    const logout = () => {
        token.value = null
        user.value = null
        localStorage.removeItem('auth_token')
        localStorage.removeItem('auth_refresh_token')
        localStorage.removeItem('auth_user')
        error.value = null

        // Remover token do cliente API
        setAuthToken(null)
    }

    const checkAuth = () => {
        const savedToken = localStorage.getItem('auth_token')
        if (savedToken) {
            token.value = savedToken
            setAuthToken(savedToken) // Configurar token no cliente API
            loadUser()
        } else {
            logout()
        }
    }

    return {
        token,
        user,
        loading,
        error,
        isAuthenticated,
        login: loginUser,
        register: registerUser,
        logout,
        checkAuth,
    }
})

