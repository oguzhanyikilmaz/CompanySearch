import { useEffect } from "react";
import { MapContainer, TileLayer, CircleMarker, Popup, useMap, Circle } from "react-leaflet";
import type { BusinessListItem } from "../types";
import { ScoreBadge } from "./ScoreBadge";

type MapPanelProps = {
  businesses: BusinessListItem[];
  selectedBusinessId?: string;
  onSelect: (businessId: string) => void;
  referenceCenter?: { latitude: number; longitude: number } | null;
  /** Tarayıcıdan alınan cihaz konumu */
  deviceLocation?: { latitude: number; longitude: number } | null;
  radiusKm?: number;
};

function FitToSelection({
  businesses,
  selectedBusinessId,
  referenceCenter,
  deviceLocation
}: {
  businesses: BusinessListItem[];
  selectedBusinessId?: string;
  referenceCenter?: { latitude: number; longitude: number } | null;
  deviceLocation?: { latitude: number; longitude: number } | null;
}) {
  const map = useMap();

  useEffect(() => {
    if (businesses.length === 0 && !referenceCenter && !deviceLocation) {
      return;
    }

    const selected = businesses.find((business) => business.id === selectedBusinessId);
    if (selected) {
      map.flyTo([selected.latitude, selected.longitude], 15, {
        duration: 0.8
      });
      return;
    }

    const points: [number, number][] = businesses.map((business) => [business.latitude, business.longitude]);
    if (referenceCenter) {
      points.push([referenceCenter.latitude, referenceCenter.longitude]);
    }
    if (deviceLocation) {
      points.push([deviceLocation.latitude, deviceLocation.longitude]);
    }
    if (points.length === 0) {
      return;
    }
    if (points.length === 1) {
      map.flyTo(points[0], 14, { duration: 0.6 });
      return;
    }
    map.fitBounds(points, {
      padding: [40, 40]
    });
  }, [businesses, selectedBusinessId, referenceCenter, deviceLocation, map]);

  return null;
}

export function MapPanel({ businesses, selectedBusinessId, onSelect, referenceCenter, deviceLocation, radiusKm }: MapPanelProps) {
  const center =
    businesses.length > 0
      ? ([businesses[0].latitude, businesses[0].longitude] as [number, number])
      : referenceCenter
        ? ([referenceCenter.latitude, referenceCenter.longitude] as [number, number])
        : ([41.0082, 28.9784] as [number, number]);

  const radiusM = radiusKm && radiusKm > 0 ? radiusKm * 1000 : undefined;

  return (
    <section className="flex min-h-0 max-h-[min(85dvh,48rem)] flex-col overflow-hidden rounded-2xl border border-zinc-200/80 bg-white shadow-sm">
      <div className="border-b border-zinc-100 px-5 py-4">
        <p className="text-[11px] font-medium uppercase tracking-wider text-zinc-400">Harita</p>
        <h2 className="mt-0.5 text-lg font-semibold tracking-tight text-zinc-900">Konumlar</h2>
        <p className="mt-1 text-sm text-zinc-500">
          Mavi nokta: cihaz konumunuz. Gri daire: arama merkezi ve yarıçap (varsa).
        </p>
      </div>

      <div className="min-h-0 flex-1">
        <MapContainer center={center} zoom={12} className="h-full w-full" scrollWheelZoom>
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> katkıcıları'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <FitToSelection
            businesses={businesses}
            selectedBusinessId={selectedBusinessId}
            referenceCenter={referenceCenter ?? null}
            deviceLocation={deviceLocation ?? null}
          />

          {deviceLocation ? (
            <CircleMarker
              center={[deviceLocation.latitude, deviceLocation.longitude]}
              radius={9}
              pathOptions={{
                fillColor: "#2563eb",
                color: "#1e40af",
                fillOpacity: 0.9,
                weight: 2
              }}
            >
              <Popup>
                <p className="text-sm font-medium text-zinc-900">Bulunduğunuz konum</p>
              </Popup>
            </CircleMarker>
          ) : null}

          {referenceCenter && radiusM ? (
            <Circle
              center={[referenceCenter.latitude, referenceCenter.longitude]}
              radius={radiusM}
              pathOptions={{
                color: "#a1a1aa",
                weight: 1,
                fillColor: "#71717a",
                fillOpacity: 0.06
              }}
            />
          ) : null}

          {businesses.map((business) => {
            const selected = business.id === selectedBusinessId;
            const score = business.latestScore ?? 0;
            const fillColor = score >= 80 ? "#0f8b6d" : score >= 55 ? "#d48a16" : "#c75768";

            return (
              <CircleMarker
                key={business.id}
                center={[business.latitude, business.longitude]}
                radius={selected ? 11 : 8}
                pathOptions={{
                  fillColor,
                  color: selected ? "#18181b" : fillColor,
                  fillOpacity: 0.82,
                  weight: selected ? 2 : 1
                }}
                eventHandlers={{
                  click: () => onSelect(business.id)
                }}
              >
                <Popup>
                  <div className="space-y-2 min-w-[10rem]">
                    <p className="font-medium text-zinc-900">{business.name}</p>
                    <p className="text-sm text-zinc-600">{business.address}</p>
                    <ScoreBadge value={business.latestScore} />
                  </div>
                </Popup>
              </CircleMarker>
            );
          })}
        </MapContainer>
      </div>
    </section>
  );
}
