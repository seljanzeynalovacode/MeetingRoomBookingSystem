import { createReservation, updateReservation } from "../api/reservationsApi.js";
import { createParticipant, getParticipantsByReservation, deleteParticipant } from "../api/participantsApi.js";
import { showFieldErrors, clearFieldErrors } from "../components/alert.js";
import { getSystemSettings } from "../api/systemSettingsApi.js";

let modalEl = null;
let pendingParticipants = [];
let existingParticipants = [];
let currentOptions = null;
let currentSettings = { minReservationMinutes: 30, maxReservationHours: 4, bookingHorizonDays: 30 };

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function ensureModal() {
  if (modalEl) return modalEl;

  modalEl = document.createElement("div");
  modalEl.className = "modal-overlay";
  modalEl.innerHTML = `
    <div class="modal-panel">
      <div class="modal-header">
        <h3 id="bookingModalTitle">Rezervasiya</h3>
        <button type="button" class="modal-close" id="bookingModalClose" aria-label="Bağla"><i class="bi bi-x-lg"></i></button>
      </div>

      <div id="bookingModalAlert"></div>

      <div class="modal-room-chip"><i class="bi bi-door-open"></i><span id="bookingModalRoomName"></span></div>

      <form id="bookingModalForm" novalidate>
        <div class="field">
          <label class="field-label" for="bookingTitle">Başlıq</label>
          <div class="input-wrap">
            <i class="bi bi-card-text leading-icon"></i>
            <input type="text" class="input has-icon" id="bookingTitle" name="Title" required minlength="2" maxlength="200" placeholder="Həftəlik komanda görüşü" />
          </div>
        </div>

        <div class="field">
          <label class="field-label" for="bookingDate">Tarix</label>
          <input type="date" class="input" id="bookingDate" required />
        </div>

        <div class="grid-2">
          <div class="field">
            <label class="field-label" for="bookingStart">Başlama</label>
            <input type="time" class="input" id="bookingStart" required />
          </div>
          <div class="field">
            <label class="field-label" for="bookingEnd">Bitmə</label>
            <input type="time" class="input" id="bookingEnd" required />
          </div>
        </div>

        <div class="field">
          <label class="field-label">Qonaqlar (email)</label>
          <div class="participant-list" id="participantList"></div>
          <div class="participant-add-row">
            <input type="email" class="input" id="participantEmailInput" placeholder="qonaq@nümunə.com" />
            <button type="button" class="btn btn-secondary btn-sm" id="addParticipantBtn"><i class="bi bi-plus"></i></button>
          </div>
        </div>

        <div class="modal-footer">
          <button type="button" class="btn btn-outline" id="bookingModalCancelBtn">İmtina</button>
          <button type="submit" class="btn btn-primary" id="bookingModalSubmitBtn">Rezerv et</button>
        </div>
      </form>
    </div>
  `;
  document.body.appendChild(modalEl);

  modalEl.querySelector("#bookingModalClose").addEventListener("click", closeModal);
  modalEl.querySelector("#bookingModalCancelBtn").addEventListener("click", closeModal);
  modalEl.addEventListener("click", (e) => {
    if (e.target === modalEl) closeModal();
  });
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && modalEl.classList.contains("open")) closeModal();
  });

  modalEl.querySelector("#addParticipantBtn").addEventListener("click", addParticipantFromInput);
  modalEl.querySelector("#participantEmailInput").addEventListener("keydown", (e) => {
    if (e.key === "Enter") {
      e.preventDefault();
      addParticipantFromInput();
    }
  });

  modalEl.querySelector("#bookingModalForm").addEventListener("submit", handleSubmit);

  return modalEl;
}

function showModalAlert(message, type = "danger") {
  const box = modalEl.querySelector("#bookingModalAlert");
  const icon = type === "success" ? "bi-check-circle" : type === "warning" ? "bi-exclamation-triangle" : "bi-exclamation-circle";
  box.innerHTML = `<div class="alert alert-${type}"><i class="bi ${icon}"></i><span>${escapeHtml(message)}</span></div>`;
}

function clearModalAlert() {
  modalEl.querySelector("#bookingModalAlert").innerHTML = "";
}

function renderParticipants() {
  const listEl = modalEl.querySelector("#participantList");
  const items = currentOptions.mode === "edit" ? existingParticipants : pendingParticipants;

  if (!items.length) {
    listEl.innerHTML = "";
    return;
  }

  listEl.innerHTML = items
    .map((p, idx) => {
      const email = p.guestEmail || p.email;
      const key = currentOptions.mode === "edit" ? p.id : idx;
      return `<div class="participant-row"><span class="p-email"><i class="bi bi-person"></i> ${escapeHtml(email)}</span><button type="button" data-key="${key}" class="remove-participant-btn" aria-label="Sil"><i class="bi bi-x"></i></button></div>`;
    })
    .join("");

  listEl.querySelectorAll(".remove-participant-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const key = btn.dataset.key;
      if (currentOptions.mode === "edit") {
        try {
          await deleteParticipant(key);
          existingParticipants = existingParticipants.filter((p) => p.id !== key);
          renderParticipants();
        } catch (err) {
          showModalAlert(err.message);
        }
      } else {
        pendingParticipants.splice(Number(key), 1);
        renderParticipants();
      }
    });
  });
}

async function addParticipantFromInput() {
  const input = modalEl.querySelector("#participantEmailInput");
  const email = input.value.trim();
  if (!email) return;

  if (currentOptions.mode === "edit") {
    try {
      const created = await createParticipant({
        ReservationId: currentOptions.reservation.id,
        GuestEmail: email,
        GuestName: null,
      });
      existingParticipants.push(created);
      renderParticipants();
      input.value = "";
    } catch (err) {
      showModalAlert(err.message);
    }
  } else {
    pendingParticipants.push({ email });
    renderParticipants();
    input.value = "";
  }
}

function closeModal() {
  if (modalEl) modalEl.classList.remove("open");
}

async function handleSubmit(e) {
  e.preventDefault();
  const form = modalEl.querySelector("#bookingModalForm");
  clearModalAlert();
  clearFieldErrors(form);

  const titleInput = modalEl.querySelector("#bookingTitle");
  const dateInput = modalEl.querySelector("#bookingDate");
  const startInput = modalEl.querySelector("#bookingStart");
  const endInput = modalEl.querySelector("#bookingEnd");
  const submitBtn = modalEl.querySelector("#bookingModalSubmitBtn");

  const title = titleInput.value.trim();
  const date = dateInput.value;
  const start = startInput.value;
  const end = endInput.value;

  if (!title || !date) {
    showModalAlert("Bütün sahələri doldurun.", "warning");
    return;
  }

  if (!start || !end) {
    showModalAlert("Zəhmət olmasa saat təyin edin.", "warning");
    return;
  }

  if (title.length < 2) {
    showFieldErrors(form, { Title: ["Başlıq minimum 2 simvol olmalıdır."] });
    showModalAlert("Başlıq minimum 2 simvol olmalıdır.", "warning");
    return;
  }

  const startDate = new Date(`${date}T${start}`);
  const endDate = new Date(`${date}T${end}`);

    if (startDate >= endDate) {
    showModalAlert("Bitmə saatı başlama saatından sonra olmalıdır.", "warning");
    return;
  }

  if (startDate < new Date()) {
    showModalAlert("Keçmiş tarixə və ya saata rezervasiya edilə bilməz.", "warning");
    return;
  }

  if (endDate - startDate < currentSettings.minReservationMinutes * 60 * 1000) {
    showModalAlert(`Rezervasiya minimum ${currentSettings.minReservationMinutes} dəqiqə olmalıdır.`, "warning");
    return;
  }

  if (endDate - startDate > currentSettings.maxReservationHours * 60 * 60 * 1000) {
    showModalAlert(`Rezervasiya maksimum ${currentSettings.maxReservationHours} saat ola bilər.`, "warning");
    return;
  }

  if (currentOptions.isAdmin !== true && startDate > new Date(Date.now() + currentSettings.bookingHorizonDays * 24 * 60 * 60 * 1000)) {
    showModalAlert(`Rezervasiya yalnız növbəti ${currentSettings.bookingHorizonDays} gün ərzində yaradıla bilər.`, "warning");
    return;
  }

  submitBtn.disabled = true;
  submitBtn.textContent = currentOptions.mode === "edit" ? "Yadda saxlanılır..." : "Rezerv edilir...";

  try {
    if (currentOptions.mode === "edit") {
      await updateReservation(currentOptions.reservation.id, {
        Title: title,
        StartDate: startDate.toISOString(),
        EndDate: endDate.toISOString(),
        RoomId: currentOptions.reservation.roomId,
      });
    } else {
      const reservation = await createReservation({
        Title: title,
        StartDate: startDate.toISOString(),
        EndDate: endDate.toISOString(),
        RoomId: currentOptions.room.id,
      });

      if (pendingParticipants.length) {
        const results = await Promise.allSettled(
          pendingParticipants.map((p) =>
            createParticipant({ ReservationId: reservation.id, GuestEmail: p.email, GuestName: null })
          )
        );
        const failed = results.filter((r) => r.status === "rejected").length;
        if (failed) {
          showModalAlert(`Rezervasiya yaradıldı, amma ${failed} qonaq əlavə edilmədi.`, "warning");
          setTimeout(() => {
            closeModal();
            if (currentOptions.onSuccess) currentOptions.onSuccess();
          }, 1600);
          return;
        }
      }
    }

    closeModal();
    if (currentOptions.onSuccess) currentOptions.onSuccess();
  } catch (err) {
    if (err.fieldErrors) {
      showFieldErrors(form, err.fieldErrors);
    }
    showModalAlert(err.message);
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = currentOptions.mode === "edit" ? "Yadda saxla" : "Rezerv et";
  }
}

export async function openBookingModal(options) {
  currentOptions = options;
  pendingParticipants = [];
  existingParticipants = [];
  ensureModal();

  const isAdmin = options.isAdmin === true;

  try {
    currentSettings = await getSystemSettings();
  } catch {
    // Ayarlar yüklənə bilmədi — defolt dəyərlərlə davam et
  }

  const form = modalEl.querySelector("#bookingModalForm");
  form.reset();
  clearModalAlert();
  clearFieldErrors(form);

  const titleInput = modalEl.querySelector("#bookingTitle");
  const dateInput = modalEl.querySelector("#bookingDate");
  const startInput = modalEl.querySelector("#bookingStart");
  const endInput = modalEl.querySelector("#bookingEnd");
  const roomNameEl = modalEl.querySelector("#bookingModalRoomName");
  const modalTitleEl = modalEl.querySelector("#bookingModalTitle");
  const submitBtn = modalEl.querySelector("#bookingModalSubmitBtn");

  dateInput.min = new Date().toISOString().slice(0, 10);
  dateInput.max = isAdmin ? "" : new Date(Date.now() + currentSettings.bookingHorizonDays * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);

  if (options.mode === "edit") {
    modalTitleEl.textContent = "Rezervasiyanı redaktə et";
    submitBtn.textContent = "Yadda saxla";
    roomNameEl.textContent = options.reservation.roomName;
    titleInput.value = options.reservation.title;

    const start = new Date(options.reservation.startDate);
    const end = new Date(options.reservation.endDate);
    dateInput.value = start.toISOString().slice(0, 10);
    startInput.value = start.toTimeString().slice(0, 5);
    endInput.value = end.toTimeString().slice(0, 5);

    renderParticipants();
    getParticipantsByReservation(options.reservation.id)
      .then((list) => {
        existingParticipants = list;
        renderParticipants();
      })
      .catch((err) => showModalAlert(err.message));
  } else {
    modalTitleEl.textContent = "Otağı rezerv et";
    submitBtn.textContent = "Rezerv et";
    roomNameEl.textContent = options.room.name;
    titleInput.value = "";
    dateInput.value = options.prefillDate || new Date().toISOString().slice(0, 10);
    startInput.value = options.prefillStart || "";
    endInput.value = options.prefillEnd || "";
    renderParticipants();
  }

  modalEl.classList.add("open");
  titleInput.focus();
}