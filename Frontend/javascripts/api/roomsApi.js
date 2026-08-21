import { apiFetch } from "./apiClient.js";

export function getAllRooms() {
  return apiFetch("/rooms", { auth: true });
}

export function searchAvailableRooms(dto) {
  return apiFetch("/rooms/search-available", { method: "POST", body: dto, auth: true });
}

export function getAllBuildings() {
  return apiFetch("/buildings", { auth: true });
}

export function getAllFloors() {
  return apiFetch("/floors", { auth: true });
}

export function getAllEquipment() {
  return apiFetch("/equipments", { auth: true });
}