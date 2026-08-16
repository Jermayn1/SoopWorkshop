import type { components } from '../../api/schema'
import { jsonRequest, request, type ApiResult } from '../../api/client'
import { toCategory, toTask } from '../../api/mappers'
import type { Category, Task } from '../../api/types'

type Schemas = components['schemas']

// Gleicher Helfer wie in api/endpoints.ts: hebt eine Umsetzung in ApiResult
// hinein, ohne die Fehlerfaelle anzufassen. Die bleiben unterscheidbar.
function map<A, B>(result: ApiResult<A>, project: (value: A) => B): ApiResult<B> {
  return result.kind === 'ok' ? { kind: 'ok', value: project(result.value) } : result
}

// Anders als GET /api/categories liefert dieser Endpunkt auch die verborgenen
// Kategorien und Aufgaben — die Verwaltung muss ja gerade das sehen, was der
// Teilnehmer (noch) nicht sieht.
//
// Was hier NICHT mitkommt: Hints, erwartete Methoden, Testfaelle, JUnit-Dateien
// und Gewichte. Der Service fuellt die Aufgaben in der Kategorie nur mit den
// Skalarfeldern. Fuer eine Uebersicht reicht das; der Aufgaben-Editor holt sich
// den Rest ueber die eigenen Endpunkte.
export async function fetchAdminCategories(signal?: AbortSignal): Promise<ApiResult<Category[]>> {
  const result = await request<Schemas['TaskCategoryDto'][]>('/api/admin/categories', { signal })
  return map(result, (dtos) => dtos.map(toCategory).sort((a, b) => a.order - b.order))
}

export async function fetchAdminTask(id: string, signal?: AbortSignal): Promise<ApiResult<Task>> {
  const result = await request<Schemas['TaskItemDto']>(`/api/admin/tasks/${id}`, { signal })
  return map(result, toTask)
}

// Umschalten statt Setzen: der Endpunkt kennt nur PATCH ohne Rumpf und
// antwortet mit dem Zustand danach. Genau der wird uebernommen, statt ihn im
// Frontend zu erraten — beim Einschalten kann der Server ablehnen, wenn die
// Testdaten zum Auswertungsmodus fehlen.
export function toggleCategoryVisibility(
  id: string,
  signal?: AbortSignal,
): Promise<ApiResult<Schemas['VisibilityStateDto']>> {
  return jsonRequest<Schemas['VisibilityStateDto']>(
    `/api/admin/categories/${id}/visibility`,
    'PATCH',
    undefined,
    signal,
  )
}

export function toggleTaskVisibility(
  id: string,
  signal?: AbortSignal,
): Promise<ApiResult<Schemas['VisibilityStateDto']>> {
  return jsonRequest<Schemas['VisibilityStateDto']>(
    `/api/admin/tasks/${id}/visibility`,
    'PATCH',
    undefined,
    signal,
  )
}
