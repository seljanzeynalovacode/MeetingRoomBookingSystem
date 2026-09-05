import { escapeHtml } from "../utils/escapeHtml.js";

const ICONS = {
  danger: "bi-exclamation-circle",
  success: "bi-check-circle",
  warning: "bi-exclamation-triangle",
};

export function showAlert(container, message, type = "danger") {
  const icon = ICONS[type] || ICONS.danger;
  container.innerHTML = `<div class="alert alert-${type}" role="alert"><i class="bi ${icon}"></i><span>${escapeHtml(message)}</span></div>`;
}

export function clearAlert(container) {
  container.innerHTML = "";
}

export function clearFieldErrors(form) {
  form.querySelectorAll(".field-error").forEach((el) => el.classList.remove("field-error"));
  form.querySelectorAll(".field-error-msg").forEach((el) => el.remove());
}

export function showFieldErrors(form, fieldErrors) {
  for (const fieldName in fieldErrors) {
    const input = form.querySelector(`[name="${fieldName}"]`);
    if (!input) continue;
    input.classList.add("field-error");
    const feedback = document.createElement("div");
    feedback.className = "field-error-msg";
    feedback.textContent = fieldErrors[fieldName].join(" ");
    const wrap = input.closest(".input-wrap") || input;
    wrap.insertAdjacentElement("afterend", feedback);
  }
}