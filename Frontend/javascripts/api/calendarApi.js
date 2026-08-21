import { apiFetch } from "./apiClient.js";

export function getRoomCalendar(roomId, startDate, endDate) {
  const params = new URLSearchParams({ startDate, endDate });
  return apiFetch(`/rooms/${roomId}/calendar?${params.toString()}`, { auth: true });
}