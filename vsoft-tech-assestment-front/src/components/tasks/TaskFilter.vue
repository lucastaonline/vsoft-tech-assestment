<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Filter, X } from 'lucide-vue-next'
import type { TaskStatus } from '@/lib/api/types.gen'
import type { UserListItemResponse } from '@/stores/users'

export interface ColumnFilter {
  title?: string
  dueDateFrom?: string
  dueDateTo?: string
  userId?: string
}

const props = defineProps<{
  status: TaskStatus
  users?: UserListItemResponse[]
}>()

const emit = defineEmits<{
  filter: [filter: ColumnFilter]
  clear: []
}>()

const titleFilter = ref('')
const dueDateFrom = ref('')
const dueDateTo = ref('')
const selectedUserId = ref<string>('')
const isOpen = ref(false)

const hasActiveFilters = computed(() => {
  return !!(titleFilter.value || dueDateFrom.value || dueDateTo.value || selectedUserId.value)
})

const applyFilter = () => {
  const filter: ColumnFilter = {}
  
  if (titleFilter.value) {
    filter.title = titleFilter.value
  }
  if (dueDateFrom.value) {
    filter.dueDateFrom = dueDateFrom.value
  }
  if (dueDateTo.value) {
    filter.dueDateTo = dueDateTo.value
  }
  if (selectedUserId.value) {
    filter.userId = selectedUserId.value
  }
  
  emit('filter', filter)
}

const clearFilters = () => {
  titleFilter.value = ''
  dueDateFrom.value = ''
  dueDateTo.value = ''
  selectedUserId.value = ''
  emit('clear')
}

watch([titleFilter, dueDateFrom, dueDateTo, selectedUserId], () => {
  applyFilter()
})
</script>

<template>
  <DropdownMenu v-model:open="isOpen">
    <DropdownMenuTrigger as-child>
      <Button
        variant="ghost"
        size="icon"
        class="h-8 w-8"
        :class="{ 'bg-accent': hasActiveFilters }"
      >
        <Filter class="h-4 w-4" />
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent class="w-80" align="end">
      <div class="space-y-4 p-2">
        <div class="flex items-center justify-between">
          <h4 class="font-semibold">Filtros</h4>
          <Button
            v-if="hasActiveFilters"
            variant="ghost"
            size="icon"
            class="h-6 w-6"
            @click="clearFilters"
          >
            <X class="h-4 w-4" />
          </Button>
        </div>

        <div class="space-y-2">
          <Label for="filter-title">Título</Label>
          <Input
            id="filter-title"
            v-model="titleFilter"
            placeholder="Buscar por título..."
          />
        </div>

        <div class="space-y-2">
          <Label>Data de vencimento</Label>
          <div class="grid grid-cols-2 gap-2">
            <div class="space-y-1">
              <Label for="filter-date-from" class="text-xs">De</Label>
              <Input
                id="filter-date-from"
                v-model="dueDateFrom"
                type="date"
              />
            </div>
            <div class="space-y-1">
              <Label for="filter-date-to" class="text-xs">Até</Label>
              <Input
                id="filter-date-to"
                v-model="dueDateTo"
                type="date"
              />
            </div>
          </div>
        </div>

        <div v-if="users && users.length > 0" class="space-y-2">
          <Label for="filter-user">Responsável</Label>
          <select
            id="filter-user"
            v-model="selectedUserId"
            class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
          >
            <option value="">Todos</option>
            <option
              v-for="user in users"
              :key="user.id"
              :value="user.id"
            >
              {{ user.userName || user.email }}
            </option>
          </select>
        </div>
      </div>
    </DropdownMenuContent>
  </DropdownMenu>
</template>

