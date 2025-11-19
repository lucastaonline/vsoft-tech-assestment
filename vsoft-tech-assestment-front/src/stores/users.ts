import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as usersService from '@/services/usersService'

export interface UserListItemResponse {
    id: string
    userName: string
    email: string
}

export const useUsersStore = defineStore('users', () => {
    const users = ref<UserListItemResponse[]>([])
    const loading = ref(false)

    // Carregar usuários
    const fetchUsers = async () => {
        if (users.value.length > 0) return // Já carregados

        loading.value = true

        try {
            users.value = await usersService.listUsers()
        } catch (error) {
            console.error('Erro ao carregar usuários:', error)
            throw error
        } finally {
            loading.value = false
        }
    }

    return {
        users,
        loading,
        fetchUsers,
    }
})

