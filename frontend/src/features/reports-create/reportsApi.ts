import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import { toast } from 'sonner'
import { api } from '@/lib/api'
import { downloadBlob, fileNameFromDisposition } from '@/lib/download'
import { getErrorMessage } from '@/lib/errors'
import type { PagedResult } from '@/types/api'

/** Teléfono guardado: código de área + número (y parentesco en los de familiares). */
export interface SocialReportPhone {
  areaCodeId: string
  /** Código de área, p. ej. "0414". */
  areaCode: string
  number: string
  kinshipId: string | null
  kinship: string | null
}

/** Teléfono a guardar: código de la tabla maestra o uno nuevo escrito en "Otro" (lo mismo el parentesco). */
export interface SocialReportPhoneInput {
  areaCodeId: string | null
  areaCodeOther: string | null
  number: string
  kinshipId: string | null
  kinshipOther: string | null
}

/** Informe completo tal como lo devuelve la API (para editarlo). */
export interface SocialReport {
  id: string
  medicalRecordNumber: string
  /** yyyy-mm-dd */
  admissionDate: string
  religionId: string | null
  lastNames: string
  firstNames: string
  identityCard: string | null
  passportNumber: string | null
  birthPlace: string | null
  birthCountryId: string | null
  birthStateId: string | null
  birthMunicipalityId: string | null
  birthDate: string | null
  genderId: string | null
  raceId: string | null
  maritalStatusId: string | null
  educationLevelId: string | null
  professionId: string | null
  occupationId: string | null
  homeAddress: string | null
  phones: SocialReportPhone[]
  hasIvss: boolean | null
  hasIpasme: boolean | null
  insurancePolicy: string | null
  otherSocialSecurity: string | null
  referredBy: string | null
  workplace: string | null
  workAddress: string | null
  workPhones: SocialReportPhone[]
  workSchedule: string | null
  isPediatric: boolean
  guardianFullName: string | null
  guardianIdentityCard: string | null
  guardianKinshipId: string | null
  contactAddress: string | null
  contactFullName: string | null
  contactPhones: SocialReportPhone[]
  medicalSocialArea: string | null
  psychosocialArea: string | null
  socioeconomicArea: string | null
  physicalEnvironmentArea: string | null
  clinicalDiagnosis: string | null
  referralServices: string | null
  observations: string | null
  /** Trabajadora vinculada al usuario que creó el informe (puede faltar si lo creó un administrador). */
  socialWorkerId: string | null
  socialWorkerName: string | null
  createdAt: string
  createdBy: string
  updatedAt: string | null
  updatedBy: string | null
}

/** Datos que se envían al crear o editar. Los "…Other" son valores nuevos para la tabla maestra. */
export type SocialReportPayload = Omit<
  SocialReport,
  | 'id'
  | 'socialWorkerId'
  | 'socialWorkerName'
  | 'createdAt'
  | 'createdBy'
  | 'updatedAt'
  | 'updatedBy'
  | 'phones'
  | 'workPhones'
  | 'contactPhones'
> & {
  religionOther: string | null
  raceOther: string | null
  maritalStatusOther: string | null
  educationLevelOther: string | null
  professionOther: string | null
  occupationOther: string | null
  guardianKinshipOther: string | null
  phones: SocialReportPhoneInput[]
  workPhones: SocialReportPhoneInput[]
  contactPhones: SocialReportPhoneInput[]
}

export interface SocialReportListItem {
  id: string
  medicalRecordNumber: string
  patientName: string
  identityCard: string | null
  admissionDate: string
  socialWorkerId: string | null
  socialWorkerName: string | null
  createdAt: string
  updatedAt: string | null
}

export interface SocialWorkerSummary {
  id: string
  fullName: string
  identityCard: string
  antsRegistrationNumber: string
  socialWorkersAssociation: string
}

export interface SocialReportContext {
  /** Trabajadora social vinculada al usuario: se asigna sola a los informes que crea. */
  socialWorker: SocialWorkerSummary | null
  /** Administrador: elige quién firma al descargar el PDF y ve todos los informes. */
  canChooseSocialWorker: boolean
}

export interface ReportListParams {
  search: string
  page: number
  pageSize: number
}

const BASE = '/social-reports'

export const reportsApi = {
  context: async () => (await api.get<SocialReportContext>(`${BASE}/context`)).data,
  get: async (id: string) => (await api.get<SocialReport>(`${BASE}/${id}`)).data,
  create: async (payload: SocialReportPayload) => (await api.post<SocialReport>(BASE, payload)).data,
  update: async (id: string, payload: SocialReportPayload) => (await api.put<SocialReport>(`${BASE}/${id}`, payload)).data,

  /**
   * Descarga el PDF (2 hojas Carta) con el nombre que propone la API. signerId: otra trabajadora
   * para la firma (solo la aplica la API si el usuario es administrador).
   */
  downloadPdf: async (id: string, signerId?: string) => {
    try {
      const response = await api.get<Blob>(`${BASE}/${id}/pdf`, {
        responseType: 'blob',
        params: signerId ? { socialWorkerId: signerId } : undefined,
      })
      const fileName = fileNameFromDisposition(response.headers['content-disposition'] as string | undefined, 'Entrevista.pdf')
      downloadBlob(response.data, fileName)
    } catch (error) {
      throw await withJsonProblem(error)
    }
  },
}

/** Descarga el PDF y avisa si falla. Devuelve si se descargó. */
export async function downloadReportPdf(id: string, signerId?: string, failurePrefix = 'No se pudo descargar el PDF.') {
  try {
    await reportsApi.downloadPdf(id, signerId)
    return true
  } catch (error) {
    toast.error(`${failurePrefix} ${getErrorMessage(error, '')}`.trim())
    return false
  }
}

/** Con responseType "blob" el error (ProblemDetails) también llega como Blob: se convierte a JSON. */
async function withJsonProblem(error: unknown) {
  if (isAxiosError(error) && error.response?.data instanceof Blob) {
    try {
      error.response.data = JSON.parse(await error.response.data.text())
    } catch {
      /* no era JSON: se deja como está */
    }
  }
  return error
}

export function useReportContext() {
  return useQuery({ queryKey: ['social-reports', 'context'], queryFn: reportsApi.context, staleTime: 5 * 60_000 })
}

export function useReportList(params: ReportListParams) {
  return useQuery({
    queryKey: ['social-reports', 'list', params],
    queryFn: async ({ signal }) =>
      (
        await api.get<PagedResult<SocialReportListItem>>(BASE, {
          params: { ...params, search: params.search || undefined },
          signal,
        })
      ).data,
    placeholderData: keepPreviousData,
  })
}
