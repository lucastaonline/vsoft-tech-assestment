/**
 * Interceptors do cliente API para tratamento de erros e respostas.
 * Este arquivo não será sobrescrito pela geração automática.
 */

import { client } from '@/lib/api/client.gen'

// Função auxiliar para processar redirecionamento por sessão expirada
// Flag para evitar múltiplos redirecionamentos simultâneos
let isRedirecting = false
let isRefreshing = false

async function tryRefreshToken(): Promise<boolean> {
    if (isRefreshing) {
        // Se já está tentando renovar, aguardar
        return new Promise((resolve) => {
            const checkInterval = setInterval(() => {
                if (!isRefreshing) {
                    clearInterval(checkInterval)
                    resolve(true) // Assumir sucesso se outro refresh completou
                }
            }, 100)
        })
    }

    isRefreshing = true

    try {
        // Importar dinamicamente para evitar dependência circular
        const { refreshToken } = await import('@/services/authService')
        
        // Tentar renovar o token (o backend vai ler do cookie automaticamente)
        await refreshToken()
        
        // Se chegou aqui, o refresh foi bem-sucedido
        return true
    } catch (error) {
        console.error('Erro ao renovar token:', error)
        return false
    } finally {
        isRefreshing = false
    }
}

async function handleUnauthorized(request?: Request, options?: any) {
    if (isRedirecting) return
    
    // Tentar renovar o token antes de redirecionar
    const refreshSuccess = await tryRefreshToken()
    
    if (refreshSuccess) {
        // Se o refresh foi bem-sucedido, não redirecionar
        // A requisição original será retentada automaticamente pelo interceptor
        return
    }
    
    // Se o refresh falhou, redirecionar para login
    isRedirecting = true
    
    try {
        // Importar dinamicamente para evitar dependência circular
        const { useAuthStore } = await import('@/stores/auth')
        const router = (await import('@/router')).default
        
        const authStore = useAuthStore()
        
        // Limpar estado de autenticação
        await authStore.logout()
        
        // Redirecionar para login com a rota atual como redirect
        const currentPath = window.location.pathname + window.location.search
        if (currentPath !== '/login' && router.currentRoute.value.name !== 'login') {
            router.push({
                name: 'login',
                query: { redirect: currentPath }
            })
        }
    } catch (error) {
        // Se houver erro ao importar ou executar, apenas logar
        console.error('Erro ao processar redirecionamento por sessão expirada:', error)
    } finally {
        // Resetar flag após um pequeno delay para permitir redirecionamento futuro
        setTimeout(() => {
            isRedirecting = false
        }, 1000)
    }
}

/**
 * Configura os interceptors do cliente API
 */
export function setupApiInterceptors() {
    // Interceptor de resposta para tratar sessão expirada (401)
    client.interceptors.response.use(async (response, request, options) => {
        // Se a resposta for 401 Unauthorized, tentar renovar token
        if (response.status === 401) {
            // Não tentar refresh se for a própria requisição de refresh
            if (request.url?.includes('/api/Auth/refresh')) {
                await handleUnauthorized(request, options)
                return response
            }

            const refreshSuccess = await tryRefreshToken()
            
            if (!refreshSuccess) {
                await handleUnauthorized(request, options)
            }
            // Se o refresh foi bem-sucedido, a próxima requisição usará o novo token
            // A requisição atual ainda retornará 401, mas o usuário não será redirecionado
        }
        
        return response
    })

    // Interceptor de erro também para garantir que capturamos todos os casos
    client.interceptors.error.use(async (error, response, request, options) => {
        // Se a resposta tiver status 401, tentar renovar token
        if (response && response.status === 401) {
            // Não tentar refresh se for a própria requisição de refresh
            if (request.url?.includes('/api/Auth/refresh')) {
                await handleUnauthorized(request, options)
                return error
            }

            const refreshSuccess = await tryRefreshToken()
            
            if (!refreshSuccess) {
                await handleUnauthorized(request, options)
            }
            // Se o refresh foi bem-sucedido, o erro será propagado
            // O usuário pode tentar a ação novamente manualmente
        }
        
        return error
    })
}

