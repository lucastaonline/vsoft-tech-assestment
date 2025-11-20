<script setup lang="ts">
import { computed, onMounted, onUnmounted, watch } from 'vue'
import { Bell, Check, CheckCheck } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { useNotificationsStore } from '@/stores/notifications'
import { useAuthStore } from '@/stores/auth'
import { useDateUtils } from '@/composables/useDateUtils'
import { useSignalR } from '@/composables/useSignalR'
import type { NotificationResponse } from '@/services/notificationsService'
import { toast } from 'vue-sonner'

const notificationsStore = useNotificationsStore()
const authStore = useAuthStore()
const { formatDate } = useDateUtils()
const signalR = useSignalR()

// Formatar data relativa (ex: "há 5 minutos")
const formatRelativeTime = (dateString: string): string => {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Agora'
  if (diffMins < 60) return `há ${diffMins} minuto${diffMins > 1 ? 's' : ''}`
  if (diffHours < 24) return `há ${diffHours} hora${diffHours > 1 ? 's' : ''}`
  if (diffDays < 7) return `há ${diffDays} dia${diffDays > 1 ? 's' : ''}`
  return formatDate(dateString)
}

// Carregar notificações ao montar
onMounted(async () => {
  try {
    await notificationsStore.fetchNotifications()
  } catch (error) {
    console.error('Erro ao carregar notificações:', error)
  }

  // Conectar ao SignalR se autenticado
  if (authStore.isAuthenticated) {
    await signalR.connect()
    
    // Registrar handler para receber notificações em tempo real
    signalR.onNotification((notification: NotificationResponse) => {
      notificationsStore.addNotification(notification)
      // Opcional: mostrar toast para novas notificações
      if (!notification.isRead) {
        toast.info(notification.message, {
          duration: 5000,
        })
      }
    })
  }
})

// Desconectar ao desmontar
onUnmounted(async () => {
  await signalR.disconnect()
})

// Reconectar quando autenticação mudar
watch(
  () => authStore.isAuthenticated,
  async (isAuthenticated) => {
    if (isAuthenticated) {
      await signalR.connect()
      signalR.onNotification((notification: NotificationResponse) => {
        notificationsStore.addNotification(notification)
        if (!notification.isRead) {
          toast.info(notification.message, {
            duration: 5000,
          })
        }
      })
    } else {
      await signalR.disconnect()
    }
  }
)

// Handler para marcar como lida
const handleMarkAsRead = async (notificationId: string) => {
  try {
    await notificationsStore.markAsRead(notificationId)
  } catch (error) {
    toast.error('Erro ao marcar notificação como lida')
    console.error(error)
  }
}

// Handler para marcar todas como lidas
const handleMarkAllAsRead = async () => {
  if (notificationsStore.unreadCount === 0) return
  
  try {
    await notificationsStore.markAllAsRead()
    toast.success('Todas as notificações foram marcadas como lidas')
  } catch (error) {
    toast.error('Erro ao marcar todas as notificações como lidas')
    console.error(error)
  }
}
</script>

<template>
  <DropdownMenu>
    <DropdownMenuTrigger as-child>
      <Button variant="ghost" size="icon" class="relative h-9 w-9">
        <Bell class="h-4 w-4" />
        <span
          v-if="notificationsStore.unreadCount > 0"
          class="absolute -top-1 -right-1 flex h-5 w-5 items-center justify-center rounded-full bg-destructive text-xs font-bold text-destructive-foreground"
        >
          {{ notificationsStore.unreadCount > 99 ? '99+' : notificationsStore.unreadCount }}
        </span>
        <span class="sr-only">Notificações</span>
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end" class="w-80">
      <div class="flex items-center justify-between p-2">
        <h3 class="text-sm font-semibold">Notificações</h3>
        <Button
          v-if="notificationsStore.unreadCount > 0"
          variant="ghost"
          size="sm"
          class="h-7 text-xs"
          @click="handleMarkAllAsRead"
        >
          <CheckCheck class="mr-1 h-3 w-3" />
          Marcar todas como lidas
        </Button>
      </div>
      
      <DropdownMenuSeparator />
      
      <div class="max-h-96 overflow-y-auto">
        <div
          v-if="notificationsStore.loading"
          class="p-4 text-center text-sm text-muted-foreground"
        >
          Carregando...
        </div>
        
        <div
          v-else-if="notificationsStore.notifications.length === 0"
          class="p-4 text-center text-sm text-muted-foreground"
        >
          Nenhuma notificação
        </div>
        
        <template v-else>
          <DropdownMenuItem
            v-for="notification in notificationsStore.sortedNotifications"
            :key="notification.id"
            :class="{
              'bg-accent': !notification.isRead,
              'cursor-pointer': true,
            }"
            @click="handleMarkAsRead(notification.id)"
          >
            <div class="flex w-full flex-col gap-1">
              <div class="flex items-start justify-between gap-2">
                <p
                  class="text-sm"
                  :class="{
                    'font-medium': !notification.isRead,
                  }"
                >
                  {{ notification.message }}
                </p>
                <Check
                  v-if="notification.isRead"
                  class="h-3 w-3 shrink-0 text-muted-foreground"
                />
                <div
                  v-else
                  class="h-2 w-2 shrink-0 rounded-full bg-primary"
                />
              </div>
              <p class="text-xs text-muted-foreground">
                {{ formatRelativeTime(notification.createdAt) }}
              </p>
            </div>
          </DropdownMenuItem>
        </template>
      </div>
    </DropdownMenuContent>
  </DropdownMenu>
</template>

