import { requireAdmin, getDisplayIdentity } from "../components/adminGuard.js";
import { initAdminMobileNav, initAdminAccountMenu } from "../components/mobileNav.js";
import { logout } from "../api/authApi.js";
import { getAccessToken, API_BASE_URL } from "../api/apiClient.js";
import { getAllRooms, getAllBuildings } from "../api/roomsApi.js";
import { getAllUsersWithRoles } from "../api/adminApi.js";
import { getAllReservationsAdmin } from "../api/reservationsApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

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

const roomCountEl = document.getElementById("statRoomCount");
const buildingCountEl = document.getElementById("statBuildingCount");
const userCountEl = document.getElementById("statUserCount");
const reservationCountEl = document.getElementById("statReservationCount");
const peakHourBadge = document.getElementById("peakHourBadge");
const peakDayBadge = document.getElementById("peakDayBadge");

const WEEKDAY_LABELS = ["Bazar ertəsi", "Çərşənbə axşamı", "Çərşənbə", "Cümə axşamı", "Cümə", "Şənbə", "Bazar"];

function cssVar(name) {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
}

const CHART_COLORS = {
  violetLight: cssVar("--violet-100"),
  violet: cssVar("--violet-500"),
  violetPeak: cssVar("--violet-600"),
  violetDeep: cssVar("--violet-900"),
  amber: cssVar("--amber-500"),
  amberSoft: cssVar("--amber-400"),
  amberLight: cssVar("--amber-300"),
  amberPeak: cssVar("--amber-500"),
  green: cssVar("--green-500"),
  red: cssVar("--red-500"),
  grid: cssVar("--border"),
  text: cssVar("--text-secondary"),
};

function peakBarGradient(context) {
  const { ctx, chartArea } = context.chart;
  if (!chartArea) return CHART_COLORS.violetPeak;
  const gradient = ctx.createLinearGradient(0, chartArea.top, 0, chartArea.bottom);
  gradient.addColorStop(0, CHART_COLORS.violet);
  gradient.addColorStop(1, CHART_COLORS.violetDeep);
  return gradient;
}

function peakBarGradientHorizontal(context) {
  const { ctx, chartArea } = context.chart;
  if (!chartArea) return CHART_COLORS.violetPeak;
  const gradient = ctx.createLinearGradient(chartArea.left, 0, chartArea.right, 0);
  gradient.addColorStop(0, CHART_COLORS.violetDeep);
  gradient.addColorStop(1, CHART_COLORS.violet);
  return gradient;
}

const barShadowPlugin = {
  id: "barShadow",
  beforeDatasetsDraw(chart) {
    chart.ctx.save();
    chart.ctx.shadowColor = "rgba(29,78,216,0.35)";
    chart.ctx.shadowBlur = 14;
    chart.ctx.shadowOffsetY = 8;
  },
  afterDatasetsDraw(chart) {
    chart.ctx.restore();
  },
};

Chart.defaults.font.family = "'Plus Jakarta Sans', sans-serif";
Chart.defaults.color = CHART_COLORS.text;

let peakHoursChartInstance = null;
let statusDonutChartInstance = null;
let weekdayChartInstance = null;
let buildingChartInstance = null;

function setChartsEmptyState(isEmpty) {
  document.querySelectorAll(".chart-canvas-wrap").forEach((wrap) => {
    const canvas = wrap.querySelector("canvas");
    let emptyEl = wrap.querySelector(".chart-empty-state");

    if (isEmpty) {
      if (canvas) canvas.style.display = "none";
      if (!emptyEl) {
        emptyEl = document.createElement("div");
        emptyEl.className = "empty-state chart-empty-state";
        emptyEl.style.padding = "2rem 1rem";
        emptyEl.innerHTML = `<i class="bi bi-bar-chart"></i>Hələ rezervasiya yoxdur.`;
        wrap.appendChild(emptyEl);
      }
    } else {
      if (canvas) canvas.style.display = "";
      if (emptyEl) emptyEl.remove();
    }
  });
}

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "adminLogin.html";
});

function buildPeakHoursChart(reservations) {
  const hourCounts = new Array(24).fill(0);
  reservations.forEach((r) => {
    const hour = new Date(r.startDate).getHours();
    hourCounts[hour] += 1;
  });

  const maxCount = Math.max(...hourCounts);
  const peakHour = hourCounts.indexOf(maxCount);
  peakHourBadge.textContent = maxCount > 0 ? `Pik: ${String(peakHour).padStart(2, "0")}:00` : "";

  const firstHour = hourCounts.findIndex((c) => c > 0);
  const lastHour = hourCounts.length - 1 - [...hourCounts].reverse().findIndex((c) => c > 0);
  const visibleCounts = hourCounts.slice(firstHour, lastHour + 1);

  const labels = visibleCounts.map((_, i) => `${String(firstHour + i).padStart(2, "0")}:00`);
  const colors = visibleCounts.map((c, i) => (firstHour + i === peakHour && c > 0 ? peakBarGradient : CHART_COLORS.violetLight));

  if (peakHoursChartInstance) peakHoursChartInstance.destroy();
  peakHoursChartInstance = new Chart(document.getElementById("peakHoursChart"), {
    type: "bar",
    plugins: [barShadowPlugin],
    data: {
      labels,
      datasets: [{ data: visibleCounts, backgroundColor: (context) => (typeof colors[context.dataIndex] === "function" ? colors[context.dataIndex](context) : colors[context.dataIndex]), borderRadius: 8, maxBarThickness: 22 }],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: { legend: { display: false } },
      scales: {
        x: { grid: { display: false }, ticks: { maxRotation: 0, autoSkip: true, maxTicksLimit: 8, font: { size: 10.5 } } },
        y: { beginAtZero: true, ticks: { precision: 0, font: { size: 10.5 } }, grid: { color: CHART_COLORS.grid } },
      },
    },
  });
}

function buildStatusDonutChart(reservations) {
  const now = new Date();
  const confirmed = reservations.filter((r) => r.status === "Confirmed" && new Date(r.endDate) >= now).length;
  const completed = reservations.filter((r) => r.status === "Confirmed" && new Date(r.endDate) < now).length;
  const cancelled = reservations.filter((r) => r.status === "Cancelled").length;

  if (statusDonutChartInstance) statusDonutChartInstance.destroy();
  statusDonutChartInstance = new Chart(document.getElementById("statusDonutChart"), {
    type: "doughnut",
    data: {
      labels: ["Aktiv", "Tamamlanıb", "Ləğv edilib"],
      datasets: [{ data: [confirmed, completed, cancelled], backgroundColor: [CHART_COLORS.violetPeak, CHART_COLORS.amberSoft, CHART_COLORS.red], borderWidth: 0 }],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      cutout: "68%",
      plugins: {
        legend: { position: "bottom", labels: { boxWidth: 10, boxHeight: 10, padding: 14, font: { size: 11.5 } } },
      },
    },
  });
}

function buildWeekdayChart(reservations) {
  const counts = new Array(7).fill(0);
  reservations.forEach((r) => {
    const jsDay = new Date(r.startDate).getDay();
    const mondayFirstIndex = (jsDay + 6) % 7;
    counts[mondayFirstIndex] += 1;
  });

  const maxCount = Math.max(...counts);
  const peakDayIndex = counts.indexOf(maxCount);
  peakDayBadge.textContent = maxCount > 0 ? `Pik: ${WEEKDAY_LABELS[peakDayIndex]}` : "";

  const colors = counts.map((c, i) => (i === peakDayIndex && c > 0 ? peakBarGradient : CHART_COLORS.violetLight));

  if (weekdayChartInstance) weekdayChartInstance.destroy();
  weekdayChartInstance = new Chart(document.getElementById("weekdayChart"), {
    type: "bar",
    plugins: [barShadowPlugin],
    data: {
      labels: WEEKDAY_LABELS,
      datasets: [{ data: counts, backgroundColor: (context) => (typeof colors[context.dataIndex] === "function" ? colors[context.dataIndex](context) : colors[context.dataIndex]), borderRadius: 8, maxBarThickness: 40 }],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: { legend: { display: false } },
      scales: {
        x: { grid: { display: false }, ticks: { font: { size: 10.5 } } },
        y: { beginAtZero: true, ticks: { precision: 0, font: { size: 10.5 } }, grid: { color: CHART_COLORS.grid } },
      },
    },
  });
}

function buildBuildingChart(reservations, rooms) {
  const roomMap = new Map(rooms.map((r) => [r.id, r]));
  const counts = {};

  reservations.forEach((r) => {
    const room = roomMap.get(r.roomId);
    const buildingName = room ? room.buildingName : "Naməlum";
    counts[buildingName] = (counts[buildingName] || 0) + 1;
  });

  const sorted = Object.entries(counts).sort((a, b) => b[1] - a[1]);
  const maxCount = Math.max(...sorted.map(([, count]) => count));
  const colors = sorted.map(([, count]) => (count === maxCount ? peakBarGradientHorizontal : CHART_COLORS.violetLight));

  if (buildingChartInstance) buildingChartInstance.destroy();
  buildingChartInstance = new Chart(document.getElementById("buildingChart"), {
    type: "bar",
    plugins: [barShadowPlugin],
    data: {
      labels: sorted.map(([name]) => name),
      datasets: [{ data: sorted.map(([, count]) => count), backgroundColor: (context) => (typeof colors[context.dataIndex] === "function" ? colors[context.dataIndex](context) : colors[context.dataIndex]), borderRadius: 8, maxBarThickness: 28 }],
    },
    options: {
      indexAxis: "y",
      responsive: true,
      maintainAspectRatio: false,
      plugins: { legend: { display: false } },
      scales: {
        x: { beginAtZero: true, ticks: { precision: 0, font: { size: 10.5 } }, grid: { color: CHART_COLORS.grid } },
        y: { grid: { display: false }, ticks: { font: { size: 10.5 } } },
      },
    },
  });
}

async function init() {
  try {
    const [rooms, buildings, users, reservations] = await Promise.all([
      getAllRooms(),
      getAllBuildings(),
      getAllUsersWithRoles(),
      getAllReservationsAdmin(),
    ]);

    roomCountEl.textContent = rooms.length;
    buildingCountEl.textContent = buildings.length;
    userCountEl.textContent = users.length;
    reservationCountEl.textContent = reservations.filter((r) => r.status === "Confirmed" && new Date(r.endDate) >= new Date()).length;

    if (!reservations.length) {
      setChartsEmptyState(true);
      return;
    }

    setChartsEmptyState(false);
    buildPeakHoursChart(reservations);
    buildStatusDonutChart(reservations);
    buildWeekdayChart(reservations);
    buildBuildingChart(reservations, rooms);
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

init();

const hubConnection = new signalR.HubConnectionBuilder()
  .withUrl(`${API_BASE_URL.replace("/api", "")}/hubs/room-calendar`, {
    accessTokenFactory: () => getAccessToken(),
  })
  .withAutomaticReconnect()
  .build();

hubConnection.on("ReservationsChanged", () => {
  init();
});

hubConnection.onreconnecting(() => {
  showAlert(alertBox, "Canlı yeniləmə bağlantısı kəsildi, yenidən qoşulmağa çalışılır...", "warning");
});

hubConnection.onreconnected(() => {
  clearAlert(alertBox);
});

hubConnection.onclose(() => {
  showAlert(alertBox, "Canlı yeniləmə bağlantısı kəsildi. Yeni dəyişiklikləri görmək üçün səhifəni yeniləyin.", "warning");
});

hubConnection.start().catch((err) => console.error("SignalR bağlantısı qurula bilmədi:", err));