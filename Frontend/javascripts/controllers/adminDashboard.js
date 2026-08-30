import { requireAdmin } from "../components/adminGuard.js";
import { logout } from "../api/authApi.js";
import { getAllRooms, getAllBuildings } from "../api/roomsApi.js";
import { getAllUsersWithRoles } from "../api/adminApi.js";
import { getAllReservationsAdmin } from "../api/reservationsApi.js";
import { showAlert } from "../components/alert.js";

requireAdmin();

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
  violetLight: cssVar("--violet-300"),
  violet: cssVar("--violet-500"),
  violetPeak: cssVar("--violet-600"),
  amber: cssVar("--amber-500"),
  green: cssVar("--green-500"),
  red: cssVar("--red-500"),
  grid: cssVar("--border"),
  text: cssVar("--text-secondary"),
};

const PASTEL_PALETTE = [
  "oklch(72% 0.19 350)",
  "oklch(65% 0.16 300)",
  "oklch(78% 0.17 55)",
  "oklch(85% 0.16 95)",
];

Chart.defaults.font.family = "'Manrope', sans-serif";
Chart.defaults.color = CHART_COLORS.text;

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
  const colors = visibleCounts.map((c, i) => (firstHour + i === peakHour && c > 0 ? CHART_COLORS.violetPeak : CHART_COLORS.violetLight));

  new Chart(document.getElementById("peakHoursChart"), {
    type: "bar",
    data: {
      labels,
      datasets: [{ data: visibleCounts, backgroundColor: colors, borderRadius: 6, maxBarThickness: 22 }],
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

  new Chart(document.getElementById("statusDonutChart"), {
    type: "doughnut",
    data: {
      labels: ["Aktiv", "Tamamlanıb", "Ləğv edilib"],
      datasets: [{ data: [confirmed, completed, cancelled], backgroundColor: [CHART_COLORS.green, CHART_COLORS.amber, CHART_COLORS.red], borderWidth: 0 }],
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

  const colors = counts.map((c, i) => (i === peakDayIndex && c > 0 ? CHART_COLORS.violetPeak : CHART_COLORS.violetLight));

  new Chart(document.getElementById("weekdayChart"), {
    type: "bar",
    data: {
      labels: WEEKDAY_LABELS,
      datasets: [{ data: counts, backgroundColor: colors, borderRadius: 6, maxBarThickness: 40 }],
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

  new Chart(document.getElementById("buildingChart"), {
    type: "pie",
    data: {
      labels: sorted.map(([name]) => name),
      datasets: [{ data: sorted.map(([, count]) => count), backgroundColor: sorted.map((_, i) => PASTEL_PALETTE[i % PASTEL_PALETTE.length]), borderWidth: 0 }],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { position: "bottom", labels: { boxWidth: 10, boxHeight: 10, padding: 12, font: { size: 11.5 } } },
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
      document.querySelectorAll(".chart-canvas-wrap").forEach((el) => {
        el.innerHTML = `<div class="empty-state" style="padding:2rem 1rem;"><i class="bi bi-bar-chart"></i>Hələ rezervasiya yoxdur.</div>`;
      });
      return;
    }

    buildPeakHoursChart(reservations);
    buildStatusDonutChart(reservations);
    buildWeekdayChart(reservations);
    buildBuildingChart(reservations, rooms);
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

init();