import { requireAdmin, getDisplayIdentity } from "../components/adminGuard.js";
import { initAdminMobileNav, initAdminAccountMenu } from "../components/mobileNav.js";
import { getAllBuildings } from "../api/roomsApi.js";
import { getWorkingHours, updateWorkingHours } from "../api/workingHoursApi.js";
import { logout } from "../api/authApi.js";
import { showAlert, clearAlert } from "../components/alert.js";
import { initDropdown } from "../components/customDropdown.js";
import { escapeHtml } from "../utils/escapeHtml.js";

requireAdmin();
initAdminMobileNav();
initAdminAccountMenu();

{
  const { initials, name } = getDisplayIdentity();
  const adminAccountInitials = document.getElementById("adminAccountInitials");
  const adminAccountName = document.getElementById("adminAccountName");
  if (adminAccountInitials) adminAccountInitials.textContent = initials;
  if (adminAccountName) adminAccountName.textContent = name;
}

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");
const buildingSelectBtn = document.getElementById("buildingSelectBtn");
const buildingSelectPanel = document.getElementById("buildingSelectPanel");
const weekCard = document.getElementById("weekCard");

const buildingDropdown = initDropdown({
  btn: buildingSelectBtn,
  panel: buildingSelectPanel,
  options: [],
  placeholder: "Seçin",
  onChange: (id) => selectBuilding(id),
});

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
function toMinutes(time) {
  const [h, m] = time.split(":").map(Number);
  return h * 60 + m;
}

function minutesToTime(minutes) {
  const h = String(Math.floor(minutes / 60)).padStart(2, "0");
  const m = String(minutes % 60).padStart(2, "0");
  return `${h}:${m}`;
}

async function selectBuilding(buildingId) {
  selectedBuildingId = buildingId;
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
    hours = [];
    weekCard.innerHTML = `<div class="empty-state"><i class="bi bi-exclamation-triangle"></i>İş saatları yüklənə bilmədi.</div>`;
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
            <input type="checkbox" class="day-toggle" data-day="${d.key}" aria-label="${d.label} iş günüdür" ${row.isWorkingDay ? "checked" : ""} />
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

  const workingHoursOnly = hours.filter((h) => h.isWorkingDay);
  const workingDaysCount = workingHoursOnly.length;
  const totalMinutes = workingHoursOnly.reduce((sum, h) => sum + (toMinutes(h.endTime) - toMinutes(h.startTime)), 0);
  const totalHoursLabel = (totalMinutes / 60).toFixed(1).replace(/\.0$/, "");
  const earliestStart = workingDaysCount
    ? minutesToTime(Math.min(...workingHoursOnly.map((h) => toMinutes(h.startTime))))
    : "—";
  const latestEnd = workingDaysCount
    ? minutesToTime(Math.max(...workingHoursOnly.map((h) => toMinutes(h.endTime))))
    : "—";

  weekCard.innerHTML = `
    <div class="wh-week-layout">
      <div class="card wh-card">
        <div class="wh-card-head">
          <h3><i class="bi bi-clock-history"></i>${escapeHtml(building ? building.name : "")} — həftəlik qrafik</h3>
          <button type="button" class="wh-copy-link" id="copyMondayBtn"><i class="bi bi-copy"></i>Bazar ertəsi saatlarını bütün iş günlərinə köçür</button>
        </div>
        ${rowsHtml}
      </div>
      <div class="wh-summary-col">
      <div class="wh-summary">
        <p class="wh-summary-label">Bu həftə</p>
        <div class="wh-summary-divider"></div>
        <div class="wh-summary-item">
          <span class="stat-icon violet"><i class="bi bi-calendar-check"></i></span>
          <div>
            <p class="wh-summary-value">${workingDaysCount}</p>
            <p class="wh-summary-item-label">iş günü</p>
          </div>
        </div>
        <div class="wh-summary-item">
          <span class="stat-icon amber"><i class="bi bi-hourglass-split"></i></span>
          <div>
            <p class="wh-summary-value">${totalHoursLabel}</p>
            <p class="wh-summary-item-label">saat/həftə</p>
          </div>
        </div>
        <div class="wh-summary-divider"></div>
        <div class="wh-summary-item">
          <span class="stat-icon green"><i class="bi bi-sunrise"></i></span>
          <div>
            <p class="wh-summary-value">${earliestStart}</p>
            <p class="wh-summary-item-label">ən erkən başlama</p>
          </div>
        </div>
        <div class="wh-summary-item">
          <span class="stat-icon green"><i class="bi bi-sunset"></i></span>
          <div>
            <p class="wh-summary-value">${latestEnd}</p>
            <p class="wh-summary-item-label">ən gec bitmə</p>
          </div>
        </div>
      </div>
      <div class="card wh-actions-bar">
        <span class="wh-hint"><i class="bi bi-lightbulb"></i>Dəyişikliklər yalnız bu binadakı otaqlara aiddir</span>
        <button type="button" class="btn btn-primary" id="saveWeekBtn"><i class="bi bi-check-lg"></i>Yadda saxla</button>
      </div>
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
      renderWeek();
    });
  });

  weekCard.querySelectorAll(".end-time").forEach((el) => {
    el.addEventListener("change", () => {
      hours.find((h) => h.dayOfWeek === el.dataset.day).endTime = el.value;
      renderWeek();
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

  const invalidDay = hours.find((h) => h.isWorkingDay && h.startTime >= h.endTime);
  if (invalidDay) {
    const label = DAYS.find((d) => d.key === invalidDay.dayOfWeek)?.label ?? invalidDay.dayOfWeek;
    showAlert(alertBox, `${label}: başlama saatı bitmə saatından əvvəl olmalıdır.`);
    return;
  }

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
  weekCard.innerHTML = `<div class="empty-state"><i class="bi bi-hourglass-split"></i>Yüklənir...</div>`;

  try {
    buildings = await getAllBuildings();
    if (!buildings.length) {
      buildingDropdown.setOptions([], "");
      weekCard.innerHTML = `<div class="empty-state"><i class="bi bi-building"></i>Əvvəlcə bina əlavə edin.</div>`;
      return;
    }
    buildingDropdown.setOptions(
      buildings.map((b) => ({ value: b.id, label: b.name })),
      buildings[0].id
    );
    await selectBuilding(buildings[0].id);
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

init();