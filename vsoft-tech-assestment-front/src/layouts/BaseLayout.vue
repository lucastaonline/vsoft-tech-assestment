<script setup lang="ts">
import { Toaster } from '@/components/ui/sonner'
import { useTheme } from '@/composables/useTheme'

// Inicializar o tema globalmente
// O useDark do VueUse gerencia automaticamente:
// - Adiciona/remove classe 'dark' no HTML
// - Persiste no localStorage
// - Aplica imediatamente ao ser chamado
useTheme()
</script>

<template>
  <div class="min-h-screen w-full bg-background text-foreground">
    <!-- Conteúdo da aplicação -->
    <slot />
    
    <!-- Toaster global -->
    <Toaster />
  </div>
</template>

<style scoped></style>

