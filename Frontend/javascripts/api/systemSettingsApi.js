import { apiFetch } from "./apiClient.js";

export function getSystemSettings(buildingId) {
  const query = buildingId ? `?buildingId=${buildingId}` : "";
  return apiFetch(`/system-settings${query}`, { auth: true });
}
export function updateSystemSettings(buildingId, dto) {
  const query = buildingId ? `?buildingId=${buildingId}` : "";
  return apiFetch(`/system-settings${query}`, { method: "PUT", body: dto, auth: true });
}
export function resetSystemSettingsToGlobal(buildingId) {
  return apiFetch(`/system-settings/${buildingId}`, { method: "DELETE", auth: true });
}
