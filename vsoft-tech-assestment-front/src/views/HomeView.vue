<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

const authStore = useAuthStore()
</script>

<template>
  <div class="space-y-8">
    <div>
      <h1 class="text-4xl font-bold tracking-tight">
        Bem-vindo, {{ authStore.user?.userName }}!
      </h1>
      <p class="text-muted-foreground mt-2">
        Você está autenticado e pronto para começar.
      </p>
    </div>

    <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      <Card>
        <CardHeader>
          <CardTitle>Dashboard</CardTitle>
          <CardDescription>
            Visão geral do sistema
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p class="text-sm text-muted-foreground">
            Conteúdo do dashboard será adicionado aqui.
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Tarefas</CardTitle>
          <CardDescription>
            Gerencie suas tarefas
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p class="text-sm text-muted-foreground">
            Sistema de tarefas será implementado aqui.
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Configurações</CardTitle>
          <CardDescription>
            Ajuste suas preferências
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p class="text-sm text-muted-foreground">
            Configurações do usuário.
          </p>
        </CardContent>
      </Card>
    </div>
  </div>
</template>

