<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import ContactForm from '../components/ContactForm.vue'
import { createContact } from '../api/contactsApi'
import type { ContactRequest } from '../api/contactTypes'
import { isApiError } from '../../../shared/api/http'

const router = useRouter()
const isSubmitting = ref(false)
const formError = ref<string | null>(null)
const serverErrors = ref<Record<string, string>>({})

async function handleSubmit(request: ContactRequest): Promise<void> {
  isSubmitting.value = true
  formError.value = null
  serverErrors.value = {}

  try {
    await createContact(request)
    await router.push({ name: 'contacts-list' })
  } catch (error) {
    if (isApiError(error)) {
      serverErrors.value = toFormErrors(error.errors)
      formError.value = error.title
      return
    }

    formError.value = 'Contact could not be created. Check the API connection and try again.'
  } finally {
    isSubmitting.value = false
  }
}

function toFormErrors(errors: Readonly<Record<string, readonly string[]>>): Record<string, string> {
  return Object.fromEntries(
    Object.entries(errors).map(([key, messages]) => [
      normalizeErrorKey(key),
      messages[0] ?? 'Check this field.',
    ]),
  )
}

function normalizeErrorKey(key: string): string {
  return key.length === 0 ? 'request' : `${key[0].toLowerCase()}${key.slice(1)}`
}
</script>

<template>
  <section class="page-heading" aria-labelledby="create-contact-title">
    <div>
      <h1 id="create-contact-title">Create Contact</h1>
      <p>Add a contact with at least one phone number.</p>
    </div>
  </section>

  <section v-if="formError" class="status-message error" aria-live="polite">
    {{ formError }}
  </section>

  <ContactForm
    :server-errors="serverErrors"
    :is-submitting="isSubmitting"
    @submit="handleSubmit"
  />
</template>
