import { apiFetch } from "./apiClient.js";

export function getMyNotifications() {
  return apiFetch("/notifications/my", { auth: true });
}

export function markNotificationAsRead(id) {
  return apiFetch(`/notifications/${id}/read`, { method: "POST", auth: true });
}