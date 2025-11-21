import { client } from '@/lib/api/client.gen'

export type CalendarScope = 'user' | 'all'

export interface CalendarLinkResponse {
  token: string
  url: string
  issuedAt: string
  scope: CalendarScope | string
}

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080').replace(/\/$/, '')

export async function generateCalendarLink(scope: CalendarScope = 'user'): Promise<CalendarLinkResponse> {
  const response = await client.request<CalendarLinkResponse>({
    url: '/api/Tasks/calendar/token',
    method: 'GET',
    query: { scope },
  })

  if (response.error) {
    throw new Error('Não foi possível gerar o link do calendário.')
  }

  if (!response.data) {
    throw new Error('Resposta vazia do servidor')
  }

  return response.data
}

export async function downloadCalendarFile(token: string, signal?: AbortSignal): Promise<Blob> {
  const url = `${API_BASE_URL}/api/Tasks/calendar.ics?token=${encodeURIComponent(token)}`
  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include',
    signal,
  })

  if (!response.ok) {
    throw new Error('Não foi possível baixar o arquivo iCalendar.')
  }

  return await response.blob()
}


