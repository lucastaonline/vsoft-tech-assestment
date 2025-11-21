import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import DashboardView from '@/views/DashboardView.vue'
import { useTasksStore } from '@/stores/tasks'

describe('DashboardView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('exibe métricas corretas com base nas tasks', async () => {
        const pinia = createPinia()
        setActivePinia(pinia)
        const tasksStore = useTasksStore()

        const now = new Date()
        const yesterday = new Date(now.getTime() - 24 * 60 * 60 * 1000)

        tasksStore.tasks = [
            {
                id: '1',
                title: 'Pendente',
                description: 'desc',
                dueDate: yesterday.toISOString(),
                status: 0,
                userId: 'u1',
                createdAt: now.toISOString(),
            },
            {
                id: '2',
                title: 'Em andamento',
                description: 'desc',
                dueDate: now.toISOString(),
                status: 1,
                userId: 'u1',
                createdAt: now.toISOString(),
            },
            {
                id: '3',
                title: 'Concluída',
                description: 'desc',
                dueDate: now.toISOString(),
                status: 2,
                userId: 'u1',
                createdAt: yesterday.toISOString(),
            },
        ]

        vi.spyOn(tasksStore, 'fetchTasks').mockResolvedValue()

        const wrapper = mount(DashboardView, {
            global: {
                plugins: [pinia],
            },
        })

        await flushPromises()

        expect(wrapper.get('[data-testid="metric-total"]').text()).toBe('3')
        expect(wrapper.get('[data-testid="metric-pending"]').text()).toBe('1')
        expect(wrapper.get('[data-testid="metric-in-progress"]').text()).toBe('1')
        expect(wrapper.get('[data-testid="metric-completed"]').text()).toBe('1')
        expect(wrapper.get('[data-testid="metric-overdue"]').text()).toBe('1')
        expect(wrapper.get('[data-testid="metric-created-today"]').text()).toBe('2')
        expect(wrapper.get('[data-testid="metric-completion-rate"]').text()).toBe('33%')
    })
})

