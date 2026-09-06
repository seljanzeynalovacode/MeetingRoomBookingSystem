import { requireAdmin, getDisplayIdentity } from "../components/adminGuard.js";
import { initAdminMobileNav, initAdminAccountMenu } from "../components/mobileNav.js";
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
const roleListEl = document.getElementById("roleList");
const newRoleInput = document.getElementById("newRoleInput");
const addRoleBtn = document.getElementById("addRoleBtn");
const usersTableBody = document.getElementById("usersTableBody");
const logoutBtn = document.getElementById("logoutBtn");
const userSearchInput = document.getElementById("userSearchInput");
const resetUserSearchBtn = document.getElementById("resetUserSearchBtn");

const filterRoleDropdown = initDropdown({
  btn: document.getElementById("filterRoleBtn"),
  panel: document.getElementById("filterRolePanel"),
  options: [],
  placeholder: "Bütün rollar",
  onChange: () => renderUsers(),
});

userSearchInput.addEventListener("input", renderUsers);
resetUserSearchBtn.addEventListener("click", () => {
  userSearchInput.value = "";
  filterRoleDropdown.setValue("");
  renderUsers();
});

let roles = [];
let users = [];
let roleAssignDropdowns = new Map();
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
      const inUseCount = users.filter((u) => u.roles.includes(r.name)).length;
      const disabled = inUseCount > 0;
      const title = disabled ? `Bu rol ${inUseCount} istifadəçiyə təyin edilib, əvvəlcə onlardan silin` : "Sil";
      return `<span class="${cls}"><span class="rm-chip-dot"></span>${escapeHtml(r.name)}<button type="button" data-id="${r.id}" data-name="${escapeHtml(r.name)}" class="rm-delete-btn" aria-label="Sil" title="${escapeHtml(title)}" ${disabled ? "disabled" : ""}><i class="bi bi-x"></i></button></span>`;
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
  filterRoleDropdown.setOptions(roles.map((r) => ({ value: r.name, label: r.name })), filterRoleDropdown.getValue());
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

function getInitials(firstName, lastName) {
  const a = (firstName || "").trim()[0] || "";
  const b = (lastName || "").trim()[0] || "";
  return (a + b).toUpperCase() || "?";
}

function createUserRowHtml(user) {
  const adminCount = users.filter((u) => u.roles.includes("Admin")).length;

  const roleChips = user.roles
    .map((roleName) => {
      const cls = roleName === "Admin" ? "role-chip role-admin" : "role-chip";
      const isLastRole = user.roles.length <= 1;
      const isLastAdmin = roleName === "Admin" && adminCount <= 1;
      const disabled = isLastRole || isLastAdmin;
      const title = isLastRole
        ? "İstifadəçinin son rolu silinə bilməz"
        : isLastAdmin
          ? "Sistemdə son Admin, rolu silinə bilməz"
          : "Rolu sil";
      return `<span class="${cls}">${escapeHtml(roleName)}<button type="button" class="remove-role-btn" data-user-id="${user.id}" data-role="${escapeHtml(roleName)}" aria-label="Rolu sil" title="${escapeHtml(title)}" ${disabled ? "disabled" : ""}><i class="bi bi-x"></i></button></span>`;
    })
    .join("");

  const availableRoles = roles.filter((r) => !user.roles.includes(r.name));
  const assignSelectHtml = availableRoles.length
    ? `<div class="assign-role-row">
         <div class="dd-wrap assign-role-dd" data-user-id="${user.id}">
           <button type="button" class="dd-btn"><span>Seçin</span><i class="bi bi-chevron-down"></i></button>
           <div class="dd-panel"></div>
         </div>
         <button type="button" class="btn btn-secondary btn-sm assign-role-btn" data-user-id="${user.id}"><i class="bi bi-plus"></i></button>
       </div>`
    : `<span class="text-muted" style="font-size:12px;">Hamısı təyin edilib</span>`;

  return `
    <div class="user-row">
      <span class="row-avatar">${getInitials(user.firstName, user.lastName)}</span>
      <div class="user-identity">
        <p class="user-name">${escapeHtml(user.firstName)} ${escapeHtml(user.lastName)}</p>
        <p class="user-meta">${escapeHtml(user.email)}</p>
        <p class="user-meta user-username">İstifadəçi adı: <span>@${escapeHtml(user.userName)}</span></p>
      </div>
      <div class="user-roles">
        ${roleChips || '<span class="text-muted" style="font-size:12px;">Rol yoxdur</span>'}
        ${assignSelectHtml}
      </div>
      <button type="button" class="icon-action-btn delete-user-btn" data-user-id="${user.id}" title="İstifadəçini sil"><i class="bi bi-trash"></i></button>
    </div>`;
}

function destroyRoleDropdowns() {
  roleAssignDropdowns.forEach((dd) => dd.destroy());
  roleAssignDropdowns.clear();
}

function renderUsers() {
  destroyRoleDropdowns();

  if (!users.length) {
    usersTableBody.innerHTML = `<div class="empty-state"><i class="bi bi-people"></i>İstifadəçi tapılmadı.</div>`;
    return;
  }

  const searchTerm = userSearchInput.value.trim().toLocaleLowerCase("az");
  const roleFilter = filterRoleDropdown.getValue();

  const matched = users.filter((u) => {
    if (roleFilter && !u.roles.includes(roleFilter)) return false;
    if (!searchTerm) return true;
    const haystack = `${u.firstName} ${u.lastName} ${u.email || ""} ${u.userName || ""}`.toLocaleLowerCase("az");
    return haystack.includes(searchTerm);
  });

  if (!matched.length) {
    usersTableBody.innerHTML = `<div class="empty-state"><i class="bi bi-search"></i>Axtarışa uyğun istifadəçi tapılmadı.</div>`;
    return;
  }

  usersTableBody.innerHTML = matched.map(createUserRowHtml).join("");

  usersTableBody.querySelectorAll(".assign-role-dd").forEach((wrap) => {
    const userId = wrap.dataset.userId;
    const user = matched.find((u) => u.id === userId);
    if (!user) return;
    const availableRoles = roles.filter((r) => !user.roles.includes(r.name));
    const dd = initDropdown({
      btn: wrap.querySelector(".dd-btn"),
      panel: wrap.querySelector(".dd-panel"),
      options: availableRoles.map((r) => ({ value: r.name, label: r.name })),
      placeholder: "Seçin",
      onChange: () => {},
    });
    roleAssignDropdowns.set(userId, dd);
  });

  usersTableBody.querySelectorAll(".assign-role-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const userId = btn.dataset.userId;
      const roleName = roleAssignDropdowns.get(userId)?.getValue();
      if (!roleName) {
        showAlert(alertBox, "Zəhmət olmasa bir rol seçin.");
        return;
      }
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
  window.location.href = "adminLogin.html";
});

async function init() {
  clearAlert(alertBox);
  try {
    const [roleList, userList] = await Promise.all([getAllRoles(), getAllUsersWithRoles()]);
    roles = roleList;
    users = userList;
    filterRoleDropdown.setOptions(roles.map((r) => ({ value: r.name, label: r.name })), "");
    renderRoles();
    renderUsers();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

init();