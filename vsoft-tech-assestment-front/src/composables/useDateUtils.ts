import { DateTime } from 'luxon'

/**
 * Composable para manipulação de datas usando Luxon
 * Garante consistência na manipulação de datas em todo o aplicativo
 */
export function useDateUtils() {
    /**
     * Extrai apenas a parte de data (YYYY-MM-DD) da string, ignorando hora e timezone
     * Útil para evitar problemas de timezone ao comparar datas
     */
    const extractDateOnly = (dateString: string): string => {
        return dateString.split('T')[0] || dateString
    }

    /**
     * Normaliza uma data para comparação (apenas ano, mês, dia - ignorando hora e timezone)
     * Retorna um DateTime no início do dia no timezone local
     */
    const normalizeDate = (dateString: string): DateTime => {
        const dateOnly = extractDateOnly(dateString)
        const date = DateTime.fromISO(dateOnly, { zone: 'local' })

        if (!date.isValid) {
            return DateTime.invalid('Invalid date')
        }

        return date.startOf('day')
    }

    /**
     * Formata uma data para exibição no formato brasileiro (DD/MM/YYYY)
     */
    const formatDate = (dateString: string): string => {
        if (!dateString) return ''
        const dateOnly = extractDateOnly(dateString)
        const date = DateTime.fromISO(dateOnly, { zone: 'local' })
        if (!date.isValid) return ''
        return date.toLocaleString({
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
        })
    }

    /**
     * Verifica se uma data está vencida (anterior à data de hoje)
     * Compara apenas a parte de data (YYYY-MM-DD) para evitar problemas de timezone
     */
    const isOverdue = (dateString: string): boolean => {
        if (!dateString) return false
        const taskDate = normalizeDate(dateString)
        const today = DateTime.now().startOf('day')

        if (!taskDate.isValid) return false
        return taskDate < today
    }

    return {
        extractDateOnly,
        normalizeDate,
        formatDate,
        isOverdue,
    }
}

