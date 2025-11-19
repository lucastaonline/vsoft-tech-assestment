<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

const authStore = useAuthStore()
</script>

<template>
  <div class="space-y-8">
    <div>
      <h1 class="text-4xl font-bold tracking-tight">
        Dashboard
      </h1>
      <p class="text-muted-foreground mt-2">
        Visão geral do sistema
      </p>
    </div>

    <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      <Card>
        <CardHeader>
          <CardTitle>Dashboard</CardTitle>
          <CardDescription>
            Conteúdo do dashboard será implementado aqui
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p class="text-sm text-muted-foreground">
            Esta é uma página placeholder para discussão futura.
          </p>
        </CardContent>
      </Card>
    </div>
  </div>
</template>
