import { isLoggedIn } from "../api/apiClient.js";
import { logout } from "../api/authApi.js";
import {
  getAllRoles,
  createRole,
  deleteRole,
  getAllUsersWithRoles,
  assignRole,
  removeRole,
  deleteUser,
} from "../api/adminApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

if (!isLoggedIn()) {
  window.location.href = "login.html";
}

const alertBox = document.getElementById("alertBox");
const roleListEl = document.getElementById("roleList");
const newRoleInput = document.getElementById("newRoleInput");
const addRoleBtn = document.getElementById("addRoleBtn");
const usersTableBody = document.getElementById("usersTableBody");
const logoutBtn = document.getElementById("logoutBtn");

let roles = [];
let users = [];

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function renderRoles() {
  const roleCountEl = document.getElementById("roleCount");
  if (roleCountEl) roleCountEl.textContent = `${roles.length} rol`;

  if (!roles.length) {
    roleListEl.innerHTML = `<span class="text-muted" style="font-size:12.5px;">Rol yoxdur.</span>`;
    return;
  }

  roleListEl.innerHTML = roles
    .map((r) => {
      const cls = r.name === "Admin" ? "rm-chip rm-chip-admin" : "rm-chip";
      return `<span class="${cls}"><span class="rm-chip-dot"></span>${escapeHtml(r.name)}<button type="button" data-id="${r.id}" data-name="${escapeHtml(r.name)}" class="rm-delete-btn" aria-label="Sil"><i class="bi bi-x"></i></button></span>`;
    })
    .join("");

  roleListEl.querySelectorAll(".rm-delete-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!confirm(`"${btn.dataset.name}" rolunu silmək istədiyinizə əminsiniz?`)) return;
      clearAlert(alertBox);
      try {
        await deleteRole(btn.dataset.id);
        await loadRoles();
      } catch (err) {
        showAlert(alertBox, err.message);
      }
    });
  });
}

async function loadRoles() {
  roles = await getAllRoles();
  renderRoles();
  renderUsers();
}

addRoleBtn.addEventListener("click", async () => {
  const name = newRoleInput.value.trim();
  if (!name) return;
  clearAlert(alertBox);
  addRoleBtn.disabled = true;
  try {
    await createRole({ Name: name });
    newRoleInput.value = "";
    await loadRoles();
  } catch (err) {
    showAlert(alertBox, err.message);
  } finally {
    addRoleBtn.disabled = false;
  }
});

newRoleInput.addEventListener("keydown", (e) => {
  if (e.key === "Enter") {
    e.preventDefault();
    addRoleBtn.click();
  }
});

function createUserRowHtml(user) {
  const roleChips = user.roles
    .map((roleName) => {
      const cls = roleName === "Admin" ? "role-chip role-admin" : "role-chip";
      return `<span class="${cls}">${escapeHtml(roleName)}<button type="button" class="remove-role-btn" data-user-id="${user.id}" data-role="${escapeHtml(roleName)}" aria-label="Rolu sil"><i class="bi bi-x"></i></button></span>`;
    })
    .join("");

  const availableRoles = roles.filter((r) => !user.roles.includes(r.name));
    const assignSelectHtml = availableRoles.length
    ? `<div class="assign-role-row">
         <div class="select-wrap">
           <select class="input assign-role-select" data-user-id="${user.id}">
             ${availableRoles.map((r) => `<option value="${escapeHtml(r.name)}">${escapeHtml(r.name)}</option>`).join("")}
           </select>
           <i class="bi bi-chevron-down select-chevron"></i>
         </div>
         <button type="button" class="btn btn-secondary btn-sm assign-role-btn" data-user-id="${user.id}"><i class="bi bi-plus"></i></button>
       </div>`
    : `<span class="text-muted" style="font-size:12px;">Hamısı təyin edilib</span>`;
  return `
    <tr>
      <td>${escapeHtml(user.firstName)} ${escapeHtml(user.lastName)}</td>
      <td>${escapeHtml(user.email)}</td>
      <td>${escapeHtml(user.userName)}</td>
      <td><div class="role-chip-row">${roleChips || '<span class="text-muted" style="font-size:12px;">Rol yoxdur</span>'}</div></td>
      <td>${assignSelectHtml}</td>
      <td><button type="button" class="icon-action-btn delete-user-btn" data-user-id="${user.id}" title="İstifadəçini sil"><i class="bi bi-trash"></i></button></td>
    </tr>`;
}

function renderUsers() {
  if (!users.length) {
    usersTableBody.innerHTML = `<tr><td colspan="6" class="text-muted" style="text-align:center; padding:2rem;">İstifadəçi tapılmadı.</td></tr>`;
    return;
  }
  usersTableBody.innerHTML = users.map(createUserRowHtml).join("");

  usersTableBody.querySelectorAll(".assign-role-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const userId = btn.dataset.userId;
      const select = usersTableBody.querySelector(`.assign-role-select[data-user-id="${userId}"]`);
      const roleName = select.value;
      clearAlert(alertBox);
      btn.disabled = true;
      try {
        await assignRole(userId, roleName);
        await loadUsers();
      } catch (err) {
        showAlert(alertBox, err.message);
      } finally {
        btn.disabled = false;
      }
    });
  });

  usersTableBody.querySelectorAll(".remove-role-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!confirm(`"${btn.dataset.role}" rolunu bu istifadəçidən silmək istədiyinizə əminsiniz?`)) return;
      clearAlert(alertBox);
      try {
        await removeRole(btn.dataset.userId, btn.dataset.role);
        await loadUsers();
      } catch (err) {
        showAlert(alertBox, err.message);
      }
    });
  });

  usersTableBody.querySelectorAll(".delete-user-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!confirm("Bu istifadəçini silmək istədiyinizə əminsiniz? Bu geri qaytarıla bilməz.")) return;
      clearAlert(alertBox);
      try {
        await deleteUser(btn.dataset.userId);
        await loadUsers();
      } catch (err) {
        showAlert(alertBox, err.message);
      }
    });
  });
}

async function loadUsers() {
  users = await getAllUsersWithRoles();
  renderUsers();
}

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "login.html";
});

async function init() {
  clearAlert(alertBox);
  try {
    const [roleList, userList] = await Promise.all([getAllRoles(), getAllUsersWithRoles()]);
    roles = roleList;
    users = userList;
    renderRoles();
    renderUsers();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

init();