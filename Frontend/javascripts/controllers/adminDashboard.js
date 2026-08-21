import { isLoggedIn } from "../api/apiClient.js";
import { logout } from "../api/authApi.js";
import { getAllRooms, getAllBuildings } from "../api/roomsApi.js";
import { getAllUsersWithRoles } from "../api/adminApi.js";
import { getAllReservationsAdmin } from "../api/reservationsApi.js";
import { showAlert } from "../components/alert.js";

if (!isLoggedIn()) {
  window.location.href = "login.html";
}

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");

const roomCountEl = document.getElementById("statRoomCount");
const buildingCountEl = document.getElementById("statBuildingCount");
const userCountEl = document.getElementById("statUserCount");
const reservationCountEl = document.getElementById("statReservationCount");
const peakHourBadge = document.getElementById("peakHourBadge");

const CHART_COLORS = {
  violet: "#6E56CF",
  violetLight: "#C4B5F5",
  violetPeak: "#4C3A9E",
  amber: "#E8A23D",
  redLight: "#F5C2C0",
  grid: "#E4E7EC",
  text: "#6B7684",
};

Chart.defaults.font.family = "'Manrope', sans-serif";
Chart.defaults.color = CHART_COLORS.text;

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "login.html";
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

  const labels = hourCounts.map((_, h) => `${String(h).padStart(2, "0")}:00`);
  const colors = hourCounts.map((c, h) => (h === peakHour && c > 0 ? CHART_COLORS.violetPeak : CHART_COLORS.violetLight));

  new Chart(document.getElementById("peakHoursChart"), {
    type: "bar",
    data: {
      labels,
      datasets: [{ data: hourCounts, backgroundColor: colors, borderRadius: 6, maxBarThickness: 22 }],
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
  const confirmed = reservations.filter((r) => r.status === "Confirmed").length;
  const cancelled = reservations.filter((r) => r.status === "Cancelled").length;

  new Chart(document.getElementById("statusDonutChart"), {
    type: "doughnut",
    data: {
      labels: ["Təsdiqlənib", "Ləğv edilib"],
      datasets: [{ data: [confirmed, cancelled], backgroundColor: [CHART_COLORS.violet, CHART_COLORS.redLight], borderWidth: 0 }],
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

function buildRoomUsageChart(reservations) {
  const counts = {};
  reservations.forEach((r) => {
    counts[r.roomName] = (counts[r.roomName] || 0) + 1;
  });

  const sorted = Object.entries(counts).sort((a, b) => b[1] - a[1]);

  new Chart(document.getElementById("roomUsageChart"), {
    type: "bar",
    data: {
      labels: sorted.map(([name]) => name),
      datasets: [{ data: sorted.map(([, count]) => count), backgroundColor: CHART_COLORS.amber, borderRadius: 6, maxBarThickness: 30 }],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      indexAxis: "y",
      plugins: { legend: { display: false } },
      scales: {
        x: { beginAtZero: true, ticks: { precision: 0, font: { size: 10.5 } }, grid: { color: CHART_COLORS.grid } },
        y: { grid: { display: false }, ticks: { font: { size: 11 } } },
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
    reservationCountEl.textContent = reservations.filter((r) => r.status === "Confirmed").length;

    if (!reservations.length) {
      document.querySelectorAll(".chart-canvas-wrap").forEach((el) => {
        el.innerHTML = `<div class="empty-state" style="padding:2rem 1rem;"><i class="bi bi-bar-chart"></i>Hələ rezervasiya yoxdur.</div>`;
      });
      return;
    }

    buildPeakHoursChart(reservations);
    buildStatusDonutChart(reservations);
    buildRoomUsageChart(reservations);
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

init();