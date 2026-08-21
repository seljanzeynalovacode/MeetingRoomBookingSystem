import { confirmEmail, resendConfirmationEmail } from "../api/authApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

const form = document.getElementById("confirmForm");
const alertBox = document.getElementById("alertBox");
const submitBtn = document.getElementById("submitBtn");
const emailInput = document.getElementById("email");
const otpRow = document.getElementById("otpRow");
const otpBoxes = Array.from(otpRow.querySelectorAll(".otp-box"));
const resendLink = document.getElementById("resendLink");
const timerEl = document.getElementById("timer");

const params = new URLSearchParams(window.location.search);
const prefillEmail = params.get("email");
if (prefillEmail) emailInput.value = prefillEmail;

otpBoxes.forEach((box, idx) => {
  box.addEventListener("input", () => {
    box.value = box.value.replace(/\D/g, "");
    if (box.value && otpBoxes[idx + 1]) otpBoxes[idx + 1].focus();
  });

  box.addEventListener("keydown", (e) => {
    if (e.key === "Backspace" && !box.value && otpBoxes[idx - 1]) {
      otpBoxes[idx - 1].focus();
    }
  });

  box.addEventListener("paste", (e) => {
    const text = (e.clipboardData || window.clipboardData).getData("text").replace(/\D/g, "");
    if (!text) return;
    e.preventDefault();
    text.split("").slice(0, otpBoxes.length).forEach((digit, i) => {
      if (otpBoxes[i]) otpBoxes[i].value = digit;
    });
    const next = otpBoxes[Math.min(text.length, otpBoxes.length - 1)];
    if (next) next.focus();
  });
});

function getCode() {
  return otpBoxes.map((b) => b.value).join("");
}

function shakeOtp() {
  otpRow.classList.remove("shake");
  void otpRow.offsetWidth;
  otpRow.classList.add("shake");
}

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  clearAlert(alertBox);

  const email = emailInput.value.trim();
  const code = getCode();

  if (!email) {
    showAlert(alertBox, "Email daxil edin.", "warning");
    return;
  }

  if (code.length !== 6) {
    showAlert(alertBox, "6 rəqəmli kodu tam daxil edin.", "warning");
    shakeOtp();
    return;
  }

  submitBtn.disabled = true;
  submitBtn.innerHTML = "Yoxlanılır...";

  try {
    await confirmEmail({ Email: email, ConfirmationCode: code });
    window.location.href = "login.html?confirmed=1";
  } catch (err) {
    showAlert(alertBox, err.message);
    shakeOtp();
    otpBoxes.forEach((b) => (b.value = ""));
    otpBoxes[0].focus();
  } finally {
    submitBtn.disabled = false;
    submitBtn.innerHTML = 'Təsdiqlə <i class="bi bi-check2"></i>';
  }
});

let secondsLeft = 60;
let timerId = null;

function startTimer() {
  secondsLeft = 60;
  resendLink.classList.add("disabled");
  timerEl.style.display = "inline";
  updateTimerText();

  clearInterval(timerId);
  timerId = setInterval(() => {
    secondsLeft -= 1;
    if (secondsLeft <= 0) {
      clearInterval(timerId);
      timerEl.style.display = "none";
      resendLink.classList.remove("disabled");
      return;
    }
    updateTimerText();
  }, 1000);
}

function updateTimerText() {
  timerEl.textContent = `0:${String(secondsLeft).padStart(2, "0")}`;
}

resendLink.addEventListener("click", async (e) => {
  e.preventDefault();
  if (resendLink.classList.contains("disabled")) return;

  const email = emailInput.value.trim();
  if (!email) {
    showAlert(alertBox, "Əvvəlcə email daxil edin.", "warning");
    return;
  }

  clearAlert(alertBox);
  try {
    await resendConfirmationEmail({ Email: email });
    showAlert(alertBox, "Əgər bu email mövcuddursa, yeni kod göndərildi.", "success");
    startTimer();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
});

startTimer();