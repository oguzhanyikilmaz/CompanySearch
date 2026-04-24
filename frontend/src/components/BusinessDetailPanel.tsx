import type { ReactNode } from "react";
import { AlertTriangle, Clock3, ExternalLink, Globe, Mail, Send, ShieldCheck, Sparkles } from "lucide-react";
import type { BusinessDetail } from "../types";
import { trEmailSentStatus, trIssueCategory, trIssueSeverity, trLeadPriority } from "../lib/analysisLabels";
import { ScoreBadge } from "./ScoreBadge";

type BusinessDetailPanelProps = {
  business?: BusinessDetail | null;
  loading: boolean;
  actionLoading: "analyze" | "generate" | "send" | null;
  onAnalyze: () => void;
  onGenerateEmail: () => void;
  onSendEmail: () => void;
};

function isHttpUrl(value: string): boolean {
  try {
    const u = new URL(value);
    return u.protocol === "http:" || u.protocol === "https:";
  } catch {
    return false;
  }
}

export function BusinessDetailPanel({
  business,
  loading,
  actionLoading,
  onAnalyze,
  onGenerateEmail,
  onSendEmail
}: BusinessDetailPanelProps) {
  if (loading) {
    return (
      <section className="flex min-h-0 max-h-[min(85dvh,48rem)] flex-1 items-center justify-center rounded-2xl border border-zinc-200/80 bg-white text-sm text-zinc-500 shadow-sm">
        Ayrıntılar yükleniyor…
      </section>
    );
  }

  if (!business) {
    return (
      <section className="flex min-h-0 max-h-[min(85dvh,48rem)] flex-1 flex-col items-center justify-center gap-2 rounded-2xl border border-dashed border-zinc-200 bg-white px-6 text-center text-sm text-zinc-500 shadow-sm">
        <p className="font-medium text-zinc-700">Henüz seçim yok</p>
        <p>Soldaki listeden bir işletme seçerek analiz ve e-posta adımlarına geçin.</p>
      </section>
    );
  }

  const analysis = business.latestAnalysis;
  const email = business.latestEmail;
  const website = business.website;

  return (
    <section className="flex min-h-0 max-h-[min(85dvh,48rem)] flex-1 flex-col overflow-hidden rounded-2xl border border-zinc-200/80 bg-white shadow-sm">
      <div className="shrink-0 border-b border-zinc-100 px-4 py-4 sm:px-5">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div className="min-w-0 flex-1">
            <p className="text-[11px] font-medium uppercase tracking-wider text-zinc-400">Seçili işletme</p>
            <h2 className="mt-1 break-words text-lg font-semibold tracking-tight text-zinc-900 sm:text-xl">
              {business.name}
            </h2>
            <p className="mt-2 line-clamp-4 text-sm leading-relaxed text-zinc-600">{business.address}</p>
          </div>
          <div className="shrink-0 self-start">
            <ScoreBadge value={analysis?.score ?? null} />
          </div>
        </div>

        <div className="mt-4 flex flex-wrap gap-2">
          <button
            className="inline-flex items-center gap-2 rounded-xl bg-zinc-900 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-zinc-800 disabled:opacity-50"
            onClick={onAnalyze}
            type="button"
            disabled={Boolean(actionLoading)}
          >
            <Sparkles className="h-4 w-4" />
            {actionLoading === "analyze" ? "Analiz ediliyor…" : "Web sitesini analiz et"}
          </button>
          <button
            className="inline-flex items-center gap-2 rounded-xl border border-zinc-200 px-4 py-2.5 text-sm font-medium text-zinc-800 transition hover:bg-zinc-50 disabled:opacity-50"
            onClick={onGenerateEmail}
            type="button"
            disabled={Boolean(actionLoading)}
          >
            <Mail className="h-4 w-4" />
            {actionLoading === "generate" ? "Oluşturuluyor…" : "E-posta taslağı oluştur"}
          </button>
          <button
            className="inline-flex items-center gap-2 rounded-xl border border-zinc-200 px-4 py-2.5 text-sm font-medium text-zinc-800 transition hover:bg-zinc-50 disabled:opacity-50"
            onClick={onSendEmail}
            type="button"
            disabled={Boolean(actionLoading)}
          >
            <Send className="h-4 w-4" />
            {actionLoading === "send" ? "Gönderiliyor…" : "E-postayı gönder"}
          </button>
        </div>
      </div>

      <div className="grid min-h-0 flex-1 auto-rows-min gap-4 overflow-y-auto px-4 py-4 sm:gap-5 sm:px-5 sm:py-5">
        <div className="grid min-h-0 gap-3 sm:grid-cols-3">
          <InfoRow
            icon={<Globe className="h-4 w-4 shrink-0" />}
            label="Web sitesi"
            value={website ?? "Yok"}
            linkify={Boolean(website && isHttpUrl(website))}
          />
          <InfoRow icon={<Mail className="h-4 w-4 shrink-0" />} label="E-posta" value={business.email ?? "Yok"} />
          <InfoRow
            icon={<ShieldCheck className="h-4 w-4 shrink-0" />}
            label="Lead skoru"
            value={`${business.leadScore}/100 · ${trLeadPriority(business.priority)}`}
          />
        </div>

        <div className="min-h-0 rounded-xl border border-zinc-100 bg-zinc-50/60 px-3 py-3 sm:px-4 sm:py-4">
          <p className="text-[11px] font-medium uppercase tracking-wider text-zinc-400">Analiz özeti</p>
          <p className="mt-2 line-clamp-6 text-sm leading-relaxed text-zinc-800">
            {analysis?.summary ?? "Bu kayıt için henüz web sitesi analizi oluşturulmadı."}
          </p>

          {analysis && (
            <div className="mt-3 grid gap-2 text-sm text-zinc-600 sm:grid-cols-3">
              <span className="inline-flex min-w-0 items-center gap-2">
                <Clock3 className="h-4 w-4 shrink-0" />
                <span className="truncate">Yanıt: {analysis.snapshot.responseTimeMs} ms</span>
              </span>
              <span className="inline-flex items-center gap-2">
                <AlertTriangle className="h-4 w-4 shrink-0" />
                {analysis.issues.length} sorun
              </span>
              <span className="inline-flex items-center gap-2">
                <ShieldCheck className="h-4 w-4 shrink-0" />
                {analysis.snapshot.usesHttps ? "HTTPS var" : "HTTPS yok"}
              </span>
            </div>
          )}
        </div>

        {analysis && analysis.issues.length > 0 && (
          <div className="grid min-h-0 gap-2">
            <p className="text-[11px] font-medium uppercase tracking-wider text-zinc-400">Öne çıkan sorunlar</p>
            <div className="max-h-52 space-y-2 overflow-y-auto pr-1 sm:max-h-60">
            {analysis.issues.slice(0, 8).map((issue) => {
              const title = issue.titleTr?.trim() || issue.title;
              const description = issue.descriptionTr?.trim() || issue.description;
              const recommendation = issue.recommendationTr?.trim() || issue.recommendation;

              return (
                <div key={issue.code} className="rounded-xl border border-zinc-100 px-3 py-2.5 sm:px-4 sm:py-3">
                  <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-start sm:justify-between">
                    <p className="min-w-0 text-sm font-medium text-zinc-900">{title}</p>
                    <div className="flex flex-wrap gap-2">
                      <span className="rounded-full bg-zinc-100 px-2 py-0.5 text-xs font-medium text-zinc-600">
                        {trIssueCategory(issue.category)}
                      </span>
                      <span className="rounded-full bg-zinc-200/80 px-2 py-0.5 text-xs font-medium text-zinc-700">
                        {trIssueSeverity(issue.severity)}
                      </span>
                    </div>
                  </div>
                  <p className="mt-2 line-clamp-4 text-sm leading-relaxed text-zinc-600">{description}</p>
                  <p className="mt-2 line-clamp-3 border-l-2 border-zinc-300 pl-3 text-sm leading-relaxed text-zinc-800">
                    <span className="font-medium text-zinc-700">Öneri: </span>
                    {recommendation}
                  </p>
                </div>
              );
            })}
            </div>
          </div>
        )}

        <div className="min-h-0 rounded-xl border border-zinc-100 px-3 py-3 sm:px-4 sm:py-4">
          <p className="text-[11px] font-medium uppercase tracking-wider text-zinc-400">E-posta önizlemesi</p>
          {email ? (
            <div className="mt-3 space-y-3">
              <div className="min-w-0">
                <p className="text-xs font-medium text-zinc-500">Konu</p>
                <p className="mt-1 line-clamp-2 break-words text-sm font-medium text-zinc-900">{email.subject}</p>
              </div>
              <div className="min-w-0">
                <p className="text-xs font-medium text-zinc-500">İleti</p>
                <div className="mt-1 max-h-48 overflow-y-auto rounded-lg border border-zinc-100 bg-white p-3 text-sm leading-relaxed text-zinc-800">
                  <p className="whitespace-pre-wrap break-words">{email.body}</p>
                </div>
              </div>
              <div className="flex flex-wrap gap-x-3 gap-y-1 text-xs text-zinc-600">
                <span>
                  Durum: <span className="font-medium text-zinc-800">{trEmailSentStatus(email.sentStatus)}</span>
                </span>
                {email.generatedByModel ? (
                  <span>
                    Model: <span className="font-medium text-zinc-800">{email.generatedByModel}</span>
                  </span>
                ) : null}
                {email.recipientEmail ? (
                  <span className="min-w-0 break-all">
                    Alıcı: <span className="font-medium text-zinc-800">{email.recipientEmail}</span>
                  </span>
                ) : null}
                {email.lastError ? (
                  <span className="text-rose-700">
                    Son hata: <span className="font-medium break-words">{email.lastError}</span>
                  </span>
                ) : null}
              </div>
            </div>
          ) : (
            <p className="mt-2 text-sm text-zinc-600">Bu kayıt için henüz e-posta taslağı yok.</p>
          )}
        </div>
      </div>
    </section>
  );
}

function InfoRow({
  icon,
  label,
  value,
  linkify
}: {
  icon: ReactNode;
  label: string;
  value: string;
  linkify?: boolean;
}) {
  return (
    <div className="flex min-h-0 min-w-0 flex-col rounded-xl border border-zinc-100 px-3 py-3">
      <div className="inline-flex items-center gap-2 text-xs font-medium uppercase tracking-wider text-zinc-400">
        {icon}
        {label}
      </div>
      <div className="mt-2 min-h-0 min-w-0">
        {linkify && value !== "Yok" ? (
          <a
            href={value}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex max-w-full items-start gap-1 text-sm font-medium text-sky-700 hover:underline"
          >
            <span className="line-clamp-3 break-all">{value}</span>
            <ExternalLink className="mt-0.5 h-3.5 w-3.5 shrink-0" aria-hidden />
          </a>
        ) : (
          <p className="line-clamp-4 break-words text-sm text-zinc-900">{value}</p>
        )}
      </div>
    </div>
  );
}
