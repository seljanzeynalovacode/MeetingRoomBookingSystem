export const API_BASE_URL = "https://localhost:7114/api";

const STORAGE_KEYS = {
  accessToken: "mrb_access_token",
  refreshToken: "mrb_refresh_token",
  expiresAt: "mrb_token_expires_at",
};

export function getAccessToken() {
  return sessionStorage.getItem(STORAGE_KEYS.accessToken);
}

export function getRefreshToken() {
  return sessionStorage.getItem(STORAGE_KEYS.refreshToken);
}

export function setTokens(tokenResponse) {
  sessionStorage.setItem(STORAGE_KEYS.accessToken, tokenResponse.accessToken);
  sessionStorage.setItem(STORAGE_KEYS.refreshToken, tokenResponse.refreshToken);
  sessionStorage.setItem(STORAGE_KEYS.expiresAt, tokenResponse.expiresAt);
}

export function clearTokens() {
  sessionStorage.removeItem(STORAGE_KEYS.accessToken);
  sessionStorage.removeItem(STORAGE_KEYS.refreshToken);
  sessionStorage.removeItem(STORAGE_KEYS.expiresAt);
}

export function isLoggedIn() {
  return !!getAccessToken();
}

class ApiError extends Error {
  constructor(status, message, fieldErrors) {
    super(message);
    this.status = status;
    this.fieldErrors = fieldErrors || null;
  }
}

async function parseErrorBody(response) {
  let body = null;
  try {
    body = await response.json();
  } catch {
    return new ApiError(response.status, response.statusText || "Xəta baş verdi.");
  }

  if (body && body.errors) {
    const fieldErrors = {};
    for (const key in body.errors) {
      fieldErrors[key] = body.errors[key];
    }
    return new ApiError(response.status, body.title || "Daxil edilən məlumatlarda xəta var.", fieldErrors);
  }

  return new ApiError(response.status, (body && body.message) || "Xəta baş verdi.");
}

let refreshPromise = null;

export async function refreshAccessToken() {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    const refreshToken = getRefreshToken();
    if (!refreshToken) throw new ApiError(401, "Sessiya bitib, yenidən daxil olun.");

    const response = await fetch(`${API_BASE_URL}/auths/refresh-token`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      clearTokens();
      throw await parseErrorBody(response);
    }

    const tokens = await response.json();
    setTokens(tokens);
    return tokens;
  })();

  try {
    return await refreshPromise;
  } finally {
    refreshPromise = null;
  }
}

function redirectToLogin() {
  if (!window.location.pathname.endsWith("login.html")) {
    window.location.href = "login.html";
  }
}

export async function apiFetch(path, { method = "GET", body, auth = false, isRetry = false } = {}) {
  const headers = { "Content-Type": "application/json" };

  if (auth) {
    const token = getAccessToken();
    if (token) headers["Authorization"] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (response.status === 401 && auth) {
    if (!isRetry && getRefreshToken()) {
      try {
        await refreshAccessToken();
        return apiFetch(path, { method, body, auth, isRetry: true });
      } catch {
        clearTokens();
        redirectToLogin();
        throw new ApiError(401, "Sessiya bitib, yenidən daxil olun.");
      }
    }

    clearTokens();
    redirectToLogin();
    throw new ApiError(401, "Sessiya bitib, yenidən daxil olun.");
  }

  if (!response.ok) {
    throw await parseErrorBody(response);
  }

  if (response.status === 204) return null;
  return response.json();
}