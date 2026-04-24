import { Download, Filter, Radar, Search, Trash2 } from "lucide-react";
import type { LeadPriorityApi } from "../lib/api";

export type LeadPriorityFilter = "all" | LeadPriorityApi;

type SearchControlsProps = {
  location: string;
  radiusKm: number;
  searchTerm: string;
  hasWebsite: "all" | "yes" | "no";
  leadPriority: LeadPriorityFilter;
  minScore: string;
  maxScore: string;
  autoAnalyzeWebsites: boolean;
  autoGenerateEmails: boolean;
  onLocationChange: (value: string) => void;
  onRadiusChange: (value: number) => void;
  onSearchTermChange: (value: string) => void;
  onHasWebsiteChange: (value: "all" | "yes" | "no") => void;
  onLeadPriorityChange: (value: LeadPriorityFilter) => void;
  onMinScoreChange: (value: string) => void;
  onMaxScoreChange: (value: string) => void;
  onAutoAnalyzeChange: (value: boolean) => void;
  onAutoGenerateChange: (value: boolean) => void;
  onRunSearch: () => void;
  onExport: () => void;
  onPurgeAll?: () => void;
  busy: boolean;
  purgeBusy?: boolean;
};

export function SearchControls(props: SearchControlsProps) {
  return (
    <section className="rounded-2xl border border-zinc-200/80 bg-white p-6 shadow-sm">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-[11px] font-medium uppercase tracking-wider text-zinc-400">Arama</p>
          <h1 className="mt-1 text-xl font-semibold tracking-tight text-zinc-900">Lead keşfi</h1>
          <p className="mt-1 max-w-xl text-sm leading-relaxed text-zinc-500">
            Konum ve yarıçapa göre işletmeleri bulun; liste filtreleri aşağıdaki tabloda kayıtları daraltır.
          </p>
        </div>

        <div className="flex shrink-0 flex-wrap gap-2">
          <button
            className="inline-flex items-center gap-2 rounded-xl bg-zinc-900 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-zinc-800 disabled:opacity-50"
            onClick={props.onRunSearch}
            type="button"
            disabled={props.busy}
          >
            <Radar className="h-4 w-4" />
            {props.busy ? "Kuyruğa alınıyor…" : "Aramayı başlat"}
          </button>
          <button
            className="inline-flex items-center gap-2 rounded-xl border border-zinc-200 px-4 py-2.5 text-sm font-medium text-zinc-800 transition hover:bg-zinc-50"
            onClick={props.onExport}
            type="button"
          >
            <Download className="h-4 w-4" />
            CSV dışa aktar
          </button>
        </div>
      </div>

      <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-12 xl:items-end">
        <label className="grid gap-2 xl:col-span-4">
          <span className="text-xs font-medium text-zinc-500">Konum</span>
          <div className="flex items-center gap-2 rounded-xl border border-zinc-200 bg-zinc-50/50 px-3 py-2.5 focus-within:border-zinc-300 focus-within:ring-2 focus-within:ring-zinc-900/5">
            <Search className="h-4 w-4 shrink-0 text-zinc-400" />
            <input
              className="w-full min-w-0 bg-transparent text-sm text-zinc-900 outline-none placeholder:text-zinc-400"
              value={props.location}
              onChange={(event) => props.onLocationChange(event.target.value)}
              placeholder="Örn. Kadıköy, İstanbul"
            />
          </div>
        </label>

        <label className="grid gap-2 xl:col-span-2">
          <span className="text-xs font-medium text-zinc-500">Yarıçap (km)</span>
          <input
            className="rounded-xl border border-zinc-200 bg-white px-3 py-2.5 text-sm text-zinc-900 outline-none focus:border-zinc-300 focus:ring-2 focus:ring-zinc-900/5"
            type="number"
            min={1}
            max={50}
            value={props.radiusKm}
            onChange={(event) => props.onRadiusChange(Number(event.target.value))}
          />
        </label>

        <label className="grid gap-2 xl:col-span-4">
          <span className="text-xs font-medium text-zinc-500">Liste içi metin</span>
          <div className="flex items-center gap-2 rounded-xl border border-zinc-200 bg-zinc-50/50 px-3 py-2.5 focus-within:border-zinc-300 focus-within:ring-2 focus-within:ring-zinc-900/5">
            <Filter className="h-4 w-4 shrink-0 text-zinc-400" />
            <input
              className="w-full min-w-0 bg-transparent text-sm text-zinc-900 outline-none placeholder:text-zinc-400"
              value={props.searchTerm}
              onChange={(event) => props.onSearchTermChange(event.target.value)}
              placeholder="İsim, adres veya web adresi"
            />
          </div>
        </label>

        <label className="grid gap-2 xl:col-span-2">
          <span className="text-xs font-medium text-zinc-500">Web sitesi</span>
          <select
            className="rounded-xl border border-zinc-200 bg-white px-3 py-2.5 text-sm text-zinc-900 outline-none focus:ring-2 focus:ring-zinc-900/5"
            value={props.hasWebsite}
            onChange={(event) => props.onHasWebsiteChange(event.target.value as "all" | "yes" | "no")}
          >
            <option value="all">Tümü</option>
            <option value="yes">Var</option>
            <option value="no">Yok</option>
          </select>
        </label>

        <label className="grid gap-2 xl:col-span-2">
          <span className="text-xs font-medium text-zinc-500">Lead önceliği</span>
          <select
            className="rounded-xl border border-zinc-200 bg-white px-3 py-2.5 text-sm text-zinc-900 outline-none focus:ring-2 focus:ring-zinc-900/5"
            value={props.leadPriority}
            onChange={(event) => props.onLeadPriorityChange(event.target.value as LeadPriorityFilter)}
          >
            <option value="all">Tümü</option>
            <option value="low">Düşük</option>
            <option value="medium">Orta</option>
            <option value="high">Yüksek</option>
            <option value="strategic">Stratejik</option>
          </select>
        </label>

        <div className="grid grid-cols-2 gap-3 xl:col-span-4">
          <label className="grid gap-2">
            <span className="text-xs font-medium text-zinc-500">Min. site skoru</span>
            <input
              className="rounded-xl border border-zinc-200 px-3 py-2.5 text-sm text-zinc-900 outline-none focus:ring-2 focus:ring-zinc-900/5"
              type="number"
              min={0}
              max={100}
              placeholder="—"
              value={props.minScore}
              onChange={(event) => props.onMinScoreChange(event.target.value)}
            />
          </label>
          <label className="grid gap-2">
            <span className="text-xs font-medium text-zinc-500">Maks. site skoru</span>
            <input
              className="rounded-xl border border-zinc-200 px-3 py-2.5 text-sm text-zinc-900 outline-none focus:ring-2 focus:ring-zinc-900/5"
              type="number"
              min={0}
              max={100}
              placeholder="—"
              value={props.maxScore}
              onChange={(event) => props.onMaxScoreChange(event.target.value)}
            />
          </label>
        </div>

        <div className="flex flex-wrap gap-6 text-sm text-zinc-600 xl:col-span-8 xl:items-center">
          <label className="inline-flex cursor-pointer items-center gap-2.5">
            <input
              className="size-4 rounded border-zinc-300 text-zinc-900 focus:ring-zinc-900/20"
              checked={props.autoAnalyzeWebsites}
              onChange={(event) => props.onAutoAnalyzeChange(event.target.checked)}
              type="checkbox"
            />
            <span>Web sitelerini otomatik analiz et</span>
          </label>
          <label className="inline-flex cursor-pointer items-center gap-2.5">
            <input
              className="size-4 rounded border-zinc-300 text-zinc-900 focus:ring-zinc-900/20"
              checked={props.autoGenerateEmails}
              onChange={(event) => props.onAutoGenerateChange(event.target.checked)}
              type="checkbox"
            />
            <span>Analiz sonrası e-posta taslağı üret</span>
          </label>
        </div>
      </div>

      {props.onPurgeAll && (
        <div className="mt-6 border-t border-zinc-100 pt-5">
          <p className="text-[11px] font-medium uppercase tracking-wider text-rose-600/90">Tehlikeli bölge</p>
          <div className="mt-2 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <p className="max-w-xl text-sm text-zinc-600">
              Tüm işletmeler, analizler, e-postalar ve arama geçmişi veritabanından kalıcı olarak silinir. Bu işlem
              geri alınamaz.
            </p>
            <button
              type="button"
              onClick={props.onPurgeAll}
              disabled={props.purgeBusy || props.busy}
              className="inline-flex shrink-0 items-center gap-2 rounded-xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-sm font-medium text-rose-900 transition hover:bg-rose-100 disabled:opacity-50"
            >
              <Trash2 className="h-4 w-4" />
              {props.purgeBusy ? "Siliniyor…" : "Tüm veriyi sil"}
            </button>
          </div>
        </div>
      )}
    </section>
  );
}
