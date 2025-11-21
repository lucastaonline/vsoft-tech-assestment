import { ref, watch, onMounted, onUnmounted, type Ref } from 'vue'

export interface UseIntersectionOptions {
    /**
     * Define se o observer deve continuar monitorando após a primeira interseção.
     * Padrão é true (desconecta após entrar no viewport, mantendo o conteúdo montado).
     */
    once?: boolean
    /**
     * Opções nativas do IntersectionObserver.
     */
    root?: Element | Document | null
    /**
     * Referência reativa para o root (útil quando depende de refs do Vue).
     */
    rootRef?: Ref<Element | null>
    rootMargin?: string
    threshold?: number | number[]
}

export interface UseIntersectionReturn {
    target: Ref<HTMLElement | null>
    isVisible: Ref<boolean>
}

/**
 * Composable para observar quando um elemento entra no viewport e liberar o carregamento lazy.
 */
export function useIntersection(options: UseIntersectionOptions = {}): UseIntersectionReturn {
    const target = ref<HTMLElement | null>(null)
    const isVisible = ref(false)
    let observer: IntersectionObserver | null = null

    const disconnect = () => {
        if (observer) {
            observer.disconnect()
            observer = null
        }
    }

    const handleEntries = (entries: IntersectionObserverEntry[]) => {
        entries.forEach(entry => {
            if (entry.target !== target.value) return

            if (entry.isIntersecting) {
                isVisible.value = true
                if (options.once !== false) {
                    disconnect()
                }
            } else if (options.once === false) {
                isVisible.value = false
            }
        })
    }

    const initObserver = () => {
        if (!target.value || observer) return

        observer = new IntersectionObserver(handleEntries, {
            root: options.rootRef?.value ?? options.root ?? null,
            rootMargin: options.rootMargin ?? '0px',
            threshold: options.threshold ?? 0.1,
        })

        observer.observe(target.value)
    }

    watch(
        () => target.value,
        () => {
            disconnect()
            initObserver()
        }
    )

    watch(
        () => options.rootRef?.value,
        () => {
            disconnect()
            initObserver()
        }
    )

    onMounted(initObserver)
    onUnmounted(disconnect)

    return {
        target,
        isVisible,
    }
}

