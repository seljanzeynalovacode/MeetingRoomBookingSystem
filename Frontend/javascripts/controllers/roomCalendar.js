import { isLoggedIn, getAccessToken, API_BASE_URL } from "../api/apiClient.js";
import { getAllRooms, getAllBuildings, getAllFloors } from "../api/roomsApi.js";
import { getRoomCalendar } from "../api/calendarApi.js";
import { getWorkingHours } from "../api/workingHoursApi.js";
import { logout } from "../api/authApi.js";
import { openBookingModal } from "../components/bookingModal.js";
import { showAlert, clearAlert } from "../components/alert.js";
import { initNotificationBell } from "../components/notificationBell.js?v=2";
import { initChatWidget, clearChatState } from "../components/chatWidget.js";
import { initDropdown } from "../components/customDropdown.js";
import { initMobileNav } from "../components/mobileNav.js";
import { initThemeToggle } from "../components/theme.js";
import { escapeHtml } from "../utils/escapeHtml.js";
import { t, getLang } from "../components/i18n.js";
import { floorLabel } from "../utils/floorLabel.js";

if (!isLoggedIn()) {
  window.location.href = "landing.html";
  throw new Error("Not authenticated");
}

initNotificationBell();
initChatWidget();
initThemeToggle(document.getElementById("themeToggleBtn"));
initMobileNav();

let GRID_START_HOUR = 7;
let GRID_END_HOUR = 22;
let HOURS = Array.from({ length: GRID_END_HOUR - GRID_START_HOUR }, (_, i) => GRID_START_HOUR + i);

function toMinutes(time) {
  const [h, m] = time.split(":").map(Number);
  return h * 60 + m;
}

const hubConnection = new signalR.HubConnectionBuilder()
  .withUrl(`${API_BASE_URL.replace("/api", "")}/hubs/room-calendar`, {
    accessTokenFactory: () => getAccessToken(),
  })
  .withAutomaticReconnect()
  .build();

let joinedRoomId = "";

async function joinRoomGroup(roomId) {
  if (joinedRoomId === roomId) return;
  if (joinedRoomId) {
    try {
      await hubConnection.invoke("LeaveRoom", joinedRoomId);
    } catch {
      // bağlantı kəsilibsə, sakitcə keç
    }
  }
  joinedRoomId = roomId;
  if (roomId && hubConnection.state === signalR.HubConnectionState.Connected) {
    await hubConnection.invoke("JoinRoom", roomId);
  }
}

hubConnection.on("RoomCalendarChanged", () => {
  fetchAndRender();
});

hubConnection.onreconnecting(() => {
  showAlert(alertBox, "Canlı yeniləmə bağlantısı kəsildi, yenidən qoşulmağa çalışılır...", "warning");
});

hubConnection.onreconnected(() => {
  clearAlert(alertBox);
  if (joinedRoomId) hubConnection.invoke("JoinRoom", joinedRoomId);
});

hubConnection.onclose(() => {
  showAlert(alertBox, "Canlı yeniləmə bağlantısı kəsildi. Yeni dəyişiklikləri görmək üçün səhifəni yeniləyin.", "warning");
});

async function updateGridHoursForBuilding(buildingId) {
  if (!buildingId) return;
  try {
    const hours = await getWorkingHours(buildingId);
    const workingDays = hours.filter((h) => h.isWorkingDay);
    if (!workingDays.length) return;

    const minStart = Math.min(...workingDays.map((h) => toMinutes(h.startTime)));
    const maxEnd = Math.max(...workingDays.map((h) => toMinutes(h.endTime)));

    GRID_START_HOUR = Math.floor(minStart / 60);
    GRID_END_HOUR = Math.max(Math.ceil(maxEnd / 60), GRID_START_HOUR + 1);
    HOURS = Array.from({ length: GRID_END_HOUR - GRID_START_HOUR }, (_, i) => GRID_START_HOUR + i);
  } catch {
    // sakitcə keç, defolt saatlarla davam et
  }
}

const alertBox = document.getElementById("alertBox");
const roomInfoName = document.getElementById("roomInfoName");
const roomInfoLocation = document.getElementById("roomInfoLocation");
const dayViewBtn = document.getElementById("dayViewBtn");
const weekViewBtn = document.getElementById("weekViewBtn");
const prevBtn = document.getElementById("prevBtn");
const nextBtn = document.getElementById("nextBtn");
const todayBtn = document.getElementById("todayBtn");
const dateLabelEl = document.getElementById("dateLabel");
const contentEl = document.getElementById("calendarContent");

const avatarChip = document.getElementById("avatarChip");
const avatarMenu = document.getElementById("avatarMenu");
const avatarInitials = document.getElementById("avatarInitials");
const avatarName = document.getElementById("avatarName");
const logoutBtn = document.getElementById("logoutBtn");

let rooms = [];
let buildings = [];
let floors = [];
let selectedRoomId = "";
let selectedRoomName = "";
let viewMode = "day";
let anchorDate = new Date();
function capitalize(s) {
  return s ? s.charAt(0).toUpperCase() + s.slice(1) : s;
}

function toDateOnlyString(date) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function getWeekStart(date) {
  const d = new Date(date);
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  d.setDate(d.getDate() + diff);
  d.setHours(0, 0, 0, 0);
  return d;
}

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

function getDisplayIdentity() {
  const token = getAccessToken();
  if (!token) return { initials: "--", name: "İstifadəçi" };

  const claims = parseJwt(token) || {};
  const givenName = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"] || "";
  const surname = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname"] || "";
  const email = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || claims["email"] || "";

  const name = givenName ? `${givenName} ${surname}`.trim() : email ? email.split("@")[0] : "İstifadəçi";
  const initials = givenName && surname ? (givenName[0] + surname[0]).toUpperCase() : name.slice(0, 2).toUpperCase();

  return { initials, name };
}

function getRoles() {
  const token = getAccessToken();
  if (!token) return [];
  const claims = parseJwt(token) || {};
  const roleClaim = claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  if (!roleClaim) return [];
  return Array.isArray(roleClaim) ? roleClaim : [roleClaim];
}

function buildTimelineItems(dayEntry) {
  const items = [];
  (dayEntry.reservations || []).forEach((r) => {
    items.push({
      type: "reservation",
      start: r.startDate,
      end: r.endDate,
      title: r.title,
      meta: r.reservedByUserName,
      status: r.status,
    });
  });
  (dayEntry.blackouts || []).forEach((b) => {
    items.push({
      type: "blackout",
      start: b.startDate,
      end: b.endDate,
      title: b.reason,
      meta: "Bloklanıb",
    });
  });
  items.sort((a, b) => new Date(a.start) - new Date(b.start));
  return items;
}

function clampHour(h) {
  return Math.min(Math.max(h, GRID_START_HOUR), GRID_END_HOUR);
}

function isMultiDayItem(item) {
  return toDateOnlyString(new Date(item.start)) !== toDateOnlyString(new Date(item.end));
}

function getEventLayout(item) {
  const start = new Date(item.start);
  const end = new Date(item.end);
  const totalHours = GRID_END_HOUR - GRID_START_HOUR;
  const startHourFloat = clampHour(start.getHours() + start.getMinutes() / 60);
  const endHourFloat = clampHour(end.getHours() + end.getMinutes() / 60);
  const top = ((startHourFloat - GRID_START_HOUR) / totalHours) * 100;
  const height = Math.max(((endHourFloat - startHourFloat) / totalHours) * 100, 2.5);

  const startLabel = start.toTimeString().slice(0, 5);
  const endLabel = end.toTimeString().slice(0, 5);

  return { top: `${top}%`, height: `${height}%`, startLabel, timeLabel: `${startLabel}–${endLabel}` };
}

function applyBlockedRows(container, blackoutItems, dayDateStr) {
  const rows = Array.from(container.querySelectorAll(".grid-hour-row"));
  const dayStart = new Date(dayDateStr);
  dayStart.setHours(0, 0, 0, 0);
  const dayEnd = new Date(dayStart);
  dayEnd.setDate(dayEnd.getDate() + 1);

  blackoutItems.forEach((item) => {
    const start = new Date(item.start);
    const end = new Date(item.end);
    const startHour = start < dayStart ? GRID_START_HOUR : Math.floor(start.getHours() + start.getMinutes() / 60);
    const endHour = end > dayEnd ? GRID_END_HOUR : Math.ceil(end.getHours() + end.getMinutes() / 60);

    const blockedRows = rows.filter((row) => {
      const h = Number(row.dataset.hour);
      return h >= startHour && h < endHour;
    });

    blockedRows.forEach((row, idx) => {
      row.classList.add("blocked");
      row.title = item.title;
      row.addEventListener("click", (e) => {
        e.stopPropagation();
        showEventPopover(item, row);
      });
      if (idx === blockedRows.length - 1) row.classList.add("blocked-last");
      if (idx === 0) {
        row.classList.add("blocked-first");
        const tag = document.createElement("span");
        tag.className = "blocked-tag";
        tag.innerHTML = `<i class="bi bi-slash-circle"></i>${escapeHtml(item.title)}`;
        row.appendChild(tag);
      }
    });
  });
}

let popoverOutsideHandler = null;
function closeEventPopover() {
  const existing = document.getElementById("eventPopover");
  if (existing) existing.remove();
  if (popoverOutsideHandler) {
    document.removeEventListener("click", popoverOutsideHandler);
    popoverOutsideHandler = null;
  }
}

function showEventPopover(item, anchorEl) {
  closeEventPopover();
  const rect = anchorEl.getBoundingClientRect();

  const pop = document.createElement("div");
  pop.className = "event-popover";
  pop.id = "eventPopover";

  const startDateObj = new Date(item.start);
  const endDateObj = new Date(item.end);
  const start = startDateObj.toTimeString().slice(0, 5);
  const end = endDateObj.toTimeString().slice(0, 5);
  const locale = getLang() === "en" ? "en-US" : "az-AZ";
  const sameDay = toDateOnlyString(startDateObj) === toDateOnlyString(endDateObj);
  const dateStr = sameDay
    ? startDateObj.toLocaleDateString(locale, { day: "2-digit", month: "short", year: "numeric" })
    : `${startDateObj.toLocaleDateString(locale, { day: "2-digit", month: "short" })} – ${endDateObj.toLocaleDateString(locale, { day: "2-digit", month: "short", year: "numeric" })}`;

  const currentRoom = rooms.find((r) => r.id === selectedRoomId);
  const buildingName = currentRoom ? currentRoom.buildingName : "";

  const bodyHtml =
    item.type === "blackout"
      ? `<div class="ep-row"><i class="bi bi-slash-circle"></i>Bloklanıb</div><div class="ep-row"><i class="bi bi-card-text"></i>${escapeHtml(item.title)}</div>`
      : `<div class="ep-row"><i class="bi bi-person"></i>${escapeHtml(item.meta)}</div><div class="ep-row"><i class="bi bi-info-circle"></i>${escapeHtml(new Date(item.end) < new Date() ? "Tamamlanıb" : "Təsdiqlənib")}</div>`;

  pop.innerHTML = `
    <div class="ep-head">
      <strong>${escapeHtml(item.title)}</strong>
      <button type="button" class="ep-close" aria-label="Bağla"><i class="bi bi-x"></i></button>
    </div>
    <div class="ep-row"><i class="bi bi-building"></i>${escapeHtml(buildingName)} · ${escapeHtml(selectedRoomName)}</div>
    <div class="ep-row"><i class="bi bi-calendar3"></i>${dateStr}</div>
    <div class="ep-time"><i class="bi bi-clock"></i>${start}–${end}</div>
    ${bodyHtml}
  `;
  document.body.appendChild(pop);

  const top = window.scrollY + rect.bottom + 8;
  const maxLeft = window.scrollX + document.documentElement.clientWidth - 276;
  const left = Math.min(window.scrollX + rect.left, Math.max(maxLeft, 12));
  pop.style.top = `${top}px`;
  pop.style.left = `${left}px`;

  pop.querySelector(".ep-close").addEventListener("click", closeEventPopover);

  popoverOutsideHandler = (e) => {
    if (!pop.contains(e.target)) closeEventPopover();
  };
  setTimeout(() => document.addEventListener("click", popoverOutsideHandler), 0);
}

function openSlotBooking(dateStr, hour) {
  if (!selectedRoomId) return;
  const startStr = `${String(hour).padStart(2, "0")}:00`;
  const endStr = `${String(Math.min(hour + 1, 23)).padStart(2, "0")}:00`;
  const currentRoom = rooms.find((r) => r.id === selectedRoomId);

  openBookingModal({
    mode: "create",
    isAdmin: getRoles().includes("Admin"),
    room: { id: selectedRoomId, name: selectedRoomName, buildingId: currentRoom ? currentRoom.buildingId : null },
    prefillDate: dateStr,
    prefillStart: startStr,
    prefillEnd: endStr,
    onSuccess: () => fetchAndRender(),
  });
}

function renderDayView(calendarDays) {
  const dayEntry = calendarDays[0];
  const dateStr = dayEntry.date.slice(0, 10);
  const allItems = buildTimelineItems(dayEntry);
  const multiDayBlackouts = allItems.filter((item) => item.type === "blackout" && isMultiDayItem(item));
  const overlayItems = allItems.filter((item) => !(item.type === "blackout" && isMultiDayItem(item)));

  const labelsHtml = HOURS.map((h) => `<div class="hour-label">${String(h).padStart(2, "0")}:00</div>`).join("");
  const rowsHtml = HOURS.map((h) => `<div class="grid-hour-row" data-hour="${h}" data-date="${dateStr}"></div>`).join("");

  contentEl.innerHTML = `
    <div class="day-grid-wrap">
      <div class="hour-labels">${labelsHtml}</div>
      <div class="day-grid-col" id="dayGridCol">
        ${rowsHtml}
        <div class="grid-events-layer" id="dayEventsLayer"></div>
      </div>
    </div>`;

  const dayGridCol = document.getElementById("dayGridCol");
  applyBlockedRows(dayGridCol, multiDayBlackouts, dateStr);

  const layer = document.getElementById("dayEventsLayer");
  overlayItems.forEach((item) => {
    const { top, height, timeLabel } = getEventLayout(item);
    const block = document.createElement("div");
    block.className = item.type === "blackout" ? "grid-event-block blackout" : "grid-event-block";
    block.style.top = top;
    block.style.height = height;
    block.innerHTML = `${escapeHtml(item.title)}<span class="ge-time">${timeLabel}</span>`;
    block.addEventListener("click", (e) => {
      e.stopPropagation();
      showEventPopover(item, block);
    });
    layer.appendChild(block);
  });

  dayGridCol.querySelectorAll(".grid-hour-row:not(.blocked)").forEach((row) => {
    row.addEventListener("click", () => {
      openSlotBooking(row.dataset.date, Number(row.dataset.hour));
    });
  });
}

function renderWeekView(calendarDays) {
  const labelsHtml = HOURS.map((h) => `<div class="hour-label">${String(h).padStart(2, "0")}:00</div>`).join("");

  const columnsHtml = calendarDays
    .map((dayEntry, colIdx) => {
      const date = new Date(dayEntry.date);
      const dayName = capitalize(date.toLocaleDateString((getLang() === "en" ? "en-US" : "az-AZ"), { weekday: "short" }));
      const dateLabel = date.toLocaleDateString((getLang() === "en" ? "en-US" : "az-AZ"), { day: "2-digit", month: "short" });
      const dateStr = dayEntry.date.slice(0, 10);
      const rowsHtml = HOURS.map((h) => `<div class="grid-hour-row" data-hour="${h}" data-date="${dateStr}"></div>`).join("");

      return `
        <div class="week-day-column">
          <div class="week-day-header">${dayName}<span class="wdh-date">${dateLabel}</span></div>
          <div class="week-day-body" data-col="${colIdx}">
            ${rowsHtml}
            <div class="grid-events-layer" data-layer-col="${colIdx}"></div>
          </div>
        </div>`;
    })
    .join("");

  contentEl.innerHTML = `
    <div class="week-grid-wrap">
      <div class="week-hour-labels">${labelsHtml}</div>
      <div class="week-days-wrap">${columnsHtml}</div>
    </div>`;

  calendarDays.forEach((dayEntry, colIdx) => {
    const dateStr = dayEntry.date.slice(0, 10);
    const allItems = buildTimelineItems(dayEntry);
    const multiDayBlackouts = allItems.filter((item) => item.type === "blackout" && isMultiDayItem(item));
    const overlayItems = allItems.filter((item) => !(item.type === "blackout" && isMultiDayItem(item)));

    const dayBody = contentEl.querySelector(`.week-day-body[data-col="${colIdx}"]`);
    applyBlockedRows(dayBody, multiDayBlackouts, dateStr);

    const layer = contentEl.querySelector(`.grid-events-layer[data-layer-col="${colIdx}"]`);
    overlayItems.forEach((item) => {
      const { top, height, startLabel } = getEventLayout(item);
      const block = document.createElement("div");
      block.className = item.type === "blackout" ? "grid-event-block blackout week-block" : "grid-event-block week-block";
      block.style.top = top;
      block.style.height = height;
      block.innerHTML = `<span class="ge-time-only">${startLabel}</span>${escapeHtml(item.title)}`;
      block.addEventListener("click", (e) => {
        e.stopPropagation();
        showEventPopover(item, block);
      });
      layer.appendChild(block);
    });
  });

  contentEl.querySelectorAll(".grid-hour-row:not(.blocked)").forEach((row) => {
    row.addEventListener("click", () => {
      openSlotBooking(row.dataset.date, Number(row.dataset.hour));
    });
  });
}

function updateHeaderLabels(startDate, endDate) {
  if (viewMode === "day") {
    const d = new Date(startDate);
    dateLabelEl.textContent = d.toLocaleDateString((getLang() === "en" ? "en-US" : "az-AZ"), {
      day: "2-digit",
      month: "long",
      year: "numeric",
      weekday: "long",
    });
  } else {
    const s = new Date(startDate);
    const e = new Date(endDate);
    dateLabelEl.textContent = `${s.toLocaleDateString((getLang() === "en" ? "en-US" : "az-AZ"), { day: "2-digit", month: "short" })} – ${e.toLocaleDateString((getLang() === "en" ? "en-US" : "az-AZ"), { day: "2-digit", month: "short", year: "numeric" })}`;
  }

  const room = rooms.find((r) => r.id === selectedRoomId);
  if (room) {
    const floor = floors.find((f) => f.id === room.floorId);
    const floorText = floor ? floorLabel(floor.levelNumber) : room.floorName;
    roomInfoName.textContent = room.name;
    roomInfoLocation.textContent = `${room.buildingName}, ${floorText} · ${room.capacity} ${t("roomCalendar.capacity_suffix")}`;
  } else {
    roomInfoName.textContent = "";
    roomInfoLocation.textContent = "";
  }
}

let fetchToken = 0;

function setNavDisabled(disabled) {
  prevBtn.disabled = disabled;
  nextBtn.disabled = disabled;
  todayBtn.disabled = disabled;
  dayViewBtn.disabled = disabled;
  weekViewBtn.disabled = disabled;
}

async function fetchAndRender() {
  if (!selectedRoomId) return;
  clearAlert(alertBox);
  closeEventPopover();
  contentEl.innerHTML = `<div class="empty-state"><i class="bi bi-hourglass-split"></i>Yüklənir...</div>`;

  const myToken = ++fetchToken;
  setNavDisabled(true);

  const room = rooms.find((r) => r.id === selectedRoomId);
  const floor = room ? floors.find((f) => f.id === room.floorId) : null;
  await updateGridHoursForBuilding(floor ? floor.buildingId : null);
  await joinRoomGroup(selectedRoomId);
  if (myToken !== fetchToken) return;

  let startDate, endDate;
  if (viewMode === "day") {
    startDate = endDate = toDateOnlyString(anchorDate);
  } else {
    const weekStart = getWeekStart(anchorDate);
    const weekEnd = new Date(weekStart);
    weekEnd.setDate(weekEnd.getDate() + 6);
    startDate = toDateOnlyString(weekStart);
    endDate = toDateOnlyString(weekEnd);
  }

  updateHeaderLabels(startDate, endDate);

  try {
    const days = await getRoomCalendar(selectedRoomId, startDate, endDate);
    if (myToken !== fetchToken) return;
    if (viewMode === "day") renderDayView(days);
    else renderWeekView(days);
  } catch (err) {
    if (myToken !== fetchToken) return;
    showAlert(alertBox, err.message);
    contentEl.innerHTML = "";
  } finally {
    if (myToken === fetchToken) setNavDisabled(false);
  }
}

function setViewMode(mode) {
  viewMode = mode;
  dayViewBtn.classList.toggle("active", mode === "day");
  weekViewBtn.classList.toggle("active", mode === "week");
  fetchAndRender();
}

dayViewBtn.addEventListener("click", () => setViewMode("day"));
weekViewBtn.addEventListener("click", () => setViewMode("week"));

prevBtn.addEventListener("click", () => {
  anchorDate.setDate(anchorDate.getDate() - (viewMode === "day" ? 1 : 7));
  fetchAndRender();
});

nextBtn.addEventListener("click", () => {
  anchorDate.setDate(anchorDate.getDate() + (viewMode === "day" ? 1 : 7));
  fetchAndRender();
});

todayBtn.addEventListener("click", () => {
  anchorDate = new Date();
  fetchAndRender();
});

function getRoomsForBuilding(buildingId) {
  if (!buildingId) return rooms;
  return rooms.filter((r) => {
    const floor = floors.find((f) => f.id === r.floorId);
    return floor && floor.buildingId === buildingId;
  });
}

const LAST_ROOM_KEY = "mrb_last_room_id";
function saveLastRoom(roomId) {
  try {
    localStorage.setItem(LAST_ROOM_KEY, roomId);
  } catch {}
}
function getLastRoomId() {
  try {
    return localStorage.getItem(LAST_ROOM_KEY) || "";
  } catch {
    return "";
  }
}
function findBuildingIdForRoom(room) {
  const floor = floors.find((f) => f.id === room.floorId);
  return floor ? floor.buildingId : "";
}

function populateRoomOptions(buildingId) {
  const filtered = getRoomsForBuilding(buildingId);
  roomDropdown.setOptions(
    filtered.map((r) => ({
      value: r.id,
      label: buildingId ? r.name : `${r.name} — ${r.buildingName}`,
    })),
    filtered.length ? filtered[0].id : ""
  );
  return filtered;
}

const buildingDropdown = initDropdown({
  btn: document.getElementById("buildingSelectBtn"),
  panel: document.getElementById("buildingSelectPanel"),
  options: [],
  placeholder: t("calendar.all_buildings"),
  onChange: (value) => {
    const filtered = populateRoomOptions(value);
    if (!filtered.length) {
      selectedRoomId = "";
      selectedRoomName = "";
      roomInfoName.textContent = "";
      roomInfoLocation.textContent = "";
      contentEl.innerHTML = `<div class="empty-state"><i class="bi bi-door-closed"></i>${t("roomCalendar.no_rooms_in_building")}</div>`;
      return;
    }
    selectedRoomId = filtered[0].id;
    selectedRoomName = filtered[0].name;
    saveLastRoom(selectedRoomId);
    fetchAndRender();
  },
});

const roomDropdown = initDropdown({
  btn: document.getElementById("roomSelectBtn"),
  panel: document.getElementById("roomSelectPanel"),
  options: [],
  placeholder: t("common.select"),
  onChange: (value) => {
    selectedRoomId = value;
    const room = rooms.find((r) => r.id === selectedRoomId);
    selectedRoomName = room ? room.name : "";
    saveLastRoom(selectedRoomId);
    fetchAndRender();
  },
});

avatarChip.addEventListener("click", (e) => {
  e.stopPropagation();
  avatarMenu.classList.toggle("open");
});
document.addEventListener("click", () => avatarMenu.classList.remove("open"));

logoutBtn.addEventListener("click", async () => {
  await logout();
  clearChatState();
  window.location.href = "landing.html";
});

async function init() {
  try {
    await hubConnection.start();
  } catch (err) {
    console.error("SignalR bağlantısı qurula bilmədi:", err);
  }

  const { initials, name } = getDisplayIdentity();
  avatarInitials.textContent = initials;
  avatarName.textContent = name;

  if (getRoles().includes("Admin")) {
    document.getElementById("adminPanelLink").style.display = "";
  }

  try {
    const [roomList, buildingList, floorList] = await Promise.all([getAllRooms(), getAllBuildings(), getAllFloors()]);
    rooms = roomList;
    buildings = buildingList;
    floors = floorList;

    if (!rooms.length) {
      contentEl.innerHTML = `<div class="empty-state"><i class="bi bi-door-closed"></i>Heç bir otaq tapılmadı.</div>`;
      return;
    }

    const lastRoomId = getLastRoomId();
    const lastRoom = lastRoomId ? rooms.find((r) => r.id === lastRoomId) : null;
    const initialBuildingId = lastRoom ? findBuildingIdForRoom(lastRoom) : buildings[0]?.id || "";

    buildingDropdown.setOptions(buildings.map((b) => ({ value: b.id, label: b.name })), initialBuildingId);

    const filtered = populateRoomOptions(initialBuildingId);
    const initialRoom = (lastRoom && filtered.find((r) => r.id === lastRoom.id)) || filtered[0];
    if (initialRoom) {
      roomDropdown.setValue(initialRoom.id);
      selectedRoomId = initialRoom.id;
      selectedRoomName = initialRoom.name;
      fetchAndRender();
    } else {
      contentEl.innerHTML = `<div class="empty-state"><i class="bi bi-door-closed"></i>${t("roomCalendar.no_rooms_in_building")}</div>`;
    }
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

init();