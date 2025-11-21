<script setup lang="ts">
import { inject, nextTick, onUnmounted, ref, watch } from 'vue'
import TaskCard from './TaskCard.vue'
import type { TaskResponse, TaskStatus } from '@/lib/api/types.gen'
import { useIntersection } from '@/composables/useIntersection'
import { taskColumnRootKey } from './tokens'

const props = defineProps<{
    task: TaskResponse
    status: TaskStatus
    estimatedHeight?: number
}>()

const emit = defineEmits<{
    'task-click': [task: TaskResponse]
    'task-edit': [task: TaskResponse]
    'task-delete': [taskId: string]
    'task-move': [taskId: string, newStatus: TaskStatus, oldStatus: TaskStatus]
}>()

const columnRoot = inject(taskColumnRootKey, null)

const measuredHeight = ref(props.estimatedHeight ?? 180)
const shouldRender = ref(false)
const cardRef = ref<HTMLElement | null>(null)
const { target: intersectionTarget, isVisible } = useIntersection({
    once: false,
    rootRef: columnRoot ?? undefined,
    rootMargin: '0px 0px 200px 0px',
    threshold: 0,
})

let resizeObserver: ResizeObserver | null = null

const stopObserving = () => {
    if (resizeObserver && cardRef.value) {
        resizeObserver.unobserve(cardRef.value)
    }
}

const startObserving = () => {
    if (!cardRef.value) return
    if (!resizeObserver) {
        resizeObserver = new ResizeObserver(entries => {
            const entry = entries[0]
            if (entry) {
                measuredHeight.value = entry.contentRect.height || measuredHeight.value
            }
        })
    }
    resizeObserver.observe(cardRef.value)
}

watch(
    () => isVisible.value,
    visible => {
        shouldRender.value = visible
    },
    { immediate: true }
)

watch(
    () => shouldRender.value,
    visible => {
        if (visible) {
            nextTick().then(() => {
                startObserving()
            })
        } else {
            stopObserving()
        }
    }
)

onUnmounted(() => {
    resizeObserver?.disconnect()
})

const handleTaskClick = () => emit('task-click', props.task)
</script>

<template>
    <div ref="intersectionTarget" :data-task-id="task.id || `temp-${task.title}`" :data-task-status="task.status"
        class="virtual-task-wrapper">
        <div v-if="shouldRender" ref="cardRef" class="cursor-pointer" @click="handleTaskClick">
            <TaskCard :task="task" :current-status="status" @edit="emit('task-edit', $event)"
                @delete="emit('task-delete', $event)"
                @move="(taskId, newStatus) => emit('task-move', taskId, newStatus, status)" />
        </div>
        <div v-else :style="{ height: `${measuredHeight}px` }"
            class="rounded-lg border border-dashed border-border/40 bg-transparent opacity-0" aria-hidden="true" />
    </div>
</template>

<style scoped>
.virtual-task-wrapper {
    width: 100%;
}
</style>
