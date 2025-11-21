<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { CalendarScope, CalendarLinkResponse } from '@/services/calendarService'
import { downloadCalendarFile, generateCalendarLink } from '@/services/calendarService'

const props = withDefaults(defineProps<{
  title?: string
  description?: string
  layout?: 'default' | 'compact'
  variant?: 'card' | 'menu'
}>(), {
  title: 'Integração iCalendar',
  description: 'Sincronize suas tarefas em clientes como Outlook ou Thunderbird.',
  layout: 'default',
  variant: 'card',
})

const panelScope = ref<CalendarScope>('user')
const loading = ref(false)
const downloading = ref(false)
const linkResponse = ref<CalendarLinkResponse | null>(null)

const scopeOptions: Array<{ value: CalendarScope; label: string; helper: string }> = [
  {
    value: 'user',
    label: 'Minhas tarefas',
    helper: 'Somente tarefas atribuídas a você.',
  },
  {
    value: 'all',
    label: 'Todas as tarefas',
    helper: 'Inclui todas as tarefas do board.',
  },
]

const issuedAtText = computed(() => {
  if (!linkResponse.value?.issuedAt) return ''
  return new Date(linkResponse.value.issuedAt).toLocaleString('pt-BR')
})

const disabledActions = computed(() => loading.value || downloading.value)
const isCardVariant = computed(() => props.variant === 'card')

watch(panelScope, () => {
  if (!isCardVariant.value) return
  linkResponse.value = null
})

const generateLink = async (scope: CalendarScope) => {
  return await generateCalendarLink(scope)
}

const copyLink = async (scope: CalendarScope, reference?: CalendarLinkResponse | null) => {
  try {
    const response = reference ?? await generateLink(scope)
    await navigator.clipboard.writeText(response.url)
    toast.success('Link copiado!')
    return response
  } catch (error) {
    console.error(error)
    toast.error('Não foi possível copiar o link.')
    throw error
  }
}

const downloadFile = async (scope: CalendarScope, reference?: CalendarLinkResponse | null) => {
  downloading.value = true
  try {
    const response = reference ?? await generateLink(scope)
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
    return response
  } catch (error) {
    console.error(error)
    toast.error('Não foi possível baixar o arquivo.')
    throw error
  } finally {
    downloading.value = false
  }
}

const handleGenerateCard = async () => {
  loading.value = true
  try {
    linkResponse.value = await generateLink(panelScope.value)
    toast.success('Link do calendário gerado!')
  } catch (error) {
    console.error(error)
    toast.error('Não foi possível gerar o link.')
  } finally {
    loading.value = false
  }
}

const handleCopyCard = async () => {
  if (!linkResponse.value) {
    linkResponse.value = await copyLink(panelScope.value)
  } else {
    await copyLink(panelScope.value, linkResponse.value)
  }
}

const handleDownloadCard = async () => {
  linkResponse.value = await downloadFile(panelScope.value, linkResponse.value || undefined)
}

const handleMenuCopy = async (scope: CalendarScope) => {
  loading.value = true
  try {
    await copyLink(scope)
  } finally {
    loading.value = false
  }
}

const handleMenuDownload = async (scope: CalendarScope) => {
  await downloadFile(scope)
}
</script>

<template>
  <div v-if="isCardVariant">
    <Card :class="layout === 'compact' ? 'border-dashed' : ''">
      <CardHeader class="space-y-2">
        <CardTitle class="text-base sm:text-lg">
          {{ title }}
        </CardTitle>
        <CardDescription class="text-sm">
          {{ description }}
        </CardDescription>
      </CardHeader>
      <CardContent class="space-y-4 text-sm">
        <div class="space-y-2">
          <Label class="text-[11px] uppercase tracking-tight text-muted-foreground">Escopo</Label>
          <div class="grid gap-2 sm:grid-cols-2">
            <button
              v-for="option in scopeOptions"
              :key="option.value"
              type="button"
              class="rounded-md border px-3 py-2 text-left transition text-sm"
              :class="panelScope === option.value ? 'border-primary bg-primary/5' : ''"
              @click="panelScope = option.value"
              :disabled="loading"
            >
              <p class="font-medium">{{ option.label }}</p>
              <p class="text-xs text-muted-foreground mt-1">
                {{ option.helper }}
              </p>
            </button>
          </div>
        </div>

        <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <p class="text-muted-foreground text-xs sm:text-sm">
            Gere um link seguro e compartilhe com o seu calendário.
          </p>
          <Button size="sm" :disabled="loading" @click="handleGenerateCard">
            {{ loading ? 'Gerando...' : 'Gerar link' }}
          </Button>
        </div>

        <div v-if="linkResponse" class="space-y-3 rounded-md border bg-muted/40 p-3">
          <div class="space-y-2">
            <Label class="text-[11px] uppercase text-muted-foreground tracking-wide">URL protegida</Label>
            <div class="flex flex-col gap-2 md:flex-row">
              <Input :value="linkResponse.url" readonly class="font-mono text-xs" />
              <div class="flex gap-2 md:w-48">
                <Button type="button" variant="secondary" size="sm" class="flex-1" @click="handleCopyCard">
                  Copiar
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  class="flex-1"
                  :disabled="disabledActions"
                  @click="handleDownloadCard"
                >
                  {{ downloading ? 'Baixando...' : 'Baixar' }}
                </Button>
              </div>
            </div>
          </div>
          <p class="text-[11px] text-muted-foreground">
            Escopo: <span class="font-medium uppercase">{{ linkResponse.scope }}</span>
            <span v-if="issuedAtText">• Gerado em {{ issuedAtText }}</span>
          </p>
        </div>
      </CardContent>
    </Card>
  </div>

  <div v-else>
    <DropdownMenu>
      <DropdownMenuTrigger as-child>
        <Button size="sm" :disabled="loading || downloading">
          Integração iCal
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent class="w-64">
        <DropdownMenuLabel>Minhas tarefas</DropdownMenuLabel>
        <DropdownMenuGroup>
          <DropdownMenuItem @click="handleMenuCopy('user')">
            Copiar link
          </DropdownMenuItem>
          <DropdownMenuItem @click="handleMenuDownload('user')">
            Baixar arquivo
          </DropdownMenuItem>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuLabel>Todas as tarefas</DropdownMenuLabel>
        <DropdownMenuGroup>
          <DropdownMenuItem @click="handleMenuCopy('all')">
            Copiar link
          </DropdownMenuItem>
          <DropdownMenuItem @click="handleMenuDownload('all')">
            Baixar arquivo
          </DropdownMenuItem>
        </DropdownMenuGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  </div>
</template>


