import { apiFetch, setTokens, clearTokens } from "./apiClient.js";

export function register(dto) {
  return apiFetch("/auths/register", { method: "POST", body: dto });
}

export function confirmEmail(dto) {
  return apiFetch("/auths/confirm-email", { method: "POST", body: dto });
}

export function resendConfirmationEmail(dto) {
  return apiFetch("/auths/resend-confirmation-email", { method: "POST", body: dto });
}

export async function userLogin(dto) {
  const tokens = await apiFetch("/auths/user/login", { method: "POST", body: dto });
  setTokens(tokens);
  return tokens;
}

export function adminLogin(dto) {
  return apiFetch("/auths/admin/login", { method: "POST", body: dto });
}

export async function verifyAdmin2FA(dto) {
  const tokens = await apiFetch("/auths/admin/login-2fa", { method: "POST", body: dto });
  setTokens(tokens);
  return tokens;
}

export function forgotPassword(dto) {
  return apiFetch("/auths/forgot-password", { method: "POST", body: dto });
}

export function resetPassword(dto) {
  return apiFetch("/auths/reset-password", { method: "POST", body: dto });
}

export async function logout() {
  try {
    await apiFetch("/auths/logout", { method: "POST", auth: true });
  } finally {
    clearTokens();
  }
}