import { apiFetch } from "./apiClient.js";

export function getSystemSettings() {
  return apiFetch("/system-settings", { auth: true });
}
export function updateSystemSettings(dto) {
  return apiFetch("/system-settings", { method: "PUT", body: dto, auth: true });
}
