/** Sunucudan gelen İngilizce etiketleri arayüzde Türkçeleştirir (bilinmeyenler aynen kalır). */

const categoryMap: Record<string, string> = {
  performance: "Performans",
  security: "Güvenlik",
  accessibility: "Erişilebilirlik",
  seo: "SEO",
  content: "İçerik",
  ux: "Kullanılabilirlik",
  technical: "Teknik",
  compliance: "Uyumluluk",
  marketing: "Pazarlama"
};

const severityMap: Record<string, string> = {
  critical: "Kritik",
  high: "Yüksek",
  medium: "Orta",
  low: "Düşük",
  info: "Bilgi"
};

export function trIssueCategory(value: string): string {
  const key = value.trim().toLowerCase();
  return categoryMap[key] ?? value;
}

export function trIssueSeverity(value: string): string {
  const key = value.trim().toLowerCase();
  return severityMap[key] ?? value;
}

const emailStatusMap: Record<string, string> = {
  draft: "Taslak",
  pending: "Beklemede",
  sent: "Gönderildi",
  failed: "Başarısız",
  cancelled: "İptal"
};

export function trEmailSentStatus(value: string): string {
  const key = value.trim().toLowerCase();
  return emailStatusMap[key] ?? value;
}

const priorityMap: Record<string, string> = {
  low: "Düşük",
  medium: "Orta",
  high: "Yüksek",
  strategic: "Stratejik"
};

export function trLeadPriority(value: string): string {
  const key = value.trim().toLowerCase();
  return priorityMap[key] ?? value;
}
