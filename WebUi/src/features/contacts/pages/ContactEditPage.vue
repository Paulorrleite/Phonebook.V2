<script setup lang="ts">
import { ref, watch } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import ContactForm from '../components/ContactForm.vue'
import { getContactForEdit, updateContact } from '../api/contactsApi'
import type { ContactEdit, ContactRequest } from '../api/contactTypes'
import { isApiError } from '../../../shared/api/http'

const props = defineProps<{
  readonly id: string
}>()

const router = useRouter()
const contact = ref<ContactEdit | null>(null)
const isLoading = ref(false)
const isSubmitting = ref(false)
const formError = ref<string | null>(null)
const serverErrors = ref<Record<string, string>>({})

watch(
  () => props.id,
  async id => {
    await loadContact(id)
  },
  { immediate: true },
)

async function loadContact(id: string): Promise<void> {
  isLoading.value = true
  formError.value = null
  serverErrors.value = {}

  try {
    contact.value = await getContactForEdit(id)
  } catch (error) {
    contact.value = null
    formError.value = isApiError(error) && error.status === 404
      ? 'Contact was not found.'
      : 'Contact could not be loaded. Check the API connection and try again.'
  } finally {
    isLoading.value = false
  }
}

async function handleSubmit(request: ContactRequest): Promise<void> {
  isSubmitting.value = true
  formError.value = null
  serverErrors.value = {}

  try {
    await updateContact(props.id, request)
    await router.push({ name: 'contacts-list' })
  } catch (error) {
    if (isApiError(error)) {
      serverErrors.value = toFormErrors(error.errors)
      formError.value = error.status === 404 ? 'Contact was not found.' : error.title
      return
    }

    formError.value = 'Contact could not be saved. Check the API connection and try again.'
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
  <section class="page-heading" aria-labelledby="edit-contact-title">
    <div>
      <h1 id="edit-contact-title">Edit Contact</h1>
      <p>Update contact details, phone numbers, address, and photo.</p>
    </div>
    <RouterLink class="button secondary" :to="{ name: 'contacts-list' }">Back to List</RouterLink>
  </section>

  <section v-if="isLoading" class="status-message" aria-live="polite">
    Loading…
  </section>

  <section v-if="formError" class="status-message error" aria-live="polite">
    {{ formError }}
  </section>

  <ContactForm
    v-if="contact"
    :initial-value="contact"
    :server-errors="serverErrors"
    :is-submitting="isSubmitting"
    @submit="handleSubmit"
  />
</template>
