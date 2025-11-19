import { client } from '@/lib/api/client.gen'
import type { UserListItemResponse } from '@/stores/users'

/**
 * Lista todos os usuários
 */
export async function listUsers(): Promise<UserListItemResponse[]> {
    const response = await client.request({
        url: '/api/Users',
        method: 'GET',
    })

    if (!response.response.ok) {
        const errorMsg = typeof response.error === 'string'
            ? response.error
            : `HTTP ${response.response.status}`
        throw new Error(errorMsg)
    }

    if (!response.data) {
        throw new Error('Resposta vazia do servidor')
    }

    return response.data as UserListItemResponse[]
}

