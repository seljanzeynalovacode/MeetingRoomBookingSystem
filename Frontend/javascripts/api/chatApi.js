import { apiFetch } from "./apiClient.js";

export function sendChatMessage(dto) {
  return apiFetch("/chat", { method: "POST", body: dto, auth: true });
}
