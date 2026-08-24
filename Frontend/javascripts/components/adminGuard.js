import { isLoggedIn, getAccessToken } from "../api/apiClient.js";

function parseJwt(token) {
  try {
    const payload = token.split(".")[1];
    const json = decodeURIComponent(
      atob(payload.replace(/-/g, "+").replace(/_/g, "/"))
        .split("")
        .map((c) => "%" + c.charCodeAt(0).toString(16).padStart(2, "0"))
        .join("")
    );
    return JSON.parse(json);
  } catch {
    return null;
  }
}

export function requireAdmin() {
  if (!isLoggedIn()) {
    window.location.href = "adminLogin.html";
    return;
  }

  const claims = parseJwt(getAccessToken()) || {};
  const roleClaim = claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  const roles = Array.isArray(roleClaim) ? roleClaim : roleClaim ? [roleClaim] : [];

  if (!roles.includes("Admin")) {
    window.location.href = "dashboard.html";
  }
}