export type DeviceCoordinates = {
  latitude: number;
  longitude: number;
  accuracyM?: number;
};

export function isGeolocationSupported(): boolean {
  return typeof navigator !== "undefined" && Boolean(navigator.geolocation);
}

/** Tarayıcıdan tek seferlik konum (HTTPS veya localhost gerekir). */
export function getCurrentPositionOnce(options?: PositionOptions): Promise<DeviceCoordinates> {
  return new Promise((resolve, reject) => {
    if (!isGeolocationSupported()) {
      reject(new Error("Geolocation desteklenmiyor"));
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        resolve({
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
          accuracyM: pos.coords.accuracy
        });
      },
      (err) => {
        reject(err);
      },
      {
        enableHighAccuracy: false,
        maximumAge: 60_000,
        timeout: 15_000,
        ...options
      }
    );
  });
}
