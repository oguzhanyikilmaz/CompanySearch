import clsx from "clsx";
import { ChevronLeft, ChevronRight, Globe, Loader2, Mail, MapPin, Navigation, Phone } from "lucide-react";
import type { BusinessListSort } from "../lib/api";
import { formatDistanceKm, haversineDistanceKm } from "../lib/geo";
import { isGeolocationSupported } from "../lib/deviceLocation";
import { trLeadPriority } from "../lib/analysisLabels";
import type { BusinessListItem } from "../types";
import { ScoreBadge } from "./ScoreBadge";

export type DistanceReferenceSource = "device" | "search";

type BusinessListProps = {
  businesses: BusinessListItem[];
  selectedBusinessId?: string;
  onSelect: (businessId: string) => void;
  loading: boolean;
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  sortBy: BusinessListSort;
  onSortByChange: (sort: BusinessListSort) => void;
  /** Mesafe sıralaması ve sütun için: önce cihaz, yoksa arama merkezi */
  distanceReferenceCenter: { latitude: number; longitude: number } | null;
  distanceSource: DistanceReferenceSource | null;
  deviceLocationLoading: boolean;
  onRefreshDeviceLocation?: () => void;
  /** Arama yapılmış merkez (konum izni yoksa mesafe yedeği) */
  searchCenterAvailable: boolean;
};

export function BusinessList({
  businesses,
  selectedBusinessId,
  onSelect,
  loading,
  page,
  totalPages,
  totalCount,
  pageSize,
  onPageChange,
  onPageSizeChange,
  sortBy,
  onSortByChange,
  distanceReferenceCenter,
  distanceSource,
  deviceLocationLoading,
  onRefreshDeviceLocation,
  searchCenterAvailable
}: BusinessListProps) {
  const distanceMode = sortBy === "distance" && distanceReferenceCenter !== null;
  const canSortByDistance = isGeolocationSupported() || searchCenterAvailable;

  const sortHint =
    distanceSource === "device"
      ? "Cihaz konumunuza göre"
      : distanceSource === "search"
        ? "Arama merkezine göre"
        : null;

  return (
    <section className="flex min-h-0 max-h-[min(85dvh,48rem)] flex-col overflow-hidden rounded-2xl border border-zinc-200/80 bg-white shadow-sm">
      <header className="border-b border-zinc-100 px-5 py-4">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-[11px] font-medium uppercase tracking-wider text-zinc-400">Liste</p>
            <h2 className="mt-0.5 text-lg font-semibold tracking-tight text-zinc-900">İşletmeler</h2>
            <p className="mt-1 text-sm text-zinc-500">
              {loading ? "Yükleniyor…" : `${new Intl.NumberFormat("tr-TR").format(totalCount)} kayıt`}
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <label className="grid gap-1 text-xs text-zinc-500">
              <span>Sıralama</span>
              <div className="flex items-center gap-2">
                <select
                  className="min-w-[12rem] rounded-lg border border-zinc-200 bg-white px-3 py-2 text-sm text-zinc-900 outline-none ring-zinc-900/5 transition focus:ring-2 disabled:opacity-50"
                  value={sortBy}
                  disabled={deviceLocationLoading}
                  onChange={(e) => onSortByChange(e.target.value as BusinessListSort)}
                >
                  <option value="leadScore">Önce yüksek lead skoru</option>
                  <option value="newest">En yeni kayıtlar</option>
                  <option value="distance" disabled={!canSortByDistance}>
                    Yakınımdan uzağa (konum)
                  </option>
                </select>
                {deviceLocationLoading && <Loader2 className="h-4 w-4 shrink-0 animate-spin text-zinc-500" aria-hidden />}
              </div>
            </label>
            {sortBy === "distance" && onRefreshDeviceLocation && isGeolocationSupported() && (
              <button
                type="button"
                onClick={onRefreshDeviceLocation}
                disabled={deviceLocationLoading || loading}
                className="inline-flex items-center gap-1.5 rounded-lg border border-zinc-200 px-2.5 py-2 text-xs font-medium text-zinc-700 transition hover:bg-zinc-50 disabled:opacity-40"
                title="Konumu yenile"
              >
                <Navigation className="h-3.5 w-3.5" />
                Konumu yenile
              </button>
            )}
            {!canSortByDistance && (
              <p className="max-w-[14rem] text-xs leading-relaxed text-zinc-400">
                Mesafe için tarayıcı konumu veya önce bir arama (merkez yedeği) gerekir.
              </p>
            )}
            {sortBy === "distance" && sortHint && (
              <p className="max-w-[12rem] text-xs leading-relaxed text-zinc-500">{sortHint}</p>
            )}
            <label className="grid gap-1 text-xs text-zinc-500">
              <span>Sayfa boyutu</span>
              <select
                className="rounded-lg border border-zinc-200 bg-white px-3 py-2 text-sm text-zinc-900 outline-none focus:ring-2 focus:ring-zinc-900/5"
                value={String(pageSize)}
                onChange={(e) => onPageSizeChange(Number(e.target.value))}
              >
                {[8, 12, 24, 48].map((n) => (
                  <option key={n} value={n}>
                    {n} / sayfa
                  </option>
                ))}
              </select>
            </label>
          </div>
        </div>
      </header>

      <div className="flex-1 overflow-y-auto">
        {businesses.map((business) => {
          const dist =
            distanceMode && distanceReferenceCenter
              ? haversineDistanceKm(
                  distanceReferenceCenter.latitude,
                  distanceReferenceCenter.longitude,
                  business.latitude,
                  business.longitude
                )
              : null;

          return (
            <button
              key={business.id}
              type="button"
              onClick={() => onSelect(business.id)}
              className={clsx(
                "grid w-full gap-2 border-b border-zinc-50 px-5 py-4 text-left transition",
                selectedBusinessId === business.id ? "bg-zinc-50" : "hover:bg-zinc-50/80"
              )}
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium text-zinc-900">{business.name}</p>
                  <p className="mt-0.5 line-clamp-2 text-sm leading-relaxed text-zinc-500">{business.address}</p>
                </div>
                <div className="flex shrink-0 flex-col items-end gap-1.5">
                  <ScoreBadge value={business.latestScore} />
                  {dist !== null && (
                    <span className="inline-flex items-center gap-1 text-xs font-medium text-zinc-500">
                      <MapPin className="h-3.5 w-3.5" />
                      {formatDistanceKm(dist)}
                    </span>
                  )}
                </div>
              </div>

              <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-zinc-500">
                {business.website && (
                  <span className="inline-flex items-center gap-1">
                    <Globe className="h-3.5 w-3.5 shrink-0" />
                    Web sitesi
                  </span>
                )}
                {business.email && (
                  <span className="inline-flex items-center gap-1">
                    <Mail className="h-3.5 w-3.5 shrink-0" />
                    E-posta
                  </span>
                )}
                {business.phone && (
                  <span className="inline-flex items-center gap-1">
                    <Phone className="h-3.5 w-3.5 shrink-0" />
                    Telefon
                  </span>
                )}
                <span className="rounded-full bg-zinc-100 px-2 py-0.5 font-medium text-zinc-700">
                  {trLeadPriority(business.priority)}
                </span>
              </div>
            </button>
          );
        })}

        {!loading && businesses.length === 0 && (
          <div className="px-5 py-14 text-center text-sm text-zinc-500">
            Filtrelere uyan işletme bulunamadı. Arama metnini veya skor aralığını gevşetmeyi deneyin.
          </div>
        )}
      </div>

      {totalPages > 0 && (
        <footer className="flex flex-wrap items-center justify-between gap-3 border-t border-zinc-100 px-5 py-3">
          <p className="text-xs text-zinc-500">
            Sayfa{" "}
            <span className="font-medium text-zinc-800">
              {page} / {Math.max(1, totalPages)}
            </span>
          </p>
          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={page <= 1 || loading}
              onClick={() => onPageChange(page - 1)}
              className="inline-flex items-center gap-1 rounded-lg border border-zinc-200 px-3 py-1.5 text-sm font-medium text-zinc-700 transition enabled:hover:bg-zinc-50 disabled:opacity-40"
            >
              <ChevronLeft className="h-4 w-4" />
              Önceki
            </button>
            <button
              type="button"
              disabled={page >= totalPages || loading}
              onClick={() => onPageChange(page + 1)}
              className="inline-flex items-center gap-1 rounded-lg border border-zinc-200 px-3 py-1.5 text-sm font-medium text-zinc-700 transition enabled:hover:bg-zinc-50 disabled:opacity-40"
            >
              Sonraki
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        </footer>
      )}
    </section>
  );
}
