export function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML.replaceAll('"', "&quot;").replaceAll("'", "&#39;");
}
