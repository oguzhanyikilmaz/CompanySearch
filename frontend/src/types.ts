export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type BusinessListItem = {
  id: string;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  phone?: string | null;
  email?: string | null;
  website?: string | null;
  source: string;
  leadScore: number;
  priority: string;
  latestScore?: number | null;
  latestEmailStatus?: string | null;
  createdAtUtc: string;
};

export type WebsiteIssue = {
  category: string;
  severity: string;
  code: string;
  title: string;
  description: string;
  recommendation: string;
  penalty: number;
  evidence?: string | null;
  /** Sunucudan Türkçe başlık (yoksa `title` kullanılır) */
  titleTr?: string | null;
  descriptionTr?: string | null;
  recommendationTr?: string | null;
};

export type WebsiteSnapshot = {
  finalUrl?: string | null;
  statusCode: number;
  title?: string | null;
  metaDescription?: string | null;
  h1Tags: string[];
  h2Tags: string[];
  internalLinks: string[];
  brokenLinks: string[];
  responseTimeMs: number;
  imageCount: number;
  imagesWithoutAltCount: number;
  javaScriptFileCount: number;
  stylesheetFileCount: number;
  hasViewportMeta: boolean;
  usesHttps: boolean;
  missingSecurityHeaders: string[];
};

export type WebsiteAnalysis = {
  id: string;
  score: number;
  summary: string;
  createdAtUtc: string;
  snapshot: WebsiteSnapshot;
  issues: WebsiteIssue[];
};

export type SalesEmail = {
  id: string;
  subject: string;
  body: string;
  recipientEmail?: string | null;
  sentStatus: string;
  generatedByModel?: string | null;
  retryCount: number;
  lastError?: string | null;
  createdAtUtc: string;
  sentAtUtc?: string | null;
};

export type BusinessDetail = {
  id: string;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  phone?: string | null;
  email?: string | null;
  website?: string | null;
  source: string;
  leadScore: number;
  priority: string;
  tags: string[];
  latestAnalysis?: WebsiteAnalysis | null;
  latestEmail?: SalesEmail | null;
  createdAtUtc: string;
};

export type SearchJob = {
  id: string;
  locationQuery: string;
  latitude: number;
  longitude: number;
  radiusKm: number;
  source: string;
  status: string;
  autoAnalyzeWebsites: boolean;
  autoGenerateEmails: boolean;
  businessesDiscovered: number;
};
