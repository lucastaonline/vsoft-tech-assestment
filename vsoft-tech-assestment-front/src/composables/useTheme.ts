import { useDark, useToggle } from '@vueuse/core'
import { computed } from 'vue'

/**
 * Composable para gerenciar o tema dark/light usando VueUse
 * Segue a recomendação do shadcn-vue: https://www.shadcn-vue.com/docs/dark-mode
 */
export function useTheme() {
    // useDark do VueUse gerencia automaticamente:
    // - Adiciona/remove classe 'dark' no html
    // - Persiste no localStorage
    // - Detecta preferência do sistema quando não há valor salvo
    const isDark = useDark({
        // Quando não há valor salvo, detecta a preferência do sistema
        // através do media query prefers-color-scheme
        // Se o usuário já escolheu um tema, usa o valor salvo
        attribute: 'class',
        selector: 'html',
        storageKey: 'theme',
        // Não definir initialValue permite que use a preferência do sistema
    })

    const toggle = useToggle(isDark)

    // Computed para retornar 'dark' ou 'light' como string
    const theme = computed(() => (isDark.value ? 'dark' : 'light'))

    const setTheme = (newTheme: 'dark' | 'light') => {
        isDark.value = newTheme === 'dark'
    }

    const toggleTheme = () => {
        toggle()
    }

    return {
        isDark,
        theme,
        setTheme,
        toggleTheme,
    }
}

