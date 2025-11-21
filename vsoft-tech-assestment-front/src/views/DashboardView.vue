<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useTasksStore } from '@/stores/tasks'
import { useAuthStore } from '@/stores/auth'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import {
  CheckCircle2,
  Clock,
  AlertCircle,
  ListTodo,
  Calendar,
  TrendingUp,
  Users
} from 'lucide-vue-next'
import { useDateUtils } from '@/composables/useDateUtils'
import type { TaskResponse, TaskStatus } from '@/lib/api/types.gen'
import { DateTime } from 'luxon'

const tasksStore = useTasksStore()
const authStore = useAuthStore()
const { isOverdue, formatDate } = useDateUtils()

// Carregar tarefas ao montar
onMounted(async () => {
  if (tasksStore.tasks.length === 0) {
    await tasksStore.fetchTasks()
  }
})

// Métricas principais
const totalTasks = computed(() => tasksStore.tasks.length)
const pendingTasks = computed(() => tasksStore.tasksByStatus[0].length)
const inProgressTasks = computed(() => tasksStore.tasksByStatus[1].length)
const completedTasks = computed(() => tasksStore.tasksByStatus[2].length)

// Tarefas vencidas
const overdueTasks = computed(() => {
  return tasksStore.tasks.filter(task => {
    if (!task.dueDate) return false
    return isOverdue(task.dueDate) && task.status !== 2 // Não incluir concluídas
  })
})

// Tarefas criadas hoje
const tasksCreatedToday = computed(() => {
  const today = DateTime.now().startOf('day')
  return tasksStore.tasks.filter(task => {
    if (!task.createdAt) return false
    const createdDate = DateTime.fromISO(task.createdAt).startOf('day')
    return createdDate.equals(today)
  }).length
})

// Tarefas por usuário
const tasksByUser = computed(() => {
  const userMap = new Map<string, { userName: string; count: number }>()

  tasksStore.tasks.forEach(task => {
    const userId = task.userId || 'unknown'
    const userName = task.userName || 'Sem nome'

    if (userMap.has(userId)) {
      const user = userMap.get(userId)!
      user.count++
    } else {
      userMap.set(userId, { userName, count: 1 })
    }
  })

  return Array.from(userMap.values())
    .sort((a, b) => b.count - a.count)
    .slice(0, 5) // Top 5 usuários
})

// Tarefas recentes (últimas 5)
const recentTasks = computed(() => {
  return [...tasksStore.tasks]
    .sort((a, b) => {
      const dateA = a.createdAt ? new Date(a.createdAt).getTime() : 0
      const dateB = b.createdAt ? new Date(b.createdAt).getTime() : 0
      return dateB - dateA
    })
    .slice(0, 5)
})

// Taxa de conclusão
const completionRate = computed(() => {
  if (totalTasks.value === 0) return 0
  return Math.round((completedTasks.value / totalTasks.value) * 100)
})

// Status names
const statusNames: Record<TaskStatus, string> = {
  0: 'Pendente',
  1: 'Em Progresso',
  2: 'Concluída',
}
</script>

<template>
  <div class="space-y-8">
    <!-- Header -->
    <div>
      <h1 class="text-4xl font-bold tracking-tight">
        Dashboard
      </h1>
      <p class="text-muted-foreground mt-2">
        Visão geral das tarefas e métricas do sistema
      </p>
    </div>

    <!-- Cards de Métricas Principais -->
    <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
      <Card>
        <CardHeader class="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle class="text-sm font-medium">
            Total de Tarefas
          </CardTitle>
          <ListTodo class="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div class="text-2xl font-bold" data-testid="metric-total">{{ totalTasks }}</div>
          <p class="text-xs text-muted-foreground mt-1">
            Todas as tarefas do sistema
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader class="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle class="text-sm font-medium">
            Pendentes
          </CardTitle>
          <Clock class="h-4 w-4 text-yellow-600" />
        </CardHeader>
        <CardContent>
          <div class="text-2xl font-bold" data-testid="metric-pending">{{ pendingTasks }}</div>
          <p class="text-xs text-muted-foreground mt-1">
            Aguardando início
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader class="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle class="text-sm font-medium">
            Em Progresso
          </CardTitle>
          <TrendingUp class="h-4 w-4 text-blue-600" />
        </CardHeader>
        <CardContent>
          <div class="text-2xl font-bold" data-testid="metric-in-progress">{{ inProgressTasks }}</div>
          <p class="text-xs text-muted-foreground mt-1">
            Sendo executadas
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader class="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle class="text-sm font-medium">
            Concluídas
          </CardTitle>
          <CheckCircle2 class="h-4 w-4 text-green-600" />
        </CardHeader>
        <CardContent>
          <div class="text-2xl font-bold" data-testid="metric-completed">{{ completedTasks }}</div>
          <p class="text-xs text-muted-foreground mt-1">
            Taxa de conclusão: {{ completionRate }}%
          </p>
        </CardContent>
      </Card>
    </div>

    <!-- Segunda linha de métricas -->
    <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      <Card>
        <CardHeader class="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle class="text-sm font-medium">
            Tarefas Vencidas
          </CardTitle>
          <AlertCircle class="h-4 w-4 text-destructive" />
        </CardHeader>
        <CardContent>
          <div class="text-2xl font-bold text-destructive" data-testid="metric-overdue">{{ overdueTasks.length }}</div>
          <p class="text-xs text-muted-foreground mt-1">
            Requerem atenção urgente
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader class="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle class="text-sm font-medium">
            Criadas Hoje
          </CardTitle>
          <Calendar class="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div class="text-2xl font-bold" data-testid="metric-created-today">{{ tasksCreatedToday }}</div>
          <p class="text-xs text-muted-foreground mt-1">
            Novas tarefas hoje
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader class="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle class="text-sm font-medium">
            Taxa de Conclusão
          </CardTitle>
          <CheckCircle2 class="h-4 w-4 text-green-600" />
        </CardHeader>
        <CardContent>
          <div class="text-2xl font-bold" data-testid="metric-completion-rate">{{ completionRate }}%</div>
          <p class="text-xs text-muted-foreground mt-1">
            {{ completedTasks }} de {{ totalTasks }} tarefas
          </p>
        </CardContent>
      </Card>
    </div>

    <!-- Distribuição por Status e Outras Visualizações -->
    <div class="grid gap-4 md:grid-cols-2">
      <!-- Distribuição por Status -->
      <Card>
        <CardHeader>
          <CardTitle>Distribuição por Status</CardTitle>
          <CardDescription>
            Visualização das tarefas por status
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div class="space-y-4">
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <Clock class="h-4 w-4 text-yellow-600" />
                <span class="text-sm">Pendente</span>
              </div>
              <div class="flex items-center gap-2">
                <div class="w-32 bg-muted rounded-full h-2">
                  <div class="bg-yellow-600 h-2 rounded-full transition-all"
                    :style="{ width: totalTasks > 0 ? `${(pendingTasks / totalTasks) * 100}%` : '0%' }"></div>
                </div>
                <span class="text-sm font-medium w-8 text-right">{{ pendingTasks }}</span>
              </div>
            </div>

            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <TrendingUp class="h-4 w-4 text-blue-600" />
                <span class="text-sm">Em Progresso</span>
              </div>
              <div class="flex items-center gap-2">
                <div class="w-32 bg-muted rounded-full h-2">
                  <div class="bg-blue-600 h-2 rounded-full transition-all"
                    :style="{ width: totalTasks > 0 ? `${(inProgressTasks / totalTasks) * 100}%` : '0%' }"></div>
                </div>
                <span class="text-sm font-medium w-8 text-right">{{ inProgressTasks }}</span>
              </div>
            </div>

            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <CheckCircle2 class="h-4 w-4 text-green-600" />
                <span class="text-sm">Concluída</span>
              </div>
              <div class="flex items-center gap-2">
                <div class="w-32 bg-muted rounded-full h-2">
                  <div class="bg-green-600 h-2 rounded-full transition-all"
                    :style="{ width: totalTasks > 0 ? `${(completedTasks / totalTasks) * 100}%` : '0%' }"></div>
                </div>
                <span class="text-sm font-medium w-8 text-right">{{ completedTasks }}</span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <!-- Top Usuários -->
      <Card>
        <CardHeader>
          <CardTitle class="flex items-center gap-2">
            <Users class="h-5 w-5" />
            Top Usuários
          </CardTitle>
          <CardDescription>
            Usuários com mais tarefas atribuídas
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div class="space-y-3">
            <div v-for="(user, index) in tasksByUser" :key="index" class="flex items-center justify-between">
              <div class="flex items-center gap-3">
                <div class="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-sm font-medium">
                  {{ user.userName.charAt(0).toUpperCase() }}
                </div>
                <span class="text-sm font-medium">{{ user.userName }}</span>
              </div>
              <div class="flex items-center gap-2">
                <div class="w-24 bg-muted rounded-full h-2">
                  <div class="bg-primary h-2 rounded-full transition-all"
                    :style="{ width: totalTasks > 0 ? `${(user.count / totalTasks) * 100}%` : '0%' }"></div>
                </div>
                <span class="text-sm font-medium w-6 text-right">{{ user.count }}</span>
              </div>
            </div>
            <p v-if="tasksByUser.length === 0" class="text-sm text-muted-foreground text-center py-4">
              Nenhum dado disponível
            </p>
          </div>
        </CardContent>
      </Card>
    </div>

    <!-- Tarefas Vencidas e Recentes -->
    <div class="grid gap-4 md:grid-cols-2">
      <!-- Tarefas Vencidas -->
      <Card>
        <CardHeader>
          <CardTitle class="flex items-center gap-2">
            <AlertCircle class="h-5 w-5 text-destructive" />
            Tarefas Vencidas
          </CardTitle>
          <CardDescription>
            Tarefas que passaram da data de vencimento
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div class="space-y-3">
            <div v-for="task in overdueTasks.slice(0, 5)" :key="task.id"
              class="flex items-start justify-between p-3 rounded-lg border border-destructive/20 bg-destructive/5">
              <div class="flex-1">
                <p class="text-sm font-medium line-clamp-1">{{ task.title || 'Sem título' }}</p>
                <p class="text-xs text-muted-foreground mt-1">
                  Vencida em {{ formatDate(task.dueDate || '') }}
                </p>
                <p v-if="task.userName" class="text-xs text-muted-foreground mt-1">
                  {{ task.userName }}
                </p>
              </div>
              <span class="text-xs font-medium text-destructive ml-2">
                {{ statusNames[task.status || 0] }}
              </span>
            </div>
            <p v-if="overdueTasks.length === 0" class="text-sm text-muted-foreground text-center py-4">
              Nenhuma tarefa vencida! 🎉
            </p>
            <p v-if="overdueTasks.length > 5" class="text-xs text-muted-foreground text-center pt-2">
              E mais {{ overdueTasks.length - 5 }} tarefa(s) vencida(s)
            </p>
          </div>
        </CardContent>
      </Card>

      <!-- Tarefas Recentes -->
      <Card>
        <CardHeader>
          <CardTitle class="flex items-center gap-2">
            <Calendar class="h-5 w-5" />
            Tarefas Recentes
          </CardTitle>
          <CardDescription>
            Últimas tarefas criadas no sistema
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div class="space-y-3">
            <div v-for="task in recentTasks" :key="task.id"
              class="flex items-start justify-between p-3 rounded-lg border">
              <div class="flex-1">
                <p class="text-sm font-medium line-clamp-1">{{ task.title || 'Sem título' }}</p>
                <p class="text-xs text-muted-foreground mt-1">
                  Criada em {{ formatDate(task.createdAt || '') }}
                </p>
                <p v-if="task.userName" class="text-xs text-muted-foreground mt-1">
                  {{ task.userName }}
                </p>
              </div>
              <span class="text-xs font-medium ml-2" :class="{
                'text-yellow-600': task.status === 0,
                'text-blue-600': task.status === 1,
                'text-green-600': task.status === 2,
              }">
                {{ statusNames[task.status || 0] }}
              </span>
            </div>
            <p v-if="recentTasks.length === 0" class="text-sm text-muted-foreground text-center py-4">
              Nenhuma tarefa encontrada
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  </div>
</template>

<style scoped>
.line-clamp-1 {
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
