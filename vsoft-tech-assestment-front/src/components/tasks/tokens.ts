import type { Ref } from 'vue'

export const taskColumnRootKey = Symbol('task-column-root')

export type TaskColumnRootRef = Ref<HTMLElement | null>

