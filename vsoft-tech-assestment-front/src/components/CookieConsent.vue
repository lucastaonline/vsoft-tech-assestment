<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { X } from 'lucide-vue-next'

const showBanner = ref(false)

onMounted(() => {
  // Verificar se o usuário já aceitou os cookies
  const consent = localStorage.getItem('cookie_consent')
  if (!consent) {
    showBanner.value = true
  }
})

const acceptCookies = () => {
  localStorage.setItem('cookie_consent', 'accepted')
  showBanner.value = false
}

const rejectCookies = () => {
  localStorage.setItem('cookie_consent', 'rejected')
  showBanner.value = false
  // Avisar que algumas funcionalidades podem não funcionar
  alert('Algumas funcionalidades podem não funcionar sem cookies. Por favor, aceite os cookies para uma melhor experiência.')
}
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition ease-out duration-300"
      enter-from-class="opacity-0 translate-y-4"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition ease-in duration-200"
      leave-from-class="opacity-100 translate-y-0"
      leave-to-class="opacity-0 translate-y-4"
    >
      <div
        v-if="showBanner"
        class="fixed bottom-0 left-0 right-0 z-50 p-4 bg-background/95 backdrop-blur-sm border-t"
      >
        <Card class="max-w-4xl mx-auto">
          <CardContent class="p-6">
            <div class="flex items-start justify-between gap-4">
              <div class="flex-1">
                <h3 class="font-semibold text-lg mb-2">Uso de Cookies</h3>
                <p class="text-sm text-muted-foreground">
                  Este site utiliza cookies para autenticação e segurança. Os cookies são essenciais para o funcionamento do sistema e são armazenados de forma segura (HttpOnly). 
                  Ao continuar, você concorda com o uso de cookies.
                </p>
              </div>
              <Button
                variant="ghost"
                size="icon"
                class="h-8 w-8 shrink-0"
                @click="rejectCookies"
              >
                <X class="h-4 w-4" />
              </Button>
            </div>
            <div class="flex items-center gap-3 mt-4">
              <Button @click="acceptCookies">
                Aceitar Cookies
              </Button>
              <Button variant="outline" @click="rejectCookies">
                Recusar
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </Transition>
  </Teleport>
</template>

