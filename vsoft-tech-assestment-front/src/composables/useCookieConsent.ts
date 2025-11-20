import { ref, computed } from 'vue'

const COOKIE_CONSENT_KEY = 'cookie_consent'

// Estado reativo compartilhado para garantir reatividade
const consentState = ref<string | null>(
    typeof window !== 'undefined' ? localStorage.getItem(COOKIE_CONSENT_KEY) : null
)

export function useCookieConsent() {
    const hasConsent = computed(() => {
        return consentState.value === 'accepted'
    })

    const acceptCookies = () => {
        localStorage.setItem(COOKIE_CONSENT_KEY, 'accepted')
        consentState.value = 'accepted'
    }

    const rejectCookies = () => {
        localStorage.setItem(COOKIE_CONSENT_KEY, 'rejected')
        consentState.value = 'rejected'
    }

    const clearConsent = () => {
        localStorage.removeItem(COOKIE_CONSENT_KEY)
        consentState.value = null
    }

    return {
        hasConsent,
        acceptCookies,
        rejectCookies,
        clearConsent,
    }
}

