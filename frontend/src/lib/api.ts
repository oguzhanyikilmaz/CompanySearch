import axios from "axios";
import type { BusinessDetail, BusinessListItem, PagedResult, SalesEmail, SearchJob, WebsiteAnalysis } from "../types";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? ""
});

/** API ile aynı olmalı: `PurgeAllApplicationDataCommandHandler.RequiredConfirmation` */
export const PURGE_ALL_CONFIRMATION_PHRASE = "TÜM VERİYİ SİL";

export async function purgeAllApplicationData(confirmation: string) {
  await api.post("/api/maintenance/purge-all", { confirmation });
}

function buildParams(params: Record<string, string | number | boolean | null | undefined>) {
  return Object.fromEntries(Object.entries(params).filter(([, value]) => value !== undefined && value !== null && value !== ""));
}

export type BusinessListSort = "leadScore" | "newest" | "distance";

/** API `priority` sorgu parametresi: low | medium | high | strategic */
export type LeadPriorityApi = "low" | "medium" | "high" | "strategic";

export async function fetchBusinesses(params: {
  page: number;
  pageSize: number;
  searchTerm?: string;
  hasWebsite?: boolean | null;
  minScore?: number | null;
  maxScore?: number | null;
  priority?: LeadPriorityApi | null;
  sortBy?: BusinessListSort;
  refLatitude?: number | null;
  refLongitude?: number | null;
}) {
  const sortBy =
    params.sortBy === "newest" ? "newest" : params.sortBy === "distance" ? "distance" : "leadScore";
  const response = await api.get<PagedResult<BusinessListItem>>("/api/businesses", {
    params: buildParams({
      page: params.page,
      pageSize: params.pageSize,
      searchTerm: params.searchTerm,
      hasWebsite: params.hasWebsite,
      minScore: params.minScore,
      maxScore: params.maxScore,
      priority: params.priority ?? null,
      sortBy,
      refLatitude: params.sortBy === "distance" ? params.refLatitude : null,
      refLongitude: params.sortBy === "distance" ? params.refLongitude : null
    })
  });
  return response.data;
}

export async function fetchBusiness(businessId: string) {
  const response = await api.get<BusinessDetail>(`/api/businesses/${businessId}`);
  return response.data;
}

export async function startSearch(payload: {
  location?: string;
  latitude?: number;
  longitude?: number;
  radiusKm: number;
  source: string;
  autoAnalyzeWebsites: boolean;
  autoGenerateEmails: boolean;
}) {
  const response = await api.post<SearchJob>("/api/search", payload);
  return response.data;
}

export async function analyzeBusiness(businessId: string, generateEmailAfterAnalysis: boolean) {
  const response = await api.post<WebsiteAnalysis>(`/api/analyze/${businessId}`, {
    generateEmailAfterAnalysis
  });
  return response.data;
}

export async function generateEmail(businessId: string) {
  const response = await api.post<SalesEmail>(`/api/generate-email/${businessId}`);
  return response.data;
}

export async function sendEmail(businessId: string) {
  const response = await api.post<SalesEmail>(`/api/send-email/${businessId}`);
  return response.data;
}

export async function exportBusinesses(params: {
  searchTerm?: string;
  hasWebsite?: boolean | null;
  minScore?: number | null;
  maxScore?: number | null;
  priority?: LeadPriorityApi | null;
}) {
  const response = await api.get<string>("/api/businesses/export", {
    params: buildParams(params),
    responseType: "text"
  });
  return response.data;
}
