<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { deleteContact, listContacts } from '../api/contactsApi'
import type { ContactListItem } from '../api/contactTypes'

const route = useRoute()
const router = useRouter()

const pageSize = 20
const contacts = ref<readonly ContactListItem[]>([])
const totalCount = ref(0)
const isLoading = ref(false)
const isDeleting = ref(false)
const errorMessage = ref<string | null>(null)
const deleteError = ref<string | null>(null)
const contactPendingDelete = ref<ContactListItem | null>(null)

const page = computed(() => {
  const value = Number(route.query.page ?? 1)
  return Number.isInteger(value) && value > 0 ? value : 1
})

const search = computed({
  get: () => String(route.query.search ?? ''),
  set: value => {
    void router.replace({
      query: {
        ...route.query,
        search: value || undefined,
        page: undefined,
      },
    })
  },
})

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize)))
const hasPreviousPage = computed(() => page.value > 1)
const hasNextPage = computed(() => page.value < totalPages.value)

watch(
  () => [route.query.search, route.query.page],
  () => {
    void loadContacts()
  },
  { immediate: true },
)

async function loadContacts(): Promise<void> {
  isLoading.value = true
  errorMessage.value = null

  try {
    const result = await listContacts({
      search: search.value,
      page: page.value,
      pageSize,
    })

    contacts.value = result.items
    totalCount.value = result.totalCount
  } catch {
    contacts.value = []
    totalCount.value = 0
    errorMessage.value = 'Contacts could not be loaded. Check the API connection and try again.'
  } finally {
    isLoading.value = false
  }
}

function setPage(nextPage: number): void {
  void router.replace({
    query: {
      ...route.query,
      page: nextPage === 1 ? undefined : String(nextPage),
    },
  })
}

function requestDelete(contact: ContactListItem): void {
  deleteError.value = null
  contactPendingDelete.value = contact
}

function cancelDelete(): void {
  if (isDeleting.value) {
    return
  }

  deleteError.value = null
  contactPendingDelete.value = null
}

async function confirmDelete(): Promise<void> {
  if (!contactPendingDelete.value) {
    return
  }

  isDeleting.value = true
  deleteError.value = null

  try {
    await deleteContact(contactPendingDelete.value.id)
    contactPendingDelete.value = null

    if (contacts.value.length === 1 && page.value > 1) {
      setPage(page.value - 1)
      return
    }

    await loadContacts()
  } catch {
    deleteError.value = 'Contact could not be deleted. Check the API connection and try again.'
  } finally {
    isDeleting.value = false
  }
}

function formatPhone(contact: ContactListItem): string {
  const phone = contact.phone
  return `${phone.countryCode} (${phone.areaCode}) ${phone.number}`
}
</script>

<template>
  <section class="page-heading" aria-labelledby="contacts-title">
    <div>
      <h1 id="contacts-title">Contacts</h1>
      <p>Find, update, and manage stored contact details.</p>
    </div>
    <RouterLink class="button primary" :to="{ name: 'contacts-create' }">
      Create Contact
    </RouterLink>
  </section>

  <section class="toolbar" aria-label="Contact filters">
    <label class="field compact">
      <span>Search</span>
      <input
        v-model="search"
        name="contact-search"
        type="search"
        autocomplete="off"
        placeholder="Name, email, or phone…"
      />
    </label>
  </section>

  <section v-if="errorMessage" class="status-message error" aria-live="polite">
    {{ errorMessage }}
  </section>

  <section v-else-if="isLoading" class="status-message" aria-live="polite">
    Loading…
  </section>

  <section v-else-if="contacts.length === 0" class="empty-state" aria-live="polite">
    <h2>No Contacts Yet</h2>
    <p v-if="search">No contacts match the current search.</p>
    <p v-else>Create the first contact to start building the phonebook.</p>
  </section>

  <section v-else class="contacts-region" aria-labelledby="contacts-count">
    <div class="list-summary">
      <p id="contacts-count">
        {{ totalCount }} contacts
      </p>
      <p>Page {{ page }} of {{ totalPages }}</p>
    </div>

    <ul class="contact-list" aria-label="Contacts">
      <li v-for="contact in contacts" :key="contact.id" class="contact-row">
        <div class="avatar" aria-hidden="true">
          {{ contact.name.slice(0, 1).toUpperCase() }}
        </div>

        <div class="contact-main">
          <h2>{{ contact.name }}</h2>
          <p>{{ formatPhone(contact) }}</p>
          <p v-if="contact.email">{{ contact.email }}</p>
          <p v-else class="muted">No email</p>
        </div>

        <div class="row-actions" aria-label="Contact actions">
          <RouterLink
            class="icon-action"
            :to="{ name: 'contacts-edit', params: { id: contact.id } }"
            :aria-label="`Edit ${contact.name}`"
          >
            <span aria-hidden="true">✎</span>
          </RouterLink>
          <button
            class="icon-action danger"
            type="button"
            :aria-label="`Delete ${contact.name}`"
            @click="requestDelete(contact)"
          >
            <span aria-hidden="true">×</span>
          </button>
        </div>
      </li>
    </ul>

    <nav class="pagination" aria-label="Contacts pagination">
      <button
        type="button"
        :disabled="!hasPreviousPage"
        @click="setPage(page - 1)"
      >
        Previous
      </button>
      <span>Page {{ page }}</span>
      <button
        type="button"
        :disabled="!hasNextPage"
        @click="setPage(page + 1)"
      >
        Next
      </button>
    </nav>
  </section>

  <div v-if="contactPendingDelete" class="modal-backdrop" role="presentation">
    <section
      class="confirm-dialog"
      role="dialog"
      aria-modal="true"
      aria-labelledby="delete-contact-title"
      aria-describedby="delete-contact-description"
    >
      <h2 id="delete-contact-title">Delete Contact</h2>
      <p id="delete-contact-description">
        Delete {{ contactPendingDelete.name }} from the phonebook?
      </p>
      <p v-if="deleteError" class="field-error" aria-live="polite">
        {{ deleteError }}
      </p>
      <div class="dialog-actions">
        <button
          type="button"
          class="button secondary"
          :disabled="isDeleting"
          @click="cancelDelete"
        >
          Cancel
        </button>
        <button
          type="button"
          class="button danger"
          :disabled="isDeleting"
          @click="confirmDelete"
        >
          {{ isDeleting ? 'Deleting…' : 'Delete Contact' }}
        </button>
      </div>
    </section>
  </div>
</template>
