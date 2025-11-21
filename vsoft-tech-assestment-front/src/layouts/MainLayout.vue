<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useTheme } from '@/composables/useTheme'
import { Button } from '@/components/ui/button'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Label } from '@/components/ui/label'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Moon, Sun, LogOut, Home, LayoutDashboard, Calendar } from 'lucide-vue-next'
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { toast } from 'vue-sonner'
import NotificationBell from '@/components/NotificationBell.vue'
import type { CalendarScope } from '@/services/calendarService'
import { generateCalendarLink, downloadCalendarFile } from '@/services/calendarService'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const { isDark, toggleTheme } = useTheme()
const isHome = computed(() => route.name === 'home')

type CalendarAction = 'copy' | 'download'

const isScopeDialogOpen = ref(false)
const pendingAction = ref<CalendarAction | null>(null)
const selectedScope = ref<CalendarScope>('user')
const actionLoading = ref(false)

const userInitials = computed(() => {
  if (authStore.user?.userName) {
    return authStore.user.userName
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2)
  }
  return 'U'
})

const handleLogout = async () => {
  await authStore.logout()
  router.push('/login')
}

const triggerScopeDialog = (action: CalendarAction) => {
  pendingAction.value = action
  selectedScope.value = 'user'
  isScopeDialogOpen.value = true
}

const fallbackCopy = (text: string) => {
  const textarea = document.createElement('textarea')
  textarea.value = text
  textarea.style.position = 'fixed'
  textarea.style.opacity = '0'
  document.body.appendChild(textarea)
  textarea.focus()
  textarea.select()
  document.execCommand('copy')
  textarea.remove()
}

const copyCalendarLink = async (scope: CalendarScope) => {
  const response = await generateCalendarLink(scope)
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(response.url)
    } else {
      fallbackCopy(response.url)
    }
    toast.success('Link copiado!')
  } catch (error) {
    console.error(error)
    toast.error('Não foi possível copiar o link.')
    throw error
  }
}

const downloadCalendarFileForScope = async (scope: CalendarScope) => {
  try {
    const response = await generateCalendarLink(scope)
    const blob = await downloadCalendarFile(response.token)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = scope === 'all' ? 'tarefas-sistema.ics' : 'minhas-tarefas.ics'
    document.body.appendChild(link)
    link.click()
    link.remove()
    URL.revokeObjectURL(url)
    toast.success('Arquivo iCalendar baixado!')
  } catch (error) {
    console.error(error)
    toast.error('Não foi possível baixar o arquivo.')
    throw error
  }
}

const confirmScopeSelection = async () => {
  if (!pendingAction.value || actionLoading.value) return

  actionLoading.value = true
  try {
    if (pendingAction.value === 'copy') {
      await copyCalendarLink(selectedScope.value)
    } else {
      await downloadCalendarFileForScope(selectedScope.value)
    }
    isScopeDialogOpen.value = false
    pendingAction.value = null
  } catch (error) {
    console.error(error)
    // mensagens específicas já são mostradas em cada função
  } finally {
    actionLoading.value = false
  }
}

const confirmLabel = computed(() => {
  if (pendingAction.value === 'download') return actionLoading.value ? 'Baixando...' : 'Baixar'
  return actionLoading.value ? 'Copiando...' : 'Copiar'
})

const handleDialogOpenChange = (val: boolean) => {
  isScopeDialogOpen.value = val
}
</script>

<template>
  <div class="flex min-h-screen w-full flex-col bg-background">
    <header
      class="sticky top-0 z-50 w-full border-b border-border/40 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div class="container mx-auto flex h-16 items-center justify-between px-4">
        <div class="flex items-center gap-6">
          <router-link to="/" class="flex items-center space-x-2">
            <span class="text-xl font-bold bg-gradient-to-r from-primary to-primary/60 bg-clip-text text-transparent">
              VSoft
            </span>
          </router-link>

          <!-- Navegação -->
          <nav class="flex items-center gap-2">
            <Button variant="ghost" size="sm" :class="{
              'bg-accent': route.name === 'home',
            }" @click="router.push('/')">
              <Home class="mr-2 h-4 w-4" />
              Home
            </Button>
            <Button variant="ghost" size="sm" :class="{
              'bg-accent': route.name === 'dashboard',
            }" @click="router.push('/dashboard')">
              <LayoutDashboard class="mr-2 h-4 w-4" />
              Dashboard
            </Button>
          </nav>
        </div>

        <div class="flex items-center gap-4">
          <div class="flex items-center gap-2" v-if="authStore.isAuthenticated">
            <DropdownMenu>
              <DropdownMenuTrigger as-child>
                <Button variant="ghost" size="icon" class="h-9 w-9">
                  <Calendar class="h-4 w-4" />
                  <span class="sr-only">Integração iCal</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" class="w-48">
                <DropdownMenuLabel class="text-sm font-semibold">
                  Integração iCal
                </DropdownMenuLabel>
                <DropdownMenuLabel class="text-xs text-muted-foreground font-normal">
                  Sincronize com Outlook/Thunderbird
                </DropdownMenuLabel>
                <DropdownMenuItem @click="triggerScopeDialog('copy')">
                  Copiar link
                </DropdownMenuItem>
                <DropdownMenuItem @click="triggerScopeDialog('download')">
                  Baixar arquivo
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>

            <!-- Notificações -->
            <NotificationBell />
          </div>

          <!-- Toggle de Tema -->
          <Button variant="ghost" size="icon" @click="toggleTheme" class="h-9 w-9">
            <Sun v-if="isDark" class="h-4 w-4" />
            <Moon v-else class="h-4 w-4" />
            <span class="sr-only">Alternar tema</span>
          </Button>

          <!-- Menu do Usuário -->
          <DropdownMenu v-if="authStore.isAuthenticated">
            <DropdownMenuTrigger as-child>
              <Button variant="ghost" class="relative h-9 w-9 rounded-full">
                <Avatar class="h-9 w-9">
                  <AvatarImage :src="''" :alt="authStore.user?.userName || 'Usuário'" />
                  <AvatarFallback>{{ userInitials }}</AvatarFallback>
                </Avatar>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" class="w-56">
              <DropdownMenuLabel class="font-normal">
                <div class="flex flex-col space-y-1">
                  <p class="text-sm font-medium leading-none">
                    {{ authStore.user?.userName }}
                  </p>
                  <p class="text-xs leading-none text-muted-foreground">
                    {{ authStore.user?.email }}
                  </p>
                </div>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem @click="handleLogout" class="cursor-pointer">
                <LogOut class="mr-2 h-4 w-4" />
                <span>Sair</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </header>

    <main class="flex-1 overflow-hidden">
      <div :class="['container mx-auto px-4 h-full', isHome ? 'py-0' : 'py-8']">
        <router-view />
      </div>
    </main>
  </div>

  <AlertDialog :open="isScopeDialogOpen" @update:open="handleDialogOpenChange">
    <AlertDialogContent>
      <AlertDialogHeader>
        <AlertDialogTitle>Escolha o escopo</AlertDialogTitle>
        <AlertDialogDescription>
          Defina se o link/arquivo deve conter apenas suas tarefas ou todas as tarefas do board.
        </AlertDialogDescription>
      </AlertDialogHeader>
      <div class="space-y-2">
        <Label class="text-xs uppercase text-muted-foreground">Escopo</Label>
        <div class="grid gap-2 sm:grid-cols-2">
          <button type="button" class="rounded-md border px-3 py-2 text-left transition text-sm"
            :class="selectedScope === 'user' ? 'border-primary bg-primary/5' : ''" @click="selectedScope = 'user'">
            <p class="font-medium">Minhas tarefas</p>
            <p class="text-xs text-muted-foreground mt-1">Somente atividades atribuídas a você.</p>
          </button>
          <button type="button" class="rounded-md border px-3 py-2 text-left transition text-sm"
            :class="selectedScope === 'all' ? 'border-primary bg-primary/5' : ''" @click="selectedScope = 'all'">
            <p class="font-medium">Todas as tarefas</p>
            <p class="text-xs text-muted-foreground mt-1">Inclui todo o board.</p>
          </button>
        </div>
      </div>
      <AlertDialogFooter>
        <AlertDialogCancel :disabled="actionLoading" @click="pendingAction = null">
          Cancelar
        </AlertDialogCancel>
        <AlertDialogAction as-child>
          <Button type="button" :disabled="actionLoading" @click="confirmScopeSelection">
            {{ confirmLabel }}
          </Button>
        </AlertDialogAction>
      </AlertDialogFooter>
    </AlertDialogContent>
  </AlertDialog>
</template>
