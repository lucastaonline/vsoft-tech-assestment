/**
 * Configuração do cliente API para enviar cookies em todas as requisições.
 * Este arquivo não será sobrescrito pela geração automática.
 * 
 * Baseado na documentação oficial: https://heyapi.dev/openapi-ts/clients/fetch
 */

import { client } from '@/lib/api/client.gen'
import { setupApiInterceptors } from './interceptors'

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080').replace(/\/$/, '')

// Configurar fetch customizado que sempre inclui credentials: 'include'
const fetchWithCredentials: typeof fetch = async (input, init) => {
    return fetch(input, {
        ...init,
        credentials: 'include',
    })
}

// Aplicar configuração no cliente
client.setConfig({
    baseUrl: API_BASE_URL,
    fetch: fetchWithCredentials,
})

// Configurar interceptors (tratamento de sessão expirada, etc.)
setupApiInterceptors()

