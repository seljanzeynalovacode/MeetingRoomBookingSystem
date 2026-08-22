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

export function createBuilding(dto) {
  return apiFetch("/buildings", { method: "POST", body: dto, auth: true });
}
export function updateBuilding(id, dto) {
  return apiFetch(`/buildings/${id}`, { method: "PUT", body: dto, auth: true });
}
export function deleteBuilding(id) {
  return apiFetch(`/buildings/${id}`, { method: "DELETE", auth: true });
}

export function createFloor(dto) {
  return apiFetch("/floors", { method: "POST", body: dto, auth: true });
}
export function updateFloor(id, dto) {
  return apiFetch(`/floors/${id}`, { method: "PUT", body: dto, auth: true });
}
export function deleteFloor(id) {
  return apiFetch(`/floors/${id}`, { method: "DELETE", auth: true });
}
export function createRoom(dto) {
  return apiFetch("/rooms", { method: "POST", body: dto, auth: true });
}
export function updateRoom(id, dto) {
  return apiFetch(`/rooms/${id}`, { method: "PUT", body: dto, auth: true });
}
export function deleteRoom(id) {
  return apiFetch(`/rooms/${id}`, { method: "DELETE", auth: true });
}
export function createEquipment(dto) {
  return apiFetch("/equipments", { method: "POST", body: dto, auth: true });
}
export function updateEquipment(id, dto) {
  return apiFetch(`/equipments/${id}`, { method: "PUT", body: dto, auth: true });
}
export function deleteEquipment(id) {
  return apiFetch(`/equipments/${id}`, { method: "DELETE", auth: true });
}