import { requireAdmin } from "../components/adminGuard.js";
import { getSystemSettings, updateSystemSettings } from "../api/systemSettingsApi.js";
import { logout } from "../api/authApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

requireAdmin();

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");

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

async function loadSettings() {
  saveBtn.disabled = true;
  saveBtn.textContent = "Yüklənir...";

  try {
    const s = await getSystemSettings();
    minMinutesInput.value = s.minReservationMinutes;
    maxHoursInput.value = s.maxReservationHours;
    bufferMinutesInput.value = s.bufferMinutes;
    horizonDaysInput.value = s.bookingHorizonDays;
    limitToggle.checked = s.isActiveReservationLimitEnabled;
    maxActiveInput.value = s.maxActiveReservationsPerUser;
    calendarRangeInput.value = s.maxCalendarRangeDays;
    updateMaxActiveState();
  } catch (err) {
    showAlert(alertBox, err.message);
  } finally {
    saveBtn.disabled = false;
    saveBtn.innerHTML = '<i class="bi bi-check-lg"></i>Bütün ayarları yadda saxla';
  }
}

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
  saveBtn.textContent = "Yadda saxlanılır...";

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
    await updateSystemSettings(dto);
    showAlert(alertBox, "Ayarlar yadda saxlanıldı.", "success");
  } catch (err) {
    showAlert(alertBox, err.message);
  } finally {
    saveBtn.disabled = false;
    saveBtn.innerHTML = '<i class="bi bi-check-lg"></i>Bütün ayarları yadda saxla';
  }
});

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "adminLogin.html";
});

loadSettings();
