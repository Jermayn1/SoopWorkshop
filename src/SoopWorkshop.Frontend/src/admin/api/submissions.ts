import type { components } from '../../api/schema'
import { request, type ApiResult } from '../../api/client'
import type { SubmissionStatus } from '../../api/types'
import { toNumber } from '../../api/mappers'
import { mapResult } from './catalog'

type Schemas = components['schemas']

// Eine Zeile der Uebersicht, mit gefuellten Pflichtfeldern.
//
// Wie ueberall zwischen schema.d.ts und der Oberflaeche: .NET gibt im
// OpenAPI-Dokument kein "required" aus, dort ist also jedes Feld optional.
// Ohne diese Umsetzung stuende in der Tabelle an jeder Stelle ein ?? ''.
export type SubmissionListItem = {
  id: string
  taskItemId: string
  taskTitle: string
  categoryName: string
  submittedAt: string
  status: SubmissionStatus
  errorMessage: string
  // Bleibt null, solange keine Auswertung vorliegt. NICHT auf 0 setzen: 0 waere
  // eine Aussage ueber die Loesung, null sagt nur "noch nicht bewertet".
  totalScore: number | null
  maxScore: number | null
}

export type SubmissionPage = {
  items: SubmissionListItem[]
  total: number
  skip: number
  take: number
}

function toListItem(dto: Schemas['SubmissionListItemDto']): SubmissionListItem {
  return {
    id: dto.id ?? '',
    taskItemId: dto.taskItemId ?? '',
    taskTitle: dto.taskTitle ?? '',
    categoryName: dto.categoryName ?? '',
    submittedAt: dto.submittedAt ?? '',
    status: dto.status ?? 'Pending',
    errorMessage: dto.errorMessage ?? '',
    // Nicht toNumber mit Rueckfall 0: hier ist "nicht da" eine eigene
    // Aussage. Ein Rueckfall auf 0 machte aus "noch nicht bewertet" ein
    // "null Punkte erreicht".
    totalScore: dto.totalScore === undefined || dto.totalScore === null
      ? null
      : toNumber(dto.totalScore),
    maxScore: dto.maxScore === undefined || dto.maxScore === null
      ? null
      : toNumber(dto.maxScore),
  }
}

export type SubmissionFilter = {
  taskItemId?: string
  status?: SubmissionStatus
  skip?: number
  take?: number
}

export async function fetchSubmissions(
  filter: SubmissionFilter = {},
  signal?: AbortSignal,
): Promise<ApiResult<SubmissionPage>> {
  const query = new URLSearchParams()

  if (filter.taskItemId) query.set('taskItemId', filter.taskItemId)
  if (filter.status) query.set('status', filter.status)
  query.set('skip', String(filter.skip ?? 0))
  query.set('take', String(filter.take ?? 25))

  const result = await request<Schemas['SubmissionPageDto']>(
    `/api/admin/submissions?${query.toString()}`,
    { signal },
  )

  return mapResult(result, (dto) => ({
    items: (dto.items ?? []).map(toListItem),
    total: toNumber(dto.total),
    skip: toNumber(dto.skip),
    take: toNumber(dto.take, 25),
  }))
}
