import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

export function requireAuth(
    to: RouteLocationNormalized,
    from: RouteLocationNormalized,
    next: NavigationGuardNext
) {
    const authStore = useAuthStore()
    authStore.checkAuth()

    if (!authStore.isAuthenticated) {
        next({ name: 'login', query: { redirect: to.fullPath } })
    } else {
        next()
    }
}

export function requireGuest(
    to: RouteLocationNormalized,
    from: RouteLocationNormalized,
    next: NavigationGuardNext
) {
    const authStore = useAuthStore()
    authStore.checkAuth()

    if (authStore.isAuthenticated) {
        next({ name: 'home' })
    } else {
        next()
    }
}

