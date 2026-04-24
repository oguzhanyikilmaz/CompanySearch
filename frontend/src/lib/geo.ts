/** İki koordinat arası kuş uçumu mesafe (km), WGS84. */
export function haversineDistanceKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const earthKm = 6371;
  const toRad = (deg: number) => (deg * Math.PI) / 180;
  const dLat = toRad(lat2 - lat1);
  const dLon = toRad(lon2 - lon1);
  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return earthKm * c;
}

export function formatDistanceKm(km: number): string {
  if (!Number.isFinite(km) || km < 0) {
    return "—";
  }
  if (km < 1) {
    return `${Math.round(km * 1000)} m`;
  }
  return `${new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 2, minimumFractionDigits: 0 }).format(km)} km`;
}
