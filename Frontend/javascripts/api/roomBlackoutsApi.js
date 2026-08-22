import { apiFetch } from "./apiClient.js";

export function getAllRoomBlackouts() {
  return apiFetch("/roomblackouts", { auth: true });
}
export function createRoomBlackout(dto) {
  return apiFetch("/roomblackouts", { method: "POST", body: dto, auth: true });
}
export function updateRoomBlackout(id, dto) {
  return apiFetch(`/roomblackouts/${id}`, { method: "PUT", body: dto, auth: true });
}
export function deleteRoomBlackout(id) {
  return apiFetch(`/roomblackouts/${id}`, { method: "DELETE", auth: true });
}