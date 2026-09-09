import { sendChatMessage } from "../api/chatApi.js";
import { escapeHtml } from "../utils/escapeHtml.js";
import { t } from "./i18n.js";

const STORAGE_KEY = "mrb_chat_state";
function formatMessageText(text) {
  const lines = escapeHtml(text).split("\n");
  const introIndex = lines.findIndex((line) => line.trim().endsWith(":"));
  if (introIndex === -1) return lines.join("<br>");
  return lines
    .map((line, i) => (i > introIndex && line.trim() ? `• ${line}` : line))
    .join("<br>");
}

function loadState() {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

function saveState(messages, history) {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ messages, history }));
  } catch {
    // sessionStorage yazıla bilmirsə, sakitcə keç
  }
}

export function clearChatState() {
  try {
    sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    // sakitcə keç
  }
}

export function initChatWidget() {
  if (document.getElementById("chatWidgetFab")) return;

  const wrap = document.createElement("div");
  wrap.innerHTML = `
    <button type="button" class="chat-widget-fab" id="chatWidgetFab" aria-label="Chatbot">
      <i class="bi bi-robot"></i>
    </button>
    <div class="chat-dock" id="chatDock">
      <div class="chat-widget-head">
        <div class="chat-widget-head-icon"><i class="bi bi-robot"></i></div>
        <div class="chat-widget-head-text">
          <div class="chat-widget-head-title">MeetAI</div>
        </div>
        <button type="button" class="chat-widget-close" id="chatWidgetClose" aria-label="Bağla"><i class="bi bi-x-lg"></i></button>
      </div>
      <div class="chat-widget-messages" id="chatWidgetMessages"></div>
      <form class="chat-widget-input-row" id="chatWidgetForm">
        <input type="text" class="input" id="chatWidgetInput" placeholder="${t("chatwidget.placeholder")}" autocomplete="off" />
        <button type="submit" class="chat-widget-send" id="chatWidgetSend" aria-label="Göndər"><i class="bi bi-send"></i></button>
      </form>
    </div>
  `;
  while (wrap.firstChild) document.body.appendChild(wrap.firstChild);

  const fab = document.getElementById("chatWidgetFab");
  const dock = document.getElementById("chatDock");
  const closeBtn = document.getElementById("chatWidgetClose");
  const messagesEl = document.getElementById("chatWidgetMessages");
  const form = document.getElementById("chatWidgetForm");
  const input = document.getElementById("chatWidgetInput");
  const sendBtn = document.getElementById("chatWidgetSend");

  const saved = loadState();
  let messages = saved?.messages?.length ? saved.messages : [{ role: "bot", text: t("chatwidget.welcome") }];
  let history = saved?.history || [];
  let sending = false;

  function renderMessages() {
    messagesEl.innerHTML = messages
      .map((m) => {
        if (m.typing) {
          return `<div class="chat-msg-row bot">
            <div class="chat-msg-avatar"><i class="bi bi-robot"></i></div>
            <div class="chat-msg-bubble typing"><span></span><span></span><span></span></div>
          </div>`;
        }
        const avatar = m.role === "user" ? "" : `<div class="chat-msg-avatar"><i class="bi bi-robot"></i></div>`;
        return `<div class="chat-msg-row ${m.role === "user" ? "user" : "bot"}">
          ${avatar}
          <div class="chat-msg-bubble">${formatMessageText(m.text)}</div>
        </div>`;
      })
      .join("");
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  function addMessage(role, text) {
    messages.push({ role, text });
    renderMessages();
    saveState(messages, history);
  }

  function setSending(state) {
    sending = state;
    sendBtn.disabled = state;
    input.disabled = state;
  }

  async function sendText(text) {
    if (!text || sending) return;

    addMessage("user", text);
    setSending(true);

    messages.push({ role: "bot", text: "", typing: true });
    renderMessages();

    try {
      const result = await sendChatMessage({ Message: text, History: history.slice(-30) });
      messages.pop();
      history = result.history || [];
      addMessage("bot", result.reply);
    } catch (err) {
      messages.pop();
      addMessage("bot", err.message || t("chatwidget.error"));
    } finally {
      setSending(false);
      input.focus();
    }
  }

  renderMessages();

  function setOpen(isOpen) {
    dock.classList.toggle("open", isOpen);
    fab.classList.toggle("open", isOpen);
    document.body.classList.toggle("chat-dock-open", isOpen);
    if (isOpen) input.focus();
  }

  fab.addEventListener("click", () => setOpen(!dock.classList.contains("open")));
  closeBtn.addEventListener("click", () => setOpen(false));

  form.addEventListener("submit", (e) => {
    e.preventDefault();
    const text = input.value.trim();
    input.value = "";
    sendText(text);
  });
}
