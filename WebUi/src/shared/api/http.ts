export interface ApiError {
  readonly status: number
  readonly title: string
  readonly errors: Readonly<Record<string, readonly string[]>>
}

export function isApiError(error: unknown): error is ApiError {
  return typeof error === 'object' &&
    error !== null &&
    'status' in error &&
    'errors' in error
}

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7069'

export async function requestJson<TResponse>(
  path: string,
  init?: RequestInit,
): Promise<TResponse> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...init?.headers,
    },
  })

  if (!response.ok) {
    throw await toApiError(response)
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  return await response.json() as TResponse
}

async function toApiError(response: Response): Promise<ApiError> {
  const fallback: ApiError = {
    status: response.status,
    title: response.statusText || 'Request failed',
    errors: {},
  }

  try {
    const problem = await response.json() as Partial<ApiError>
    return {
      status: response.status,
      title: problem.title ?? fallback.title,
      errors: problem.errors ?? {},
    }
  } catch {
    return fallback
  }
}
