import { apiFetch } from "./apiClient.js";

export function getWorkingHours(buildingId) {
  return apiFetch(`/buildings/${buildingId}/working-hours`, { auth: true });
}
export function updateWorkingHours(buildingId, dto) {
  return apiFetch(`/buildings/${buildingId}/working-hours`, { method: "PUT", body: dto, auth: true });
}