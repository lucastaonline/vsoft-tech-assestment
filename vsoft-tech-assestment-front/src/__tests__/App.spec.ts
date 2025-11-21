import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import App from '../App.vue'

describe('App', () => {
  it('renderiza layout base, router e cookie consent', () => {
    const wrapper = mount(App, {
      global: {
        stubs: {
          BaseLayout: {
            template: '<div data-testid="base-layout"><slot /></div>',
          },
          CookieConsent: {
            template: '<div data-testid="cookie-consent" />',
          },
          'router-view': {
            template: '<div data-testid="router-view" />',
          },
        },
      },
    })

    expect(wrapper.get('[data-testid="base-layout"]')).toBeTruthy()
    expect(wrapper.get('[data-testid="router-view"]')).toBeTruthy()
    expect(wrapper.get('[data-testid="cookie-consent"]')).toBeTruthy()
  })
})
