import { startTransition, useCallback, useDeferredValue, useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { BusinessDetailPanel } from "../components/BusinessDetailPanel";
import { BusinessList, type DistanceReferenceSource } from "../components/BusinessList";
import type { BusinessListSort } from "../lib/api";
import { MapPanel } from "../components/MapPanel";
import { SearchControls } from "../components/SearchControls";
import { mapApiErrorToTurkish } from "../lib/errors";
import {
  analyzeBusiness,
  exportBusinesses,
  fetchBusiness,
  fetchBusinesses,
  generateEmail,
  purgeAllApplicationData,
  PURGE_ALL_CONFIRMATION_PHRASE,
  sendEmail,
  startSearch,
  type LeadPriorityApi
} from "../lib/api";
import type { LeadPriorityFilter } from "../components/SearchControls";
import { getCurrentPositionOnce, isGeolocationSupported } from "../lib/deviceLocation";
import type { BusinessDetail, BusinessListItem } from "../types";

type Banner = { type: "success" | "error"; text: string } | null;

export function DashboardPage() {
  const navigate = useNavigate();
  const { businessId } = useParams();

  const [location, setLocation] = useState("Kadıköy, İstanbul");
  const [radiusKm, setRadiusKm] = useState(3);
  const [searchTerm, setSearchTerm] = useState("");
  const [hasWebsite, setHasWebsite] = useState<"all" | "yes" | "no">("all");
  const [leadPriority, setLeadPriority] = useState<LeadPriorityFilter>("all");
  const [minScore, setMinScore] = useState("");
  const [maxScore, setMaxScore] = useState("");
  const [autoAnalyzeWebsites, setAutoAnalyzeWebsites] = useState(true);
  const [autoGenerateEmails, setAutoGenerateEmails] = useState(true);

  const [businesses, setBusinesses] = useState<BusinessListItem[]>([]);
  const [listPage, setListPage] = useState(1);
  const [listPageSize, setListPageSize] = useState(12);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [sortBy, setSortBy] = useState<BusinessListSort>("leadScore");
  const [searchReference, setSearchReference] = useState<{ latitude: number; longitude: number } | null>(null);
  const [deviceLocation, setDeviceLocation] = useState<{ latitude: number; longitude: number } | null>(null);
  const [deviceLocationLoading, setDeviceLocationLoading] = useState(false);
  const [listNonce, setListNonce] = useState(0);

  const [selectedBusiness, setSelectedBusiness] = useState<BusinessDetail | null>(null);
  const [listLoading, setListLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [searchBusy, setSearchBusy] = useState(false);
  const [actionLoading, setActionLoading] = useState<"analyze" | "generate" | "send" | null>(null);
  const [banner, setBanner] = useState<Banner>(null);
  const [purgeBusy, setPurgeBusy] = useState(false);

  const deferredSearchTerm = useDeferredValue(searchTerm);

  const filterKeyRef = useRef("");

  const showBanner = useCallback((next: Banner) => {
    setBanner(next);
    if (next?.type === "success") {
      window.setTimeout(() => setBanner((b) => (b === next ? null : b)), 5000);
    }
  }, []);

  useEffect(() => {
    const filterKey = `${deferredSearchTerm}|${hasWebsite}|${leadPriority}|${minScore}|${maxScore}|${sortBy}|${listPageSize}`;
    const filtersChanged = filterKeyRef.current !== filterKey;
    if (filtersChanged) {
      filterKeyRef.current = filterKey;
    }
    const pageToUse = filtersChanged ? 1 : listPage;
    if (filtersChanged && listPage !== 1) {
      setListPage(1);
    }

    const distanceRefForApi =
      deviceLocation ??
      (!deviceLocationLoading && sortBy === "distance" ? searchReference : null);

    const effectiveSort: BusinessListSort =
      sortBy === "distance" && distanceRefForApi ? "distance" : sortBy === "distance" ? "leadScore" : sortBy;

    let cancelled = false;
    setListLoading(true);

    void (async () => {
      try {
        const priorityApi: LeadPriorityApi | null = leadPriority === "all" ? null : leadPriority;

        const result = await fetchBusinesses({
          page: pageToUse,
          pageSize: listPageSize,
          searchTerm: deferredSearchTerm.trim() || undefined,
          hasWebsite: hasWebsite === "all" ? null : hasWebsite === "yes",
          minScore: minScore ? Number(minScore) : null,
          maxScore: maxScore ? Number(maxScore) : null,
          priority: priorityApi,
          sortBy: effectiveSort,
          refLatitude: distanceRefForApi?.latitude ?? null,
          refLongitude: distanceRefForApi?.longitude ?? null
        });

        if (cancelled) {
          return;
        }

        startTransition(() => {
          setBusinesses(result.items);
          setTotalCount(result.totalCount);
          setTotalPages(result.totalPages);
        });
      } catch (err) {
        if (!cancelled) {
          showBanner({ type: "error", text: mapApiErrorToTurkish(err) });
        }
      } finally {
        if (!cancelled) {
          setListLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [
    deferredSearchTerm,
    deviceLocation,
    deviceLocationLoading,
    hasWebsite,
    leadPriority,
    listNonce,
    listPage,
    listPageSize,
    maxScore,
    minScore,
    searchReference,
    sortBy,
    showBanner
  ]);

  const handleListSortChange = useCallback(
    (sort: BusinessListSort) => {
      if (sort !== "distance") {
        setSortBy(sort);
        return;
      }

      void (async () => {
        if (deviceLocation) {
          setSortBy("distance");
          return;
        }

        if (isGeolocationSupported()) {
          setDeviceLocationLoading(true);
          try {
            const pos = await getCurrentPositionOnce();
            setDeviceLocation(pos);
            setSortBy("distance");
          } catch {
            if (searchReference) {
              showBanner({
                type: "success",
                text: "Cihaz konumu alınamadı; arama merkezine göre sıralanıyor."
              });
              setSortBy("distance");
            } else {
              showBanner({
                type: "error",
                text: "Konum izni gerekli veya önce bir arama çalıştırın."
              });
            }
          } finally {
            setDeviceLocationLoading(false);
          }
          return;
        }

        if (searchReference) {
          setSortBy("distance");
          return;
        }

        showBanner({
          type: "error",
          text: "Tarayıcı konumu desteklenmiyor ve arama merkezi yok; mesafe sıralaması kullanılamaz."
        });
      })();
    },
    [deviceLocation, searchReference, showBanner]
  );

  const handleRefreshDeviceLocation = useCallback(() => {
    void (async () => {
      if (!isGeolocationSupported()) {
        return;
      }
      setDeviceLocationLoading(true);
      try {
        const pos = await getCurrentPositionOnce({ maximumAge: 0 });
        setDeviceLocation(pos);
        setListNonce((n) => n + 1);
        showBanner({ type: "success", text: "Konum güncellendi." });
      } catch {
        showBanner({ type: "error", text: "Güncel konum alınamadı." });
      } finally {
        setDeviceLocationLoading(false);
      }
    })();
  }, [showBanner]);

  useEffect(() => {
    if (!businessId) {
      setSelectedBusiness(null);
      return;
    }

    void loadBusiness(businessId);
  }, [businessId]);

  async function loadBusiness(id: string) {
    setDetailLoading(true);
    try {
      const detail = await fetchBusiness(id);
      setSelectedBusiness(detail);
    } catch (err) {
      showBanner({ type: "error", text: mapApiErrorToTurkish(err) });
    } finally {
      setDetailLoading(false);
    }
  }

  async function handleRunSearch() {
    setSearchBusy(true);
    try {
      const job = await startSearch({
        location,
        radiusKm,
        source: "OpenStreetMap",
        autoAnalyzeWebsites,
        autoGenerateEmails
      });
      setSearchReference({ latitude: job.latitude, longitude: job.longitude });
      showBanner({
        type: "success",
        text: `Arama kuyruğa alındı. Merkez: ${job.locationQuery}. Liste birkaç saniye içinde yenilenecek.`
      });
      window.setTimeout(() => {
        setListPage(1);
        setListNonce((n) => n + 1);
      }, 2500);
    } catch (err) {
      showBanner({ type: "error", text: mapApiErrorToTurkish(err) });
    } finally {
      setSearchBusy(false);
    }
  }

  async function handleAnalyze() {
    if (!selectedBusiness) {
      return;
    }

    setActionLoading("analyze");
    try {
      await analyzeBusiness(selectedBusiness.id, autoGenerateEmails);
      await loadBusiness(selectedBusiness.id);
      setListNonce((n) => n + 1);
      showBanner({ type: "success", text: "Web sitesi analizi tamamlandı." });
    } catch (err) {
      showBanner({ type: "error", text: mapApiErrorToTurkish(err) });
    } finally {
      setActionLoading(null);
    }
  }

  async function handleGenerateEmail() {
    if (!selectedBusiness) {
      return;
    }

    setActionLoading("generate");
    try {
      await generateEmail(selectedBusiness.id);
      await loadBusiness(selectedBusiness.id);
      showBanner({ type: "success", text: "E-posta taslağı oluşturuldu." });
    } catch (err) {
      showBanner({ type: "error", text: mapApiErrorToTurkish(err) });
    } finally {
      setActionLoading(null);
    }
  }

  async function handleSendEmail() {
    if (!selectedBusiness) {
      return;
    }

    setActionLoading("send");
    try {
      await sendEmail(selectedBusiness.id);
      await loadBusiness(selectedBusiness.id);
      setListNonce((n) => n + 1);
      showBanner({ type: "success", text: "E-posta gönderim isteği işlendi." });
    } catch (err) {
      showBanner({ type: "error", text: mapApiErrorToTurkish(err) });
    } finally {
      setActionLoading(null);
    }
  }

  async function handlePurgeAll() {
    const msg = `Bu işlem geri alınamaz.\n\nDevam etmek için tam olarak şunu yazın:\n\n${PURGE_ALL_CONFIRMATION_PHRASE}`;
    const input = window.prompt(msg);
    if (input === null) {
      return;
    }
    if (input.trim() !== PURGE_ALL_CONFIRMATION_PHRASE) {
      showBanner({ type: "error", text: "Onay metni eşleşmedi; işlem iptal edildi." });
      return;
    }

    setPurgeBusy(true);
    try {
      await purgeAllApplicationData(input.trim());
      setBusinesses([]);
      setTotalCount(0);
      setTotalPages(0);
      setSearchReference(null);
      setDeviceLocation(null);
      setSortBy("leadScore");
      setSelectedBusiness(null);
      filterKeyRef.current = "";
      navigate("/");
      setListNonce((n) => n + 1);
      showBanner({ type: "success", text: "Tüm veriler silindi." });
    } catch (err) {
      showBanner({ type: "error", text: mapApiErrorToTurkish(err) });
    } finally {
      setPurgeBusy(false);
    }
  }

  async function handleExport() {
    try {
      const priorityApi: LeadPriorityApi | null = leadPriority === "all" ? null : leadPriority;

      const csv = await exportBusinesses({
        searchTerm,
        hasWebsite: hasWebsite === "all" ? null : hasWebsite === "yes",
        minScore: minScore ? Number(minScore) : null,
        maxScore: maxScore ? Number(maxScore) : null,
        priority: priorityApi
      });

      const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = "isletmeler.csv";
      link.click();
      URL.revokeObjectURL(url);
      showBanner({ type: "success", text: "CSV dosyası indirildi." });
    } catch (err) {
      showBanner({ type: "error", text: mapApiErrorToTurkish(err) });
    }
  }

  const summary = {
    total: totalCount,
    withWebsite: businesses.filter((b) => Boolean(b.website)).length,
    highOpportunity: businesses.filter((b) => (b.latestScore ?? 100) < 60).length,
    readyToContact: businesses.filter((b) => Boolean(b.email)).length
  };

  const distanceRefDisplay =
    deviceLocation ??
    (!deviceLocationLoading && sortBy === "distance" ? searchReference : null);

  const distanceReferenceCenterForList: { latitude: number; longitude: number } | null =
    sortBy === "distance" && distanceRefDisplay ? distanceRefDisplay : null;

  const distanceSource: DistanceReferenceSource | null =
    sortBy !== "distance"
      ? null
      : deviceLocation
        ? "device"
        : sortBy === "distance" && !deviceLocationLoading && searchReference
          ? "search"
          : null;

  return (
    <main className="flex min-h-screen flex-col bg-gradient-to-b from-zinc-50 to-zinc-100/90 text-zinc-900 antialiased">
      <div className="mx-auto flex w-full max-w-[1600px] flex-1 flex-col space-y-5 px-4 py-6 sm:px-6 lg:px-8 min-h-0">
        {banner && (
          <div
            role="status"
            className={
              banner.type === "success"
                ? "rounded-xl border border-emerald-200/80 bg-emerald-50 px-4 py-3 text-sm text-emerald-900"
                : "rounded-xl border border-rose-200/80 bg-rose-50 px-4 py-3 text-sm text-rose-900"
            }
          >
            {banner.text}
          </div>
        )}

        <SearchControls
          location={location}
          radiusKm={radiusKm}
          searchTerm={searchTerm}
          hasWebsite={hasWebsite}
          leadPriority={leadPriority}
          minScore={minScore}
          maxScore={maxScore}
          autoAnalyzeWebsites={autoAnalyzeWebsites}
          autoGenerateEmails={autoGenerateEmails}
          onLocationChange={setLocation}
          onRadiusChange={setRadiusKm}
          onSearchTermChange={setSearchTerm}
          onHasWebsiteChange={setHasWebsite}
          onLeadPriorityChange={setLeadPriority}
          onMinScoreChange={setMinScore}
          onMaxScoreChange={setMaxScore}
          onAutoAnalyzeChange={setAutoAnalyzeWebsites}
          onAutoGenerateChange={setAutoGenerateEmails}
          onRunSearch={handleRunSearch}
          onExport={handleExport}
          onPurgeAll={handlePurgeAll}
          busy={searchBusy}
          purgeBusy={purgeBusy}
        />

        <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <MetricCard label="Toplam kayıt (filtre)" value={summary.total} hint="Sunucudaki eşleşen kayıt sayısı" />
          <MetricCard label="Bu sayfada web sitesi" value={summary.withWebsite} />
          <MetricCard label="Bu sayfada düşük site skoru" value={summary.highOpportunity} hint="Site skoru 60 altı" />
          <MetricCard label="Bu sayfada e-posta var" value={summary.readyToContact} />
        </section>

        <section className="grid min-h-0 flex-1 gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(0,1.15fr)_minmax(0,1fr)] xl:items-stretch">
          <BusinessList
            businesses={businesses}
            selectedBusinessId={businessId}
            onSelect={(id) => navigate(`/business/${id}`)}
            loading={listLoading}
            page={listPage}
            totalPages={totalPages}
            totalCount={totalCount}
            pageSize={listPageSize}
            onPageChange={setListPage}
            onPageSizeChange={(n) => {
              setListPageSize(n);
              setListPage(1);
            }}
            sortBy={sortBy}
            onSortByChange={handleListSortChange}
            distanceReferenceCenter={distanceReferenceCenterForList}
            distanceSource={distanceSource}
            deviceLocationLoading={deviceLocationLoading}
            onRefreshDeviceLocation={handleRefreshDeviceLocation}
            searchCenterAvailable={searchReference !== null}
          />

          <MapPanel
            businesses={businesses}
            selectedBusinessId={businessId}
            onSelect={(id) => navigate(`/business/${id}`)}
            referenceCenter={searchReference}
            deviceLocation={deviceLocation}
            radiusKm={radiusKm}
          />

          <BusinessDetailPanel
            business={selectedBusiness}
            loading={detailLoading}
            actionLoading={actionLoading}
            onAnalyze={handleAnalyze}
            onGenerateEmail={handleGenerateEmail}
            onSendEmail={handleSendEmail}
          />
        </section>
      </div>
    </main>
  );
}

function MetricCard({ label, value, hint }: { label: string; value: number; hint?: string }) {
  return (
    <div className="rounded-2xl border border-zinc-200/80 bg-white px-5 py-4 shadow-sm">
      <p className="text-[11px] font-medium uppercase tracking-wider text-zinc-400">{label}</p>
      <p className="mt-2 text-3xl font-semibold tabular-nums tracking-tight text-zinc-900">{new Intl.NumberFormat("tr-TR").format(value)}</p>
      {hint ? <p className="mt-1.5 text-xs leading-relaxed text-zinc-500">{hint}</p> : null}
    </div>
  );
}
