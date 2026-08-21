import { apiFetch } from "./apiClient.js";

export function createReservation(dto) {
  return apiFetch("/reservations", { method: "POST", body: dto, auth: true });
}

export function updateReservation(id, dto) {
  return apiFetch(`/reservations/${id}`, { method: "PUT", body: dto, auth: true });
}

export function cancelReservation(id) {
  return apiFetch(`/reservations/${id}/cancel`, { method: "POST", auth: true });
}

export function getMyReservations() {
  return apiFetch("/reservations/my", { auth: true });
}