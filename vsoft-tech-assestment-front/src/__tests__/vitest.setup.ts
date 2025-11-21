import { vi } from 'vitest'

class IntersectionObserverMock implements IntersectionObserver {
    readonly root: Element | Document | null
    readonly rootMargin: string
    readonly thresholds: ReadonlyArray<number>

    constructor(private callback: IntersectionObserverCallback, options: IntersectionObserverInit = {}) {
        this.root = (options.root as Element | Document | null) ?? null
        this.rootMargin = options.rootMargin ?? '0px'
        const threshold = options.threshold ?? 0
        this.thresholds = Array.isArray(threshold) ? threshold : [threshold]
    }

    disconnect = vi.fn()
    observe = vi.fn((target: Element) => {
        this.callback(
            [
                {
                    boundingClientRect: target.getBoundingClientRect(),
                    intersectionRatio: 1,
                    intersectionRect: target.getBoundingClientRect(),
                    isIntersecting: true,
                    rootBounds: null,
                    target,
                    time: Date.now(),
                } as IntersectionObserverEntry,
            ],
            this,
        )
    })
    takeRecords = vi.fn(() => [] as IntersectionObserverEntry[])
    unobserve = vi.fn()

    // Not part of official interface but used by some typings
    rootBounds: DOMRectReadOnly | null = null
}

class ResizeObserverMock implements ResizeObserver {
    constructor(private callback: ResizeObserverCallback) { }

    observe = vi.fn((target: Element) => {
        this.callback([{ target, contentRect: target.getBoundingClientRect(), borderBoxSize: [], contentBoxSize: [], devicePixelContentBoxSize: [] }], this)
    })
    unobserve = vi.fn()
    disconnect = vi.fn()
}

if (typeof window !== 'undefined') {
    if (!('IntersectionObserver' in window)) {
        // @ts-expect-error - assigning to global
        window.IntersectionObserver = IntersectionObserverMock
    }

    if (!('ResizeObserver' in window)) {
        // @ts-expect-error - assigning to global
        window.ResizeObserver = ResizeObserverMock
    }
}

