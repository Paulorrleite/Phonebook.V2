export type PhoneType = 'Mobile' | 'Residential' | 'Commercial'

export interface PhoneNumberInput {
  readonly countryCode: string
  readonly areaCode: string
  readonly number: string
  readonly type: PhoneType
  readonly isFavorite: boolean
}

export interface AddressInput {
  readonly country: string | null
  readonly state: string | null
  readonly city: string | null
  readonly neighborhood: string | null
  readonly postalCode: string | null
}

export interface PhotoInput {
  readonly content: string
  readonly contentType: string
  readonly fileName: string
  readonly size: number
}

export interface ContactRequest {
  readonly firstName: string
  readonly lastName: string | null
  readonly email: string | null
  readonly phoneNumbers: readonly PhoneNumberInput[]
  readonly address: AddressInput | null
  readonly photo: PhotoInput | null
  readonly removePhoto: boolean
}

export interface CreateContactResponse {
  readonly id: string
}

export interface PagedResult<TItem> {
  readonly items: readonly TItem[]
  readonly page: number
  readonly pageSize: number
  readonly totalCount: number
}

export interface PhotoSummary {
  readonly id: string
  readonly contentType: string
  readonly fileName: string
  readonly size: number
}

export interface DisplayPhone {
  readonly countryCode: string
  readonly areaCode: string
  readonly number: string
  readonly type: PhoneType
  readonly isFavorite: boolean
}

export interface ContactListItem {
  readonly id: string
  readonly photo: PhotoSummary | null
  readonly name: string
  readonly phone: DisplayPhone
  readonly email: string | null
}

export interface PhoneNumberEdit extends PhoneNumberInput {
  readonly id: string
}

export interface AddressEdit {
  readonly id: string
  readonly country: string
  readonly state: string
  readonly city: string
  readonly neighborhood: string
  readonly postalCode: string
}

export interface ContactEdit {
  readonly id: string
  readonly firstName: string
  readonly lastName: string | null
  readonly email: string | null
  readonly phoneNumbers: readonly PhoneNumberEdit[]
  readonly address: AddressEdit | null
  readonly photo: PhotoSummary | null
}
