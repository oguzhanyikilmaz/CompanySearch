import axios from "axios";

type ProblemBody = {
  title?: string;
  detail?: string;
  status?: number;
};

/** API ve ağ hatalarını kullanıcıya gösterilecek Türkçe metne çevirir. */
export function mapApiErrorToTurkish(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const status = error.response?.status;
    const data = error.response?.data as ProblemBody | string | undefined;

    if (data && typeof data === "object" && "detail" in data && typeof data.detail === "string") {
      const detail = data.detail;
      const mapped = translateKnownDetail(detail);
      if (mapped) {
        return mapped;
      }
      return detail;
    }

    if (status === 401) {
      return "Oturum veya yetki hatası. Lütfen tekrar deneyin.";
    }
    if (status === 403) {
      return "Bu işlem için izniniz yok.";
    }
    if (status === 404) {
      return "İstenen kayıt bulunamadı.";
    }
    if (status === 429) {
      return "Çok fazla istek gönderildi. Lütfen kısa süre sonra tekrar deneyin.";
    }
    if (status && status >= 500) {
      return "Sunucuda beklenmeyen bir hata oluştu. Daha sonra tekrar deneyin.";
    }
    if (error.code === "ECONNABORTED") {
      return "İstek zaman aşımına uğradı. Bağlantınızı kontrol edin.";
    }
    if (error.message === "Network Error") {
      return "Ağa bağlanılamadı. API adresini ve internet bağlantınızı kontrol edin.";
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Bilinmeyen bir hata oluştu.";
}

function translateKnownDetail(detail: string): string | null {
  const d = detail.trim();
  const rules: [RegExp, string][] = [
    [/location is required/i, "Konum bilgisi zorunludur."],
    [/no geocoding result/i, "Konum metinden koordinat çıkarılamadı. Adresi netleştirip tekrar deneyin."],
    [/Business rule violation/i, "İş kuralı ihlali."],
    [/Resource not found/i, "Kayıt bulunamadı."],
    [/Onay için tam metin/i, "Silme onayı metni hatalı. İstemde gösterilen ifadeyi aynen yazın."]
  ];
  for (const [re, tr] of rules) {
    if (re.test(d)) {
      return tr;
    }
  }
  return null;
}
