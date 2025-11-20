/**
 * Configuração do cliente API para enviar cookies em todas as requisições.
 * Este arquivo não será sobrescrito pela geração automática.
 * 
 * Baseado na documentação oficial: https://heyapi.dev/openapi-ts/clients/fetch
 */

import { client } from '@/lib/api/client.gen'
import { setupApiInterceptors } from './interceptors'

// Configurar fetch customizado que sempre inclui credentials: 'include'
const fetchWithCredentials: typeof fetch = async (input, init) => {
    return fetch(input, {
        ...init,
        credentials: 'include',
    })
}

// Aplicar configuração no cliente
client.setConfig({
    fetch: fetchWithCredentials,
})

// Configurar interceptors (tratamento de sessão expirada, etc.)
setupApiInterceptors()

