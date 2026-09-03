import { createReservation, updateReservation } from "../api/reservationsApi.js";
import { createParticipant, getParticipantsByReservation, deleteParticipant } from "../api/participantsApi.js";
import { showFieldErrors, clearFieldErrors } from "../components/alert.js";
import { getSystemSettings } from "../api/systemSettingsApi.js";

let modalEl = null;
let pendingParticipantEmails = [""];
let existingParticipants = [];
let currentOptions = null;
let currentSettings = { minReservationMinutes: 30, maxReservationHours: 4, bookingHorizonDays: 30 };

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function toDateOnlyString(date) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
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
          <div class="participant-add-row" id="participantAddRow">
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
  const addRow = modalEl.querySelector("#participantAddRow");

  if (currentOptions.mode === "edit") {
    addRow.style.display = "";

    if (!existingParticipants.length) {
      listEl.innerHTML = "";
      return;
    }

    listEl.innerHTML = existingParticipants
      .map(
        (p) =>
          `<div class="participant-row"><span class="p-email"><i class="bi bi-person"></i> ${escapeHtml(p.guestEmail || p.email)}</span><button type="button" data-id="${p.id}" class="remove-participant-btn" aria-label="Sil"><i class="bi bi-x"></i></button></div>`
      )
      .join("");

    listEl.querySelectorAll(".remove-participant-btn").forEach((btn) => {
      btn.addEventListener("click", async () => {
        try {
          await deleteParticipant(btn.dataset.id);
          existingParticipants = existingParticipants.filter((p) => p.id !== btn.dataset.id);
          renderParticipants();
        } catch (err) {
          showModalAlert(err.message);
        }
      });
    });
    return;
  }

  addRow.style.display = "none";

  listEl.innerHTML = pendingParticipantEmails
    .map((email, idx) => {
      const isLast = idx === pendingParticipantEmails.length - 1;
      const actionBtn = isLast
        ? `<button type="button" class="btn btn-secondary btn-sm participant-add-btn" aria-label="Qonaq əlavə et"><i class="bi bi-plus"></i></button>`
        : `<button type="button" class="btn btn-outline btn-sm participant-remove-btn" data-idx="${idx}" aria-label="Sil"><i class="bi bi-x"></i></button>`;
      return `
        <div class="participant-add-row">
          <input type="email" class="input participant-email-input" data-idx="${idx}" placeholder="qonaq@nümunə.com" value="${escapeHtml(email)}" />
          ${actionBtn}
        </div>`;
    })
    .join("");

  listEl.querySelectorAll(".participant-email-input").forEach((input) => {
    input.addEventListener("input", () => {
      pendingParticipantEmails[Number(input.dataset.idx)] = input.value;
    });
  });

  const addBtn = listEl.querySelector(".participant-add-btn");
  if (addBtn) {
    addBtn.addEventListener("click", () => {
      pendingParticipantEmails.push("");
      renderParticipants();
      const inputs = listEl.querySelectorAll(".participant-email-input");
      inputs[inputs.length - 1]?.focus();
    });
  }

  listEl.querySelectorAll(".participant-remove-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      pendingParticipantEmails.splice(Number(btn.dataset.idx), 1);
      renderParticipants();
    });
  });
}

async function addParticipantFromInput() {
  const input = modalEl.querySelector("#participantEmailInput");
  const email = input.value.trim();
  if (!email) return;

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

      const guestEmails = pendingParticipantEmails.map((e) => e.trim()).filter(Boolean);
      if (guestEmails.length) {
        const results = await Promise.allSettled(
          guestEmails.map((email) =>
            createParticipant({ ReservationId: reservation.id, GuestEmail: email, GuestName: null })
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
  pendingParticipantEmails = [""];
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

  dateInput.min = toDateOnlyString(new Date());
  dateInput.max = isAdmin ? "" : toDateOnlyString(new Date(Date.now() + currentSettings.bookingHorizonDays * 24 * 60 * 60 * 1000));

  if (options.mode === "edit") {
    modalTitleEl.textContent = "Rezervasiyanı redaktə et";
    submitBtn.textContent = "Yadda saxla";
    roomNameEl.textContent = options.reservation.roomName;
    titleInput.value = options.reservation.title;

    const start = new Date(options.reservation.startDate);
    const end = new Date(options.reservation.endDate);
    dateInput.value = toDateOnlyString(start);
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
    dateInput.value = options.prefillDate || toDateOnlyString(new Date());
    startInput.value = options.prefillStart || "";
    endInput.value = options.prefillEnd || "";
    renderParticipants();
  }

  modalEl.classList.add("open");
  titleInput.focus();
}