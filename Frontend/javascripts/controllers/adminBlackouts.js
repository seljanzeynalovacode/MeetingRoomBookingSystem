import { requireAdmin } from "../components/adminGuard.js";
import { getAllRooms } from "../api/roomsApi.js";
import { logout } from "../api/authApi.js";
import { getAllRoomBlackouts, createRoomBlackout, updateRoomBlackout, deleteRoomBlackout } from "../api/roomBlackoutsApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

requireAdmin();

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");
const blackoutsList = document.getElementById("blackoutsList");
const addBlackoutBtn = document.getElementById("addBlackoutBtn");

const modal = document.getElementById("blackoutModal");
const modalTitle = document.getElementById("blackoutModalTitle");
const modalAlert = document.getElementById("blackoutModalAlert");
const modalClose = document.getElementById("blackoutModalClose");
const modalCancelBtn = document.getElementById("blackoutModalCancelBtn");
const form = document.getElementById("blackoutForm");
const roomSelect = document.getElementById("blackoutRoom");
const dateInput = document.getElementById("blackoutDate");
const startInput = document.getElementById("blackoutStart");
const endInput = document.getElementById("blackoutEnd");
const reasonInput = document.getElementById("blackoutReason");
const submitBtn = document.getElementById("blackoutModalSubmitBtn");

let blackouts = [];
let rooms = [];
let editingId = null;

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function formatRange(startIso, endIso) {
  const start = new Date(startIso);
  const end = new Date(endIso);
  const dateStr = start.toLocaleDateString("az-AZ", { day: "2-digit", month: "short", year: "numeric" });
  return `${dateStr}, ${start.toTimeString().slice(0, 5)}–${end.toTimeString().slice(0, 5)}`;
}

function openModal(blackout) {
  editingId = blackout ? blackout.id : null;
  modalTitle.textContent = blackout ? "Bloklamanı redaktə et" : "Yeni bloklama";
  submitBtn.textContent = blackout ? "Yadda saxla" : "Yarat";

  if (blackout) {
    roomSelect.value = blackout.roomId;
    roomSelect.disabled = true;
    const start = new Date(blackout.startDate);
    const end = new Date(blackout.endDate);
    dateInput.value = start.toISOString().slice(0, 10);
    startInput.value = start.toTimeString().slice(0, 5);
    endInput.value = end.toTimeString().slice(0, 5);
    reasonInput.value = blackout.reason;
  } else {
    roomSelect.disabled = false;
    roomSelect.value = rooms[0]?.id || "";
    dateInput.value = new Date().toISOString().slice(0, 10);
    startInput.value = "09:00";
    endInput.value = "10:00";
    reasonInput.value = "";
  }

  modalAlert.innerHTML = "";
  modal.classList.add("open");
}

function closeModal() {
  modal.classList.remove("open");
}

modalClose.addEventListener("click", closeModal);
modalCancelBtn.addEventListener("click", closeModal);
modal.addEventListener("click", (e) => {
  if (e.target === modal) closeModal();
});

addBlackoutBtn.addEventListener("click", () => openModal(null));

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  modalAlert.innerHTML = "";

  const date = dateInput.value;
  const start = startInput.value;
  const end = endInput.value;
  const reason = reasonInput.value.trim();

  if (!date || !start || !end || !reason) {
    modalAlert.innerHTML = `<div class="alert alert-warning"><i class="bi bi-exclamation-triangle"></i><span>Bütün sahələri doldurun.</span></div>`;
    return;
  }

  const startDate = new Date(`${date}T${start}`);
  const endDate = new Date(`${date}T${end}`);

  if (startDate >= endDate) {
    modalAlert.innerHTML = `<div class="alert alert-warning"><i class="bi bi-exclamation-triangle"></i><span>Bitmə vaxtı başlanğıcdan sonra olmalıdır.</span></div>`;
    return;
  }

  submitBtn.disabled = true;
  submitBtn.textContent = "Yadda saxlanılır...";

  try {
    if (editingId) {
      await updateRoomBlackout(editingId, {
        StartDate: startDate.toISOString(),
        EndDate: endDate.toISOString(),
        Reason: reason,
      });
    } else {
      await createRoomBlackout({
        RoomId: roomSelect.value,
        StartDate: startDate.toISOString(),
        EndDate: endDate.toISOString(),
        Reason: reason,
      });
    }
    closeModal();
    await loadData();
  } catch (err) {
    modalAlert.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-circle"></i><span>${escapeHtml(err.message)}</span></div>`;
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = editingId ? "Yadda saxla" : "Yarat";
  }
});

function createBlackoutCardHtml(b) {
  return `
    <div class="card blackout-card">
      <div class="blackout-card-body">
        <div class="blackout-card-head">
          <div class="blackout-card-title">
            <i class="bi bi-calendar-x"></i>
            <h4>${escapeHtml(b.roomName)}</h4>
          </div>
          <div class="row-actions">
            <button class="icon-action-btn edit-btn" data-id="${b.id}" title="Redaktə et"><i class="bi bi-pencil"></i></button>
            <button class="icon-action-btn delete-btn" data-id="${b.id}" title="Sil"><i class="bi bi-trash"></i></button>
          </div>
        </div>
        <div class="blackout-card-meta"><i class="bi bi-clock"></i>${formatRange(b.startDate, b.endDate)}</div>
        <p class="blackout-card-reason">${escapeHtml(b.reason)}</p>
      </div>
    </div>`;
}

function renderBlackouts() {
  if (!blackouts.length) {
    blackoutsList.innerHTML = `<div class="empty-state"><i class="bi bi-calendar-x"></i>Bloklama tapılmadı.</div>`;
    return;
  }

  blackoutsList.innerHTML = blackouts.map(createBlackoutCardHtml).join("");

  blackoutsList.querySelectorAll(".edit-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const b = blackouts.find((x) => x.id === btn.dataset.id);
      if (b) openModal(b);
    });
  });

  blackoutsList.querySelectorAll(".delete-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!confirm("Bu bloklamanı silmək istədiyinizə əminsiniz?")) return;
      clearAlert(alertBox);
      try {
        await deleteRoomBlackout(btn.dataset.id);
        await loadData();
      } catch (err) {
        showAlert(alertBox, err.message);
      }
    });
  });
}

async function loadData() {
  clearAlert(alertBox);
  try {
    const [blackoutList, roomList] = await Promise.all([getAllRoomBlackouts(), getAllRooms()]);
    blackouts = blackoutList;
    rooms = roomList;
    roomSelect.innerHTML = rooms.map((r) => `<option value="${r.id}">${escapeHtml(r.name)} — ${escapeHtml(r.buildingName)}</option>`).join("");
    renderBlackouts();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "adminLogin.html";
});

loadData();