<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useCookieConsent } from '@/composables/useCookieConsent'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/components/ui/tabs'
import { Form, Field as FormField } from 'vee-validate'
import * as z from 'zod'
import { toTypedSchema } from '@vee-validate/zod'

const router = useRouter()
const authStore = useAuthStore()
const { hasConsent } = useCookieConsent()

const activeTab = ref('login')
const showConsentBanner = () => {
  window.dispatchEvent(new Event('cookie-consent:show'))
}

onMounted(() => {
  if (!hasConsent.value) {
    showConsentBanner()
  }
})

watch(hasConsent, (value) => {
  if (!value) {
    showConsentBanner()
  }
})

// Schema de validação para Login
const loginSchema = z.object({
  userNameOrEmail: z.string().min(1, 'Usuário ou email é obrigatório'),
  password: z.string().min(1, 'Senha é obrigatória'),
})

// Schema de validação para Registro
const registerSchema = z.object({
  email: z.string().email('Email inválido'),
  userName: z.string().min(3, 'Nome de usuário deve ter pelo menos 3 caracteres'),
  password: z.string().min(6, 'Senha deve ter pelo menos 6 caracteres'),
})

const loginFormSchema = toTypedSchema(loginSchema)
const registerFormSchema = toTypedSchema(registerSchema)

const onSubmitLogin = async (values: unknown) => {
  // Verificar se o usuário aceitou os cookies
  if (!hasConsent.value) {
    showConsentBanner()
    toast.error('É necessário aceitar os cookies para fazer login')
    return
  }

  const loginValues = values as z.infer<typeof loginSchema>
  const result = await authStore.login(loginValues)

  if (result.success) {
    toast.success('Login realizado com sucesso!')
    router.push('/')
  } else {
    toast.error(result.error || 'Erro ao fazer login')
  }
}

const onSubmitRegister = async (values: unknown) => {
  // Verificar se o usuário aceitou os cookies
  if (!hasConsent.value) {
    showConsentBanner()
    toast.error('É necessário aceitar os cookies para se registrar')
    return
  }

  const registerValues = values as z.infer<typeof registerSchema>
  const result = await authStore.register(registerValues)

  if (result.success) {
    toast.success('Registro realizado com sucesso! Faça login para continuar.')
    activeTab.value = 'login'
  } else {
    toast.error(result.error || 'Erro ao registrar')
  }
}
</script>

<template>
  <div
    class="min-h-screen flex items-center justify-center bg-gradient-to-br from-background via-background to-muted/20 p-4">
    <div class="w-full max-w-md">
      <Card class="border-border/50 bg-card/50 backdrop-blur-sm shadow-2xl">
        <CardHeader class="space-y-1 text-center">
          <CardTitle class="text-3xl font-bold tracking-tight">
            Bem-vindo
          </CardTitle>
          <CardDescription class="text-base">
            Entre com sua conta ou crie uma nova
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Tabs v-model="activeTab" class="w-full">
            <TabsList class="grid w-full grid-cols-2 mb-6">
              <TabsTrigger value="login">Login</TabsTrigger>
              <TabsTrigger value="register">Registro</TabsTrigger>
            </TabsList>

            <TabsContent value="login" class="space-y-4">
              <Form :validation-schema="loginFormSchema" @submit="onSubmitLogin" class="space-y-4">
                <div class="space-y-2">
                  <Label for="login-username">Usuário ou Email</Label>
                  <FormField v-slot="{ componentField, errors }" name="userNameOrEmail">
                    <Input id="login-username" type="text" placeholder="Digite seu usuário ou email"
                      v-bind="componentField" :class="{ 'border-destructive': errors.length > 0 }" />
                    <p v-if="errors.length" class="text-sm text-destructive mt-1">
                      {{ errors[0] }}
                    </p>
                  </FormField>
                </div>

                <div class="space-y-2">
                  <Label for="login-password">Senha</Label>
                  <FormField v-slot="{ componentField, errors }" name="password">
                    <Input id="login-password" type="password" placeholder="Digite sua senha" v-bind="componentField"
                      :class="{ 'border-destructive': errors.length > 0 }" />
                    <p v-if="errors.length" class="text-sm text-destructive mt-1">
                      {{ errors[0] }}
                    </p>
                  </FormField>
                </div>

                <Button type="submit" class="w-full" :disabled="authStore.loading">
                  {{ authStore.loading ? 'Entrando...' : 'Entrar' }}
                </Button>
              </Form>
            </TabsContent>

            <TabsContent value="register" class="space-y-4">
              <Form :validation-schema="registerFormSchema" @submit="onSubmitRegister" class="space-y-4">
                <div class="space-y-2">
                  <Label for="register-email">Email</Label>
                  <FormField v-slot="{ componentField, errors }" name="email">
                    <Input id="register-email" type="email" placeholder="email@exemplo.com" v-bind="componentField"
                      :class="{ 'border-destructive': errors.length > 0 }" />
                    <p v-if="errors.length" class="text-sm text-destructive mt-1">
                      {{ errors[0] }}
                    </p>
                  </FormField>
                </div>

                <div class="space-y-2">
                  <Label for="register-username">Nome de Usuário</Label>
                  <FormField v-slot="{ componentField, errors }" name="userName">
                    <Input id="register-username" type="text" placeholder="seu.usuario" v-bind="componentField"
                      :class="{ 'border-destructive': errors.length > 0 }" />
                    <p v-if="errors.length" class="text-sm text-destructive mt-1">
                      {{ errors[0] }}
                    </p>
                  </FormField>
                </div>

                <div class="space-y-2">
                  <Label for="register-password">Senha</Label>
                  <FormField v-slot="{ componentField, errors }" name="password">
                    <Input id="register-password" type="password" placeholder="••••••••" v-bind="componentField"
                      :class="{ 'border-destructive': errors.length > 0 }" />
                    <p v-if="errors.length" class="text-sm text-destructive mt-1">
                      {{ errors[0] }}
                    </p>
                  </FormField>
                </div>

                <Button type="submit" class="w-full" :disabled="authStore.loading">
                  {{ authStore.loading ? 'Registrando...' : 'Registrar' }}
                </Button>
              </Form>
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>
    </div>
  </div>
</template>

<style scoped>
/* Estilos adicionais para o design Scalar */
</style>
