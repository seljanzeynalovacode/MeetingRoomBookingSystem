import { apiFetch } from "./apiClient.js";

export function getAllRoomEquipment() {
  return apiFetch("/roomequipments", { auth: true });
}

export function createRoomEquipment(dto) {
  return apiFetch("/roomequipments", { method: "POST", body: dto, auth: true });
}

export function updateRoomEquipment(id, dto) {
  return apiFetch(`/roomequipments/${id}`, { method: "PUT", body: dto, auth: true });
}

export function deleteRoomEquipment(id) {
  return apiFetch(`/roomequipments/${id}`, { method: "DELETE", auth: true });
}