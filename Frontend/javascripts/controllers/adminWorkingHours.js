import { requireAdmin } from "../components/adminGuard.js";
import { getAllBuildings } from "../api/roomsApi.js";
import { getWorkingHours, updateWorkingHours } from "../api/workingHoursApi.js";
import { logout } from "../api/authApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

requireAdmin();

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");
const buildingTabs = document.getElementById("buildingTabs");
const weekCard = document.getElementById("weekCard");

const DAYS = [
  { key: "Monday", label: "Bazar ertəsi" },
  { key: "Tuesday", label: "Çərşənbə axşamı" },
  { key: "Wednesday", label: "Çərşənbə" },
  { key: "Thursday", label: "Cümə axşamı" },
  { key: "Friday", label: "Cümə" },
  { key: "Saturday", label: "Şənbə" },
  { key: "Sunday", label: "Bazar" },
];

let buildings = [];
let selectedBuildingId = "";
let hours = [];

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function renderBuildingTabs() {
  buildingTabs.innerHTML = buildings
    .map(
      (b) =>
        `<button type="button" class="wh-building-tab${b.id === selectedBuildingId ? " active" : ""}" data-id="${b.id}">
          <i class="bi bi-building"></i>${escapeHtml(b.name)}
        </button>`
    )
    .join("");

  buildingTabs.querySelectorAll(".wh-building-tab").forEach((btn) => {
    btn.addEventListener("click", () => selectBuilding(btn.dataset.id));
  });
}

async function selectBuilding(buildingId) {
  selectedBuildingId = buildingId;
  renderBuildingTabs();
  clearAlert(alertBox);

  try {
    const result = await getWorkingHours(buildingId);
    hours = DAYS.map((d) => {
      const row = result.find((r) => r.dayOfWeek === d.key);
      return {
        dayOfWeek: d.key,
        isWorkingDay: row ? row.isWorkingDay : true,
        startTime: row ? row.startTime.slice(0, 5) : "09:00",
        endTime: row ? row.endTime.slice(0, 5) : "18:00",
      };
    });
    renderWeek();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

function renderWeek() {
  const building = buildings.find((b) => b.id === selectedBuildingId);

  const rowsHtml = DAYS.map((d) => {
    const row = hours.find((h) => h.dayOfWeek === d.key);
    const isOff = !row.isWorkingDay;
    return `
      <div class="wh-row${isOff ? " is-off" : ""}" data-day="${d.key}">
        <span class="wh-day-name"><span class="wh-day-dot"></span>${d.label}</span>
        <span class="wh-switch-group">
          <label class="wh-switch">
            <input type="checkbox" class="day-toggle" data-day="${d.key}" ${row.isWorkingDay ? "checked" : ""} />
            <span class="track"></span>
          </label>
          <span class="wh-status-label ${row.isWorkingDay ? "on" : "off"}">${row.isWorkingDay ? "İş günü" : "Qeyri-iş günü"}</span>
        </span>
        ${
          isOff
            ? `<span class="wh-off-note">Bu gün rezervasiya üçün bağlıdır</span>`
            : `<span class="wh-time-range">
                <span class="wh-time-input"><i class="bi bi-sunrise"></i><input type="time" class="start-time" data-day="${d.key}" value="${row.startTime}" /></span>
                <span class="wh-time-sep">—</span>
                <span class="wh-time-input"><i class="bi bi-sunset"></i><input type="time" class="end-time" data-day="${d.key}" value="${row.endTime}" /></span>
              </span>`
        }
      </div>`;
  }).join("");

  weekCard.innerHTML = `
    <div class="card wh-card">
      <div class="wh-card-head">
        <h3><i class="bi bi-clock-history"></i>${escapeHtml(building ? building.name : "")} — həftəlik qrafik</h3>
        <button type="button" class="wh-copy-link" id="copyMondayBtn"><i class="bi bi-copy"></i>Bazar ertəsi saatlarını bütün iş günlərinə köçür</button>
      </div>
      ${rowsHtml}
      <div class="wh-card-foot">
        <span class="wh-hint"><i class="bi bi-lightbulb"></i>Dəyişikliklər yalnız bu binadakı otaqlara aiddir</span>
        <button type="button" class="btn btn-primary" id="saveWeekBtn"><i class="bi bi-check-lg"></i>Yadda saxla</button>
      </div>
    </div>`;

  weekCard.querySelectorAll(".day-toggle").forEach((el) => {
    el.addEventListener("change", () => {
      const row = hours.find((h) => h.dayOfWeek === el.dataset.day);
      row.isWorkingDay = el.checked;
      renderWeek();
    });
  });

  weekCard.querySelectorAll(".start-time").forEach((el) => {
    el.addEventListener("change", () => {
      hours.find((h) => h.dayOfWeek === el.dataset.day).startTime = el.value;
    });
  });

  weekCard.querySelectorAll(".end-time").forEach((el) => {
    el.addEventListener("change", () => {
      hours.find((h) => h.dayOfWeek === el.dataset.day).endTime = el.value;
    });
  });

  document.getElementById("copyMondayBtn").addEventListener("click", () => {
    const monday = hours.find((h) => h.dayOfWeek === "Monday");
    hours.forEach((h) => {
      if (h.dayOfWeek !== "Monday" && h.isWorkingDay) {
        h.startTime = monday.startTime;
        h.endTime = monday.endTime;
      }
    });
    renderWeek();
  });

  document.getElementById("saveWeekBtn").addEventListener("click", handleSave);
}

async function handleSave() {
  clearAlert(alertBox);
  const saveBtn = document.getElementById("saveWeekBtn");
  saveBtn.disabled = true;
  saveBtn.textContent = "Yadda saxlanılır...";

  const payload = hours.map((h) => ({
    DayOfWeek: h.dayOfWeek,
    IsWorkingDay: h.isWorkingDay,
    StartTime: h.isWorkingDay ? `${h.startTime}:00` : "00:00:00",
    EndTime: h.isWorkingDay ? `${h.endTime}:00` : "00:00:00",
  }));

  try {
    await updateWorkingHours(selectedBuildingId, payload);
    showAlert(alertBox, "İş saatları yadda saxlanıldı.", "success");
  } catch (err) {
    showAlert(alertBox, err.message);
  } finally {
    saveBtn.disabled = false;
    saveBtn.innerHTML = '<i class="bi bi-check-lg"></i>Yadda saxla';
  }
}

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "adminLogin.html";
});

async function init() {
  try {
    buildings = await getAllBuildings();
    if (!buildings.length) {
      buildingTabs.innerHTML = "";
      weekCard.innerHTML = `<div class="empty-state"><i class="bi bi-building"></i>Əvvəlcə bina əlavə edin.</div>`;
      return;
    }
    renderBuildingTabs();
    await selectBuilding(buildings[0].id);
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

init();