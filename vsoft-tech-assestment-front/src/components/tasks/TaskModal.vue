<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { X } from 'lucide-vue-next'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { useAuthStore } from '@/stores/auth'
import type { TaskResponse, TaskStatus, CreateTaskRequest, UpdateTaskRequest } from '@/lib/api/types.gen'
import type { UserListItemResponse } from '@/stores/users'

const props = defineProps<{
  open: boolean
  task?: TaskResponse | null
  status: TaskStatus
  users?: UserListItemResponse[]
  canEdit?: boolean
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  'save': [data: CreateTaskRequest | UpdateTaskRequest]
  'delete': [taskId: string]
}>()

const authStore = useAuthStore()

const title = ref('')
const description = ref('')
const dueDate = ref('')
const selectedUserId = ref<string>('')
const descriptionPreview = ref('')

// Quando o modal abrir ou task mudar, preencher campos
watch([() => props.open, () => props.task], () => {
  if (props.open) {
    if (props.task) {
      // Modo edição
      title.value = props.task.title || ''
      description.value = props.task.description || ''
      dueDate.value = props.task.dueDate ? new Date(props.task.dueDate).toISOString().split('T')[0] || '' : ''
      selectedUserId.value = props.task.userId || (authStore.user?.id || '')
    } else {
      // Modo criação
      title.value = ''
      description.value = ''
      dueDate.value = ''
      selectedUserId.value = authStore.user?.id || ''
    }
    updatePreview()
  }
}, { immediate: true })

// Atualizar preview do markdown
const updatePreview = async () => {
  if (description.value) {
    const html = await marked(description.value)
    descriptionPreview.value = DOMPurify.sanitize(html as string)
  } else {
    descriptionPreview.value = ''
  }
}

watch(description, updatePreview)

const isValid = computed(() => {
  return title.value.trim().length > 0 && 
         description.value.trim().length > 0 && 
         dueDate.value.length > 0 &&
         selectedUserId.value.length > 0
})

const handleSave = () => {
  if (!isValid.value) return

  if (props.task) {
    // Modo edição - UpdateTaskRequest (userId é obrigatório)
    if (!selectedUserId.value) {
      return // Não deve acontecer devido à validação, mas garantindo segurança
    }
    const taskData: UpdateTaskRequest = {
      title: title.value.trim(),
      description: description.value.trim(),
      dueDate: new Date(dueDate.value).toISOString(),
      status: props.task.status || props.status,
      userId: selectedUserId.value,
    }
    emit('save', taskData)
  } else {
    // Modo criação - CreateTaskRequest
    const taskData: CreateTaskRequest = {
      title: title.value.trim(),
      description: description.value.trim(),
      dueDate: new Date(dueDate.value).toISOString(),
      status: props.status,
      userId: selectedUserId.value || null,
    }
    emit('save', taskData)
  }

  handleClose()
}

const handleClose = () => {
  emit('update:open', false)
}

const handleDelete = () => {
  if (!props.task?.id) return
  
  if (confirm('Tem certeza que deseja deletar esta tarefa?')) {
    emit('delete', props.task.id)
    handleClose()
  }
}

// Computed para verificar se está em modo de edição e se pode editar
const isEditMode = computed(() => !!props.task)
const canEditFields = computed(() => {
  // Se não há task, está criando, então pode editar
  if (!props.task) return true
  // Se há task, verifica a prop canEdit
  return props.canEdit !== false
})

// Fechar ao pressionar ESC
const handleKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Escape' && props.open) {
    handleClose()
  }
}

// Adicionar listener quando modal abrir
watch(() => props.open, (isOpen) => {
  if (isOpen) {
    document.addEventListener('keydown', handleKeydown)
  } else {
    document.removeEventListener('keydown', handleKeydown)
  }
})
</script>

<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm"
      @click.self="handleClose"
    >
      <Card class="w-full max-w-4xl max-h-[90vh] flex flex-col m-4">
        <CardHeader class="flex flex-row items-center justify-between space-y-0 pb-4">
          <CardTitle>{{ task ? 'Editar Tarefa' : 'Nova Tarefa' }}</CardTitle>
          <Button variant="ghost" size="icon" class="h-8 w-8" @click="handleClose">
            <X class="h-4 w-4" />
          </Button>
        </CardHeader>

        <CardContent class="flex-1 overflow-y-auto space-y-6">
          <!-- Título -->
          <div class="space-y-2">
            <Label for="task-title">Título *</Label>
            <Input
              id="task-title"
              v-model="title"
              placeholder="Digite o título da tarefa"
              class="w-full"
              :disabled="!canEditFields"
            />
          </div>

          <!-- Corpo: Descrição (esquerda) e Informações (direita) -->
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <!-- Descrição com Markdown -->
            <div class="space-y-2">
              <Label for="task-description">Descrição (Markdown) *</Label>
              <div class="grid grid-cols-2 gap-2 border rounded-lg overflow-hidden">
                <div class="flex flex-col">
                  <div class="px-3 py-2 bg-muted text-xs font-medium border-b">
                    Editor
                  </div>
                  <textarea
                    id="task-description"
                    v-model="description"
                    placeholder="Digite a descrição em Markdown..."
                    class="flex-1 p-3 resize-none border-0 focus:outline-none focus:ring-0 bg-background text-sm font-mono disabled:opacity-50 disabled:cursor-not-allowed"
                    rows="12"
                    :disabled="!canEditFields"
                  />
                </div>
                <div class="flex flex-col border-l">
                  <div class="px-3 py-2 bg-muted text-xs font-medium border-b">
                    Preview
                  </div>
                  <div
                    class="flex-1 p-3 overflow-y-auto text-sm prose prose-sm dark:prose-invert max-w-none"
                    v-html="descriptionPreview || '<p class=&quot;text-muted-foreground&quot;>Preview aparecerá aqui...</p>'"
                  />
                </div>
              </div>
            </div>

            <!-- Informações (direita) -->
            <div class="space-y-4">
              <!-- Responsável -->
              <div class="space-y-2">
                <Label for="task-user">Responsável *</Label>
                <select
                  id="task-user"
                  v-model="selectedUserId"
                  class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
                  :disabled="!canEditFields"
                >
                  <option v-if="users && users.length > 0" value="">Selecione um usuário</option>
                  <option
                    v-for="user in users"
                    :key="user.id"
                    :value="user.id"
                  >
                    {{ user.userName || user.email }}
                  </option>
                </select>
              </div>

              <!-- Data de Vencimento -->
              <div class="space-y-2">
                <Label for="task-due-date">Data de Vencimento *</Label>
                <Input
                  id="task-due-date"
                  v-model="dueDate"
                  type="date"
                  class="w-full"
                  :disabled="!canEditFields"
                />
              </div>
            </div>
          </div>
        </CardContent>

        <!-- Footer com botões -->
        <div class="flex items-center justify-between p-6 border-t">
          <div>
            <Button
              v-if="isEditMode && canEditFields"
              variant="destructive"
              @click="handleDelete"
            >
              Deletar
            </Button>
          </div>
          <div class="flex items-center gap-3">
            <Button variant="outline" @click="handleClose">
              {{ isEditMode && !canEditFields ? 'Fechar' : 'Cancelar' }}
            </Button>
            <Button
              v-if="canEditFields"
              :disabled="!isValid"
              @click="handleSave"
            >
              {{ task ? 'Salvar' : 'Criar' }}
            </Button>
          </div>
        </div>
      </Card>
    </div>
  </Teleport>
</template>

<style scoped>
/* Estilos para o preview de markdown */
.prose {
  color: hsl(var(--foreground));
}

.prose p {
  margin-top: 0.5em;
  margin-bottom: 0.5em;
}

.prose h1, .prose h2, .prose h3, .prose h4, .prose h5, .prose h6 {
  margin-top: 1em;
  margin-bottom: 0.5em;
  font-weight: 600;
}

.prose code {
  background: hsl(var(--muted));
  padding: 0.125rem 0.25rem;
  border-radius: 0.25rem;
  font-size: 0.875em;
}

.prose pre {
  background: hsl(var(--muted));
  padding: 0.75rem;
  border-radius: 0.5rem;
  overflow-x: auto;
}

.prose ul, .prose ol {
  margin-left: 1.5em;
  margin-top: 0.5em;
  margin-bottom: 0.5em;
}

.prose a {
  color: hsl(var(--primary));
  text-decoration: underline;
}
</style>

