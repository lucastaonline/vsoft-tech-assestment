import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '@/stores/auth'
import * as authService from '@/services/authService'
import type { LoginResponse, RegisterResponse } from '@/lib/api/types.gen'

vi.mock('@/services/authService', () => ({
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    refreshToken: vi.fn(),
}))

describe('useAuthStore', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        localStorage.clear()
        vi.clearAllMocks()
    })

    it('realiza login com sucesso e persiste usuário', async () => {
        const store = useAuthStore()
        const loginResponse: LoginResponse = {
            success: true,
            message: 'Login realizado com sucesso',
            token: 'token',
            userId: 'user-1',
            userName: 'john',
            email: 'john@example.com',
        }

        vi.mocked(authService.login).mockResolvedValue(loginResponse)

        const result = await store.login({ userNameOrEmail: 'john', password: '123' })

        expect(result.success).toBe(true)
        expect(store.user?.userName).toBe('john')
        expect(JSON.parse(localStorage.getItem('auth_user') || '{}').userName).toBe('john')
        expect(authService.login).toHaveBeenCalledWith({ userNameOrEmail: 'john', password: '123' })
    })

    it('retorna erro quando login falha', async () => {
        const store = useAuthStore()
        vi.mocked(authService.login).mockRejectedValue(new Error('Credenciais inválidas'))

        const result = await store.login({ userNameOrEmail: 'john', password: 'wrong' })

        expect(result.success).toBe(false)
        expect(result.error).toBe('Credenciais inválidas')
        expect(store.user).toBeNull()
    })

    it('registra usuário com sucesso', async () => {
        const store = useAuthStore()
        const registerResponse: RegisterResponse = {
            success: true,
            message: 'Usuário criado com sucesso',
            userId: 'user-1',
        }

        vi.mocked(authService.register).mockResolvedValue(registerResponse)

        const result = await store.register({
            email: 'john@example.com',
            userName: 'john',
            password: 'Password123',
        })

        expect(result.success).toBe(true)
        expect(result.message).toContain('sucesso')
        expect(authService.register).toHaveBeenCalled()
    })

    it('retorna erro quando registro falha', async () => {
        const store = useAuthStore()
        vi.mocked(authService.register).mockRejectedValue(new Error('Email já usado'))

        const result = await store.register({
            email: 'john@example.com',
            userName: 'john',
            password: 'Password123',
        })

        expect(result.success).toBe(false)
        expect(result.error).toBe('Email já usado')
    })
})

