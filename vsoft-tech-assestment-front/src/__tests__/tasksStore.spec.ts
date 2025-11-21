import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useTasksStore } from '@/stores/tasks'
import * as tasksService from '@/services/tasksService'
import type {
    TaskResponse,
    CreateTaskRequest,
    UpdateTaskRequest,
    TaskStatus,
} from '@/lib/api/types.gen'

vi.mock('@/services/tasksService', () => ({
    listTasks: vi.fn(),
    createTask: vi.fn(),
    updateTask: vi.fn(),
    moveTask: vi.fn(),
    deleteTask: vi.fn(),
    getTaskById: vi.fn(),
}))

const baseTask: TaskResponse = {
    id: 'task-1',
    title: 'Task 1',
    description: 'Desc',
    dueDate: new Date().toISOString(),
    status: 0,
    userId: 'user-1',
    createdAt: new Date().toISOString(),
}

describe('useTasksStore', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it('adiciona tarefa criada com sucesso', async () => {
        const store = useTasksStore()
        const createRequest: CreateTaskRequest = {
            title: 'Task 1',
            description: 'Desc',
            dueDate: baseTask.dueDate!,
            status: 0 as TaskStatus,
            userId: 'user-1',
        }

        vi.mocked(tasksService.createTask).mockResolvedValue(baseTask)

        const result = await store.createTask(createRequest)

        expect(result?.id).toBe('task-1')
        expect(store.tasks).toHaveLength(1)
        expect(tasksService.createTask).toHaveBeenCalledWith(createRequest)
    })

    it('atualiza tarefa e reflete no estado', async () => {
        const store = useTasksStore()
        store.tasks = [{ ...baseTask }]

        const updateRequest: UpdateTaskRequest = {
            title: 'Task Atualizada',
            description: 'Nova descrição',
            dueDate: new Date().toISOString(),
            status: 1 as TaskStatus,
            userId: 'user-1',
        }

        const updatedTask: TaskResponse = {
            ...baseTask,
            ...updateRequest,
        }

        vi.mocked(tasksService.updateTask).mockResolvedValue(updatedTask)

        const result = await store.updateTask('task-1', updateRequest)

        expect(result.title).toBe('Task Atualizada')
        expect(store.tasks[0]!.status).toBe(1)
        expect(tasksService.updateTask).toHaveBeenCalledWith('task-1', updateRequest)
    })

    it('move tarefa para nova coluna mantendo estado consistente', async () => {
        const store = useTasksStore()
        store.tasks = [{ ...baseTask }]

        const movedTask: TaskResponse = {
            ...baseTask,
            status: 2,
        }

        vi.mocked(tasksService.moveTask).mockResolvedValue(movedTask)

        const result = await store.moveTask('task-1', 2)

        expect(result.status).toBe(2)
        expect(store.tasks[0]!.status).toBe(2)
        expect(tasksService.moveTask).toHaveBeenCalledWith(
            'task-1',
            2,
            expect.objectContaining({ id: 'task-1' })
        )
    })

    it('carrega todas as tasks com fetchTasks', async () => {
        const store = useTasksStore()
        vi.mocked(tasksService.listTasks).mockResolvedValue([{ ...baseTask }])

        await store.fetchTasks()

        expect(tasksService.listTasks).toHaveBeenCalledTimes(1)
        expect(store.tasks).toHaveLength(1)
        expect(store.tasksByStatus[0]).toHaveLength(1)
    })
})

