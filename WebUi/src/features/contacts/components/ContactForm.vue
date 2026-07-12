<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import type { ContactEdit, ContactRequest, PhoneType, PhotoSummary } from '../api/contactTypes'

const maxPhoneCount = 10
const maxPhotoSize = 750 * 1024
const phoneTypes: readonly PhoneType[] = ['Mobile', 'Residential', 'Commercial']

interface PhoneForm {
  countryCode: string
  areaCode: string
  number: string
  type: PhoneType
  isFavorite: boolean
}

interface AddressForm {
  country: string
  state: string
  city: string
  neighborhood: string
  postalCode: string
}

interface PhotoForm {
  content: string
  contentType: string
  fileName: string
  size: number
}

interface ContactFormState {
  firstName: string
  lastName: string
  email: string
  phoneNumbers: PhoneForm[]
  address: AddressForm
  photo: PhotoForm | null
}

const emit = defineEmits<{
  submit: [request: ContactRequest]
}>()

const props = withDefaults(defineProps<{
  readonly initialValue?: ContactEdit | null
  readonly serverErrors?: Readonly<Record<string, string>>
  readonly isSubmitting?: boolean
}>(), {
  initialValue: null,
  serverErrors: () => ({}),
  isSubmitting: false,
})

const form = reactive<ContactFormState>({
  firstName: '',
  lastName: '',
  email: '',
  phoneNumbers: [createPhone()],
  address: {
    country: '',
    state: '',
    city: '',
    neighborhood: '',
    postalCode: '',
  },
  photo: null,
})

const errors = ref<Record<string, string>>({})
const existingPhoto = ref<PhotoSummary | null>(null)
const removeExistingPhoto = ref(false)
const firstNameInput = ref<HTMLInputElement | null>(null)
const firstPhoneInput = ref<HTMLInputElement | null>(null)
const photoInput = ref<HTMLInputElement | null>(null)

const canAddPhone = computed(() => form.phoneNumbers.length < maxPhoneCount)
const allErrors = computed(() => ({ ...props.serverErrors, ...errors.value }))
const hasAddressInput = computed(() => {
  return Object.values(form.address).some(value => value.trim().length > 0)
})

watch(
  () => props.initialValue,
  contact => {
    hydrateForm(contact)
  },
  { immediate: true },
)

function createPhone(): PhoneForm {
  return {
    countryCode: '+55',
    areaCode: '',
    number: '',
    type: 'Mobile',
    isFavorite: false,
  }
}

function emptyAddress(): AddressForm {
  return {
    country: '',
    state: '',
    city: '',
    neighborhood: '',
    postalCode: '',
  }
}

function hydrateForm(contact: ContactEdit | null | undefined): void {
  errors.value = {}
  removeExistingPhoto.value = false
  form.photo = null

  if (photoInput.value) {
    photoInput.value.value = ''
  }

  if (!contact) {
    form.firstName = ''
    form.lastName = ''
    form.email = ''
    form.phoneNumbers.splice(0, form.phoneNumbers.length, createPhone())
    Object.assign(form.address, emptyAddress())
    existingPhoto.value = null
    return
  }

  form.firstName = contact.firstName
  form.lastName = contact.lastName ?? ''
  form.email = contact.email ?? ''
  form.phoneNumbers.splice(
    0,
    form.phoneNumbers.length,
    ...contact.phoneNumbers.map(phone => ({
      countryCode: phone.countryCode,
      areaCode: phone.areaCode,
      number: phone.number,
      type: phone.type,
      isFavorite: phone.isFavorite,
    })),
  )

  if (form.phoneNumbers.length === 0) {
    form.phoneNumbers.push(createPhone())
  }

  Object.assign(form.address, contact.address
    ? {
        country: contact.address.country,
        state: contact.address.state,
        city: contact.address.city,
        neighborhood: contact.address.neighborhood,
        postalCode: contact.address.postalCode,
      }
    : emptyAddress())
  existingPhoto.value = contact.photo
}

function addPhone(): void {
  if (!canAddPhone.value) {
    return
  }

  form.phoneNumbers.push(createPhone())
}

function removePhone(index: number): void {
  if (form.phoneNumbers.length === 1) {
    errors.value.phoneNumbers = 'Keep at least 1 phone number.'
    return
  }

  form.phoneNumbers.splice(index, 1)
}

function setFavorite(index: number): void {
  form.phoneNumbers.forEach((phone, phoneIndex) => {
    phone.isFavorite = phoneIndex === index
  })
}

async function onPhotoSelected(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  delete errors.value.photo

  if (!file) {
    form.photo = null
    return
  }

  if (file.size > maxPhotoSize) {
    form.photo = null
    errors.value.photo = 'Photo must be 750 KB or smaller.'
    input.value = ''
    return
  }

  form.photo = {
    content: await fileToBase64(file),
    contentType: file.type || 'application/octet-stream',
    fileName: file.name,
    size: file.size,
  }
  removeExistingPhoto.value = false
}

function removePhoto(): void {
  form.photo = null
  delete errors.value.photo

  if (existingPhoto.value) {
    existingPhoto.value = null
    removeExistingPhoto.value = true
  }

  if (photoInput.value) {
    photoInput.value.value = ''
  }
}

function submitForm(): void {
  const nextErrors = validate()
  errors.value = nextErrors

  if (Object.keys(nextErrors).length > 0) {
    focusFirstError(nextErrors)
    return
  }

  emit('submit', toRequest())
}

function validate(): Record<string, string> {
  const nextErrors: Record<string, string> = {}

  if (!form.firstName.trim()) {
    nextErrors.firstName = 'Enter a first name.'
  }

  if (form.email.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) {
    nextErrors.email = 'Enter a valid email address.'
  }

  if (form.phoneNumbers.length === 0) {
    nextErrors.phoneNumbers = 'Add at least 1 phone number.'
  }

  if (form.phoneNumbers.length > maxPhoneCount) {
    nextErrors.phoneNumbers = 'Use no more than 10 phone numbers.'
  }

  form.phoneNumbers.forEach((phone, index) => {
    const label = `phone-${index}`

    if (!phone.countryCode.trim() || !phone.areaCode.trim() || !phone.number.trim()) {
      nextErrors[label] = 'Complete country code, area code, and phone number.'
    }
  })

  if (form.phoneNumbers.filter(phone => phone.isFavorite).length > 1) {
    nextErrors.phoneNumbers = 'Choose only 1 favorite phone.'
  }

  if (errors.value.photo) {
    nextErrors.photo = errors.value.photo
  }

  if (hasAddressInput.value) {
    for (const [field, value] of Object.entries(form.address)) {
      if (!value.trim()) {
        nextErrors[`address.${field}`] = 'Complete every address field or leave address blank.'
      }
    }
  }

  return nextErrors
}

function focusFirstError(nextErrors: Record<string, string>): void {
  if (nextErrors.firstName) {
    firstNameInput.value?.focus()
    return
  }

  if (nextErrors.phoneNumbers || Object.keys(nextErrors).some(key => key.startsWith('phone-'))) {
    firstPhoneInput.value?.focus()
    return
  }

  if (nextErrors.photo) {
    photoInput.value?.focus()
  }
}

function toRequest(): ContactRequest {
  return {
    firstName: form.firstName.trim(),
    lastName: normalizeOptional(form.lastName),
    email: normalizeOptional(form.email),
    phoneNumbers: form.phoneNumbers.map(phone => ({
      countryCode: phone.countryCode.trim(),
      areaCode: phone.areaCode.trim(),
      number: phone.number.trim(),
      type: phone.type,
      isFavorite: phone.isFavorite,
    })),
    address: hasAddressInput.value
      ? {
          country: form.address.country.trim(),
          state: form.address.state.trim(),
          city: form.address.city.trim(),
          neighborhood: form.address.neighborhood.trim(),
          postalCode: form.address.postalCode.trim(),
        }
      : null,
    photo: form.photo,
    removePhoto: removeExistingPhoto.value,
  }
}

function normalizeOptional(value: string): string | null {
  const normalized = value.trim()
  return normalized.length > 0 ? normalized : null
}

function fileToBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => {
      const result = String(reader.result)
      resolve(result.includes(',') ? result.split(',')[1] : result)
    }
    reader.onerror = () => reject(reader.error)
    reader.readAsDataURL(file)
  })
}
</script>

<template>
  <form class="contact-form" novalidate @submit.prevent="submitForm">
    <section class="form-section" aria-labelledby="identity-section">
      <h2 id="identity-section">Identity</h2>
      <div class="form-grid">
        <label class="field">
          <span>First Name</span>
          <input
            ref="firstNameInput"
            v-model="form.firstName"
            name="firstName"
            autocomplete="given-name"
            :aria-invalid="Boolean(allErrors.firstName)"
            aria-describedby="first-name-error"
          />
          <span v-if="allErrors.firstName" id="first-name-error" class="field-error">
            {{ allErrors.firstName }}
          </span>
        </label>

        <label class="field">
          <span>Last Name</span>
          <input v-model="form.lastName" name="lastName" autocomplete="family-name" />
        </label>

        <label class="field">
          <span>Email</span>
          <input
            v-model="form.email"
            name="email"
            type="email"
            autocomplete="email"
            spellcheck="false"
            :aria-invalid="Boolean(allErrors.email)"
            aria-describedby="email-error"
          />
          <span v-if="allErrors.email" id="email-error" class="field-error">
            {{ allErrors.email }}
          </span>
        </label>
      </div>
    </section>

    <section class="form-section" aria-labelledby="phones-section">
      <div class="section-heading">
        <h2 id="phones-section">Phone Numbers</h2>
        <button type="button" class="button secondary" :disabled="!canAddPhone" @click="addPhone">
          Add Phone
        </button>
      </div>

      <p v-if="allErrors.phoneNumbers" class="field-error" aria-live="polite">
        {{ allErrors.phoneNumbers }}
      </p>

      <div class="phone-list">
        <fieldset v-for="(phone, index) in form.phoneNumbers" :key="index" class="phone-row">
          <legend>Phone {{ index + 1 }}</legend>

          <label class="field">
            <span>Country Code</span>
            <input
              :ref="index === 0 ? (element) => { firstPhoneInput = element as HTMLInputElement | null } : undefined"
              v-model="phone.countryCode"
              name="countryCode"
              type="tel"
              inputmode="tel"
              autocomplete="tel-country-code"
            />
          </label>

          <label class="field">
            <span>Area Code</span>
            <input v-model="phone.areaCode" name="areaCode" type="tel" inputmode="tel" autocomplete="tel-area-code" />
          </label>

          <label class="field">
            <span>Number</span>
            <input v-model="phone.number" name="phoneNumber" type="tel" inputmode="tel" autocomplete="tel-national" />
          </label>

          <label class="field">
            <span>Type</span>
            <select v-model="phone.type" name="phoneType">
              <option v-for="type in phoneTypes" :key="type" :value="type">
                {{ type }}
              </option>
            </select>
          </label>

          <label class="check-field">
            <input
              type="checkbox"
              :checked="phone.isFavorite"
              name="favoritePhone"
              @change="setFavorite(index)"
            />
            <span>Favorite</span>
          </label>

          <button
            type="button"
            class="icon-action danger"
            :aria-label="`Remove phone ${index + 1}`"
            @click="removePhone(index)"
          >
            <span aria-hidden="true">×</span>
          </button>

          <p v-if="allErrors[`phone-${index}`]" class="field-error phone-error">
            {{ allErrors[`phone-${index}`] }}
          </p>
        </fieldset>
      </div>
    </section>

    <section class="form-section" aria-labelledby="address-section">
      <h2 id="address-section">Address</h2>
      <div class="form-grid">
        <label class="field">
          <span>Country</span>
          <input v-model="form.address.country" name="country" autocomplete="country-name" />
        </label>
        <label class="field">
          <span>State</span>
          <input v-model="form.address.state" name="state" autocomplete="address-level1" />
        </label>
        <label class="field">
          <span>City</span>
          <input v-model="form.address.city" name="city" autocomplete="address-level2" />
        </label>
        <label class="field">
          <span>Neighborhood</span>
          <input v-model="form.address.neighborhood" name="neighborhood" autocomplete="address-line2" />
        </label>
        <label class="field">
          <span>ZIP Code</span>
          <input v-model="form.address.postalCode" name="postalCode" autocomplete="postal-code" />
        </label>
      </div>
      <p v-if="Object.keys(allErrors).some(key => key.startsWith('address.'))" class="field-error">
        Complete every address field or leave address blank.
      </p>
    </section>

    <section class="form-section" aria-labelledby="photo-section">
      <h2 id="photo-section">Photo</h2>
      <label class="field">
        <span>Upload Photo</span>
        <input
          ref="photoInput"
          name="photo"
          type="file"
          accept="image/*"
          :aria-invalid="Boolean(allErrors.photo)"
          aria-describedby="photo-error"
          @change="onPhotoSelected"
        />
      </label>
      <p v-if="form.photo" class="muted">
        {{ form.photo.fileName }} · {{ Math.ceil(form.photo.size / 1024) }} KB
        <button type="button" class="text-button" @click="removePhoto">Remove Photo</button>
      </p>
      <p v-else-if="existingPhoto" class="muted">
        {{ existingPhoto.fileName }} · {{ Math.ceil(existingPhoto.size / 1024) }} KB
        <button type="button" class="text-button" @click="removePhoto">Remove Photo</button>
      </p>
      <p v-if="allErrors.photo" id="photo-error" class="field-error">
        {{ allErrors.photo }}
      </p>
    </section>

    <div class="form-actions">
      <button type="submit" class="button primary" :disabled="isSubmitting">
        {{ isSubmitting ? 'Saving…' : 'Save Contact' }}
      </button>
    </div>
  </form>
</template>
