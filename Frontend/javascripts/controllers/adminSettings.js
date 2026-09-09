import { requireAdmin, getDisplayIdentity } from "../components/adminGuard.js";
import { initAdminMobileNav, initAdminAccountMenu } from "../components/mobileNav.js";
import { getSystemSettings, updateSystemSettings, resetSystemSettingsToGlobal } from "../api/systemSettingsApi.js";
import { getAllBuildings } from "../api/roomsApi.js";
import { logout } from "../api/authApi.js";
import { showAlert, clearAlert } from "../components/alert.js";
import { initDropdown } from "../components/customDropdown.js";
import { initThemeToggle } from "../components/theme.js";
import { t } from "../components/i18n.js";

requireAdmin();
initAdminMobileNav();
initAdminAccountMenu();
initThemeToggle(document.getElementById("themeToggleBtn"));

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
const settingsInfoBanner = document.getElementById("settingsInfoBanner");
const settingsInfoText = document.getElementById("settingsInfoText");
const resetToGlobalBtn = document.getElementById("resetToGlobalBtn");

const minMinutesInput = document.getElementById("minMinutesInput");
const maxHoursInput = document.getElementById("maxHoursInput");
const bufferMinutesInput = document.getElementById("bufferMinutesInput");
const horizonDaysInput = document.getElementById("horizonDaysInput");
const limitToggle = document.getElementById("limitToggle");
const maxActiveGroup = document.getElementById("maxActiveGroup");
const maxActiveInput = document.getElementById("maxActiveInput");
const calendarRangeInput = document.getElementById("calendarRangeInput");
const saveBtn = document.getElementById("saveBtn");

function updateMaxActiveState() {
  maxActiveGroup.classList.toggle("disabled", !limitToggle.checked);
}

limitToggle.addEventListener("change", updateMaxActiveState);

let selectedBuildingId = "";

const buildingDropdown = initDropdown({
  btn: buildingSelectBtn,
  panel: buildingSelectPanel,
  options: [{ value: "", label: t("adminsettings.global") }],
  value: "",
  onChange: (id) => {
    selectedBuildingId = id;
    loadSettings();
  },
});

function applyValues(s) {
  minMinutesInput.value = s.minReservationMinutes;
  maxHoursInput.value = s.maxReservationHours;
  bufferMinutesInput.value = s.bufferMinutes;
  horizonDaysInput.value = s.bookingHorizonDays;
  limitToggle.checked = s.isActiveReservationLimitEnabled;
  maxActiveInput.value = s.maxActiveReservationsPerUser;
  calendarRangeInput.value = s.maxCalendarRangeDays;
  updateMaxActiveState();
}

async function loadSettings() {
  saveBtn.disabled = true;
  saveBtn.textContent = t("common.loading");
  resetToGlobalBtn.hidden = true;

  try {
    const s = await getSystemSettings(selectedBuildingId || undefined);
    applyValues(s);

    if (!selectedBuildingId) {
      settingsInfoBanner.classList.remove("is-fallback");
      settingsInfoText.textContent = t("adminsettings.info_global");
    } else if (s.buildingId === selectedBuildingId) {
      settingsInfoBanner.classList.remove("is-fallback");
      settingsInfoText.textContent = t("adminsettings.info_custom");
      resetToGlobalBtn.hidden = false;
    } else {
      settingsInfoBanner.classList.add("is-fallback");
      settingsInfoText.textContent = t("adminsettings.info_fallback");
    }
  } catch (err) {
    showAlert(alertBox, err.message);
  } finally {
    saveBtn.disabled = false;
    saveBtn.innerHTML = `<i class="bi bi-check-lg"></i>${t("adminsettings.save_all")}`;
  }
}

resetToGlobalBtn.addEventListener("click", async () => {
  clearAlert(alertBox);
  resetToGlobalBtn.disabled = true;
  try {
    await resetSystemSettingsToGlobal(selectedBuildingId);
    showAlert(alertBox, t("adminsettings.reset_success"), "success");
    await loadSettings();
  } catch (err) {
    showAlert(alertBox, err.message);
  } finally {
    resetToGlobalBtn.disabled = false;
  }
});

const numberInputs = [
  minMinutesInput,
  maxHoursInput,
  bufferMinutesInput,
  horizonDaysInput,
  maxActiveInput,
  calendarRangeInput,
];

saveBtn.addEventListener("click", async () => {
  clearAlert(alertBox);

  const isValid = numberInputs.every((input) => input.reportValidity());
  if (!isValid) {
    return;
  }

  saveBtn.disabled = true;
  saveBtn.textContent = t("common.saving");

  const dto = {
    MinReservationMinutes: Number(minMinutesInput.value),
    MaxReservationHours: Number(maxHoursInput.value),
    BufferMinutes: Number(bufferMinutesInput.value),
    BookingHorizonDays: Number(horizonDaysInput.value),
    IsActiveReservationLimitEnabled: limitToggle.checked,
    MaxActiveReservationsPerUser: Number(maxActiveInput.value),
    MaxCalendarRangeDays: Number(calendarRangeInput.value),
  };

  try {
    await updateSystemSettings(selectedBuildingId || undefined, dto);
    showAlert(alertBox, t("adminsettings.save_success"), "success");
    await loadSettings();
  } catch (err) {
    showAlert(alertBox, err.message);
  } finally {
    saveBtn.disabled = false;
    saveBtn.innerHTML = `<i class="bi bi-check-lg"></i>${t("adminsettings.save_all")}`;
  }
});

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "adminLogin.html";
});

async function init() {
  try {
    const buildings = await getAllBuildings();
    buildingDropdown.setOptions(
      [{ value: "", label: t("adminsettings.global") }, ...buildings.map((b) => ({ value: b.id, label: b.name }))],
      ""
    );
  } catch (err) {
    showAlert(alertBox, err.message);
  }
  await loadSettings();
}

init();
