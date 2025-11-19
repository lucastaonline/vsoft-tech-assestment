import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as authService from '@/services/authService'
import type { LoginRequest, RegisterRequest } from '@/lib/api/types.gen'

interface User {
    id: string
    userName: string
    email: string
}

export const useAuthStore = defineStore('auth', () => {
    // Com cookies HttpOnly, não armazenamos token no frontend
    // O token está seguro no cookie HttpOnly
    const user = ref<User | null>(null)
    const loading = ref(false)
    const error = ref<string | null>(null)

    // Verificar autenticação checando se há cookie (via API ou checando user)
    const isAuthenticated = computed(() => !!user.value)

    // Carregar usuário do localStorage (apenas dados do usuário, não token)
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
            const response = await authService.login(credentials)

            if (response.success) {
                // Token está no cookie HttpOnly (enviado pelo backend)
                // Não armazenamos token no frontend por segurança

                // Salvar informações do usuário
                if (response.userId && response.userName && response.email) {
                    user.value = {
                        id: response.userId,
                        userName: response.userName,
                        email: response.email,
                    }
                    localStorage.setItem('auth_user', JSON.stringify(user.value))
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
            const response = await authService.register(data)

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

    const logout = async () => {
        try {
            await authService.logout()
        } catch (error) {
            console.error('Erro ao fazer logout:', error)
        } finally {
            // Limpar estado local
            user.value = null
            localStorage.removeItem('auth_user')
            error.value = null
        }
    }

    const checkAuth = () => {
        // Com cookies HttpOnly, não podemos verificar diretamente
        // Carregamos apenas os dados do usuário salvos
        loadUser()
    }

    return {
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

