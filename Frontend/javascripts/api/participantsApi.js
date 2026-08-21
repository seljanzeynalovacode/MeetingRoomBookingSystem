import { apiFetch } from "./apiClient.js";

export function createParticipant(dto) {
  return apiFetch("/participants", { method: "POST", body: dto, auth: true });
}

export function getParticipantsByReservation(reservationId) {
  return apiFetch(`/participants/by-reservation/${reservationId}`, { auth: true });
}

export function deleteParticipant(id) {
  return apiFetch(`/participants/${id}`, { method: "DELETE", auth: true });
}