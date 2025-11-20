import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as notificationsService from '@/services/notificationsService'
import type { NotificationResponse } from '@/services/notificationsService'

export const useNotificationsStore = defineStore('notifications', () => {
    const notifications = ref<NotificationResponse[]>([])
    const loading = ref(false)
    const unreadCount = ref(0)

    // Notificações não lidas
    const unreadNotifications = computed(() => {
        return notifications.value.filter(n => !n.isRead)
    })

    // Notificações ordenadas (não lidas primeiro)
    const sortedNotifications = computed(() => {
        return [...notifications.value].sort((a, b) => {
            // Não lidas primeiro
            if (a.isRead !== b.isRead) {
                return a.isRead ? 1 : -1
            }
            // Depois por data (mais recentes primeiro)
            return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        })
    })

    /**
     * Busca todas as notificações
     */
    const fetchNotifications = async () => {
        loading.value = true
        try {
            notifications.value = await notificationsService.getNotifications()
            unreadCount.value = notifications.value.filter(n => !n.isRead).length
        } catch (error) {
            console.error('Erro ao buscar notificações:', error)
            throw error
        } finally {
            loading.value = false
        }
    }

    /**
     * Busca apenas a contagem de não lidas (mais leve)
     */
    const fetchUnreadCount = async () => {
        try {
            unreadCount.value = await notificationsService.getUnreadCount()
        } catch (error) {
            console.error('Erro ao buscar contagem de notificações:', error)
        }
    }

    /**
     * Marca uma notificação como lida
     */
    const markAsRead = async (notificationId: string) => {
        try {
            const updated = await notificationsService.markAsRead(notificationId)

            // Atualizar na lista local
            const index = notifications.value.findIndex(n => n.id === notificationId)
            if (index !== -1) {
                notifications.value[index] = updated
                unreadCount.value = Math.max(0, unreadCount.value - 1)
            }
        } catch (error) {
            console.error('Erro ao marcar notificação como lida:', error)
            throw error
        }
    }

    /**
     * Marca todas as notificações como lidas
     */
    const markAllAsRead = async () => {
        try {
            await notificationsService.markAllAsRead()

            // Atualizar todas localmente
            notifications.value = notifications.value.map(n => ({
                ...n,
                isRead: true,
                readAt: new Date().toISOString(),
            }))
            unreadCount.value = 0
        } catch (error) {
            console.error('Erro ao marcar todas as notificações como lidas:', error)
            throw error
        }
    }

    /**
     * Adiciona uma nova notificação (para uso em tempo real)
     */
    const addNotification = (notification: NotificationResponse) => {
        // Verificar se já existe (evitar duplicatas)
        const exists = notifications.value.some(n => n.id === notification.id)
        if (!exists) {
            notifications.value.unshift(notification)
            if (!notification.isRead) {
                unreadCount.value++
            }
        }
    }

    return {
        notifications,
        loading,
        unreadCount,
        unreadNotifications,
        sortedNotifications,
        fetchNotifications,
        fetchUnreadCount,
        markAsRead,
        markAllAsRead,
        addNotification,
    }
})

