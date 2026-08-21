import { apiFetch } from "./apiClient.js";

export function getAllRoles() {
  return apiFetch("/auths/roles", { auth: true });
}

export function createRole(dto) {
  return apiFetch("/auths/roles", { method: "POST", body: dto, auth: true });
}

export function deleteRole(id) {
  return apiFetch(`/auths/roles/${id}`, { method: "DELETE", auth: true });
}

export function getAllUsersWithRoles() {
  return apiFetch("/auths/users", { auth: true });
}

export function assignRole(userId, roleName) {
  return apiFetch(`/auths/users/${userId}/roles/${encodeURIComponent(roleName)}`, { method: "POST", auth: true });
}

export function removeRole(userId, roleName) {
  return apiFetch(`/auths/users/${userId}/roles/${encodeURIComponent(roleName)}`, { method: "DELETE", auth: true });
}

export function deleteUser(userId) {
  return apiFetch(`/auths/users/${userId}`, { method: "DELETE", auth: true });
}