import { ref, onUnmounted } from 'vue'
import * as SignalR from '@microsoft/signalr'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'

export function useSignalR() {
    const connection = ref<SignalR.HubConnection | null>(null)
    const isConnected = ref(false)

    /**
     * Conecta ao SignalR Hub
     * O cookie HttpOnly será enviado automaticamente pelo navegador
     */
    const connect = async () => {
        if (connection.value?.state === SignalR.HubConnectionState.Connected) {
            return
        }

        try {
            const hubUrl = `${API_BASE_URL}/notificationHub`

            connection.value = new SignalR.HubConnectionBuilder()
                .withUrl(hubUrl, {
                    // withCredentials garante que cookies HttpOnly sejam enviados
                    withCredentials: true,
                })
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: (retryContext) => {
                        if (retryContext.previousRetryCount < 10) {
                            return 1000 * (retryContext.previousRetryCount + 1)
                        }
                        return 30000 // Max 30 seconds
                    }
                })
                .build()

            // Event handlers
            connection.value.onclose(() => {
                isConnected.value = false
                console.log('SignalR connection closed')
            })

            connection.value.onreconnecting(() => {
                isConnected.value = false
                console.log('SignalR reconnecting...')
            })

            connection.value.onreconnected(() => {
                isConnected.value = true
                console.log('SignalR reconnected')
            })

            // Iniciar conexão
            await connection.value.start()
            isConnected.value = true
            console.log('SignalR connected')
        } catch (error) {
            console.error('Error connecting to SignalR:', error)
            isConnected.value = false
        }
    }

    /**
     * Desconecta do SignalR Hub
     */
    const disconnect = async () => {
        if (connection.value) {
            try {
                await connection.value.stop()
                isConnected.value = false
                console.log('SignalR disconnected')
            } catch (error) {
                console.error('Error disconnecting from SignalR:', error)
            } finally {
                connection.value = null
            }
        }
    }

    /**
     * Registra um handler para receber notificações
     */
    const onNotification = (callback: (notification: any) => void) => {
        if (connection.value) {
            connection.value.on('ReceiveNotification', callback)
        }
    }

    /**
     * Remove um handler de notificações
     */
    const offNotification = (callback: (notification: any) => void) => {
        if (connection.value) {
            connection.value.off('ReceiveNotification', callback)
        }
    }

    // Limpar conexão ao desmontar
    onUnmounted(() => {
        disconnect()
    })

    return {
        connection,
        isConnected,
        connect,
        disconnect,
        onNotification,
        offNotification,
    }
}

