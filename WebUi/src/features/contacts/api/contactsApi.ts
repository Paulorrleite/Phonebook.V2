import { requestJson } from '../../../shared/api/http'
import type {
  ContactEdit,
  ContactListItem,
  ContactRequest,
  CreateContactResponse,
  PagedResult,
} from './contactTypes'

export interface ListContactsParams {
  readonly search?: string
  readonly page?: number
  readonly pageSize?: number
}

export async function listContacts(params: ListContactsParams): Promise<PagedResult<ContactListItem>> {
  const searchParams = new URLSearchParams()

  if (params.search) {
    searchParams.set('search', params.search)
  }

  if (params.page) {
    searchParams.set('page', String(params.page))
  }

  if (params.pageSize) {
    searchParams.set('pageSize', String(params.pageSize))
  }

  const query = searchParams.toString()
  return await requestJson<PagedResult<ContactListItem>>(`/api/contacts${query ? `?${query}` : ''}`)
}

export async function getContactForEdit(id: string): Promise<ContactEdit> {
  return await requestJson<ContactEdit>(`/api/contacts/${id}`)
}

export async function createContact(request: ContactRequest): Promise<CreateContactResponse> {
  return await requestJson<CreateContactResponse>('/api/contacts', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export async function updateContact(id: string, request: ContactRequest): Promise<void> {
  await requestJson<void>(`/api/contacts/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export async function deleteContact(id: string): Promise<void> {
  await requestJson<void>(`/api/contacts/${id}`, {
    method: 'DELETE',
  })
}
