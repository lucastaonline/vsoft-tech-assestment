const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'

export interface NotificationResponse {
    id: string
    message: string
    isRead: boolean
    createdAt: string
    readAt: string | null
    taskId?: string | null
}

/**
 * Busca todas as notificações do usuário autenticado
 */
export async function getNotifications(): Promise<NotificationResponse[]> {
    try {
        const response = await fetch(`${API_BASE_URL}/api/notifications`, {
            method: 'GET',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
            },
        })

        if (!response.ok) {
            throw new Error(`Erro ao buscar notificações: ${response.statusText}`)
        }

        return await response.json() as NotificationResponse[]
    } catch (error) {
        console.error('Erro ao buscar notificações:', error)
        throw error
    }
}

/**
 * Busca a quantidade de notificações não lidas
 */
export async function getUnreadCount(): Promise<number> {
    try {
        const response = await fetch(`${API_BASE_URL}/api/notifications/unread-count`, {
            method: 'GET',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
            },
        })

        if (!response.ok) {
            throw new Error(`Erro ao buscar contagem: ${response.statusText}`)
        }

        return await response.json() as number
    } catch (error) {
        console.error('Erro ao buscar contagem de notificações:', error)
        throw error
    }
}

/**
 * Marca uma notificação como lida
 */
export async function markAsRead(notificationId: string): Promise<NotificationResponse> {
    try {
        const response = await fetch(`${API_BASE_URL}/api/notifications/${notificationId}/read`, {
            method: 'PUT',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
            },
        })

        if (!response.ok) {
            throw new Error(`Erro ao marcar como lida: ${response.statusText}`)
        }

        return await response.json() as NotificationResponse
    } catch (error) {
        console.error('Erro ao marcar notificação como lida:', error)
        throw error
    }
}

/**
 * Marca todas as notificações como lidas
 */
export async function markAllAsRead(): Promise<void> {
    try {
        const response = await fetch(`${API_BASE_URL}/api/notifications/read-all`, {
            method: 'PUT',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
            },
        })

        if (!response.ok) {
            throw new Error(`Erro ao marcar todas como lidas: ${response.statusText}`)
        }
    } catch (error) {
        console.error('Erro ao marcar todas as notificações como lidas:', error)
        throw error
    }
}

