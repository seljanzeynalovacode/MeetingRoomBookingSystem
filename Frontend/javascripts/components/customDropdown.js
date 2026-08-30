function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

export function initDropdown({ btn, panel, options, value, placeholder = "Seçin", onChange, portal = false }) {
  let selected = value ?? "";
  const labelEl = btn.querySelector("span");
  const homeParent = panel.parentNode;
  const homeNextSibling = panel.nextSibling;

  function open() {
    if (portal) {
      const rect = btn.getBoundingClientRect();
      panel.style.position = "fixed";
      panel.style.top = `${rect.bottom + 6}px`;
      panel.style.left = `${rect.left}px`;
      panel.style.right = "auto";
      panel.style.width = `${rect.width}px`;
      panel.style.zIndex = "350";
      document.body.appendChild(panel);
    }
    panel.classList.add("open");
    btn.classList.add("open");
  }

  function close() {
    panel.classList.remove("open");
    btn.classList.remove("open");
    if (portal && panel.parentNode === document.body) {
      if (homeNextSibling) homeParent.insertBefore(panel, homeNextSibling);
      else homeParent.appendChild(panel);
    }
  }

  function markActive() {
    panel.querySelectorAll(".dd-option").forEach((el) => {
      el.classList.toggle("active", el.dataset.value === selected);
    });
  }

  function updateLabel() {
    const match = options.find((o) => o.value === selected);
    if (match) {
      labelEl.textContent = match.label;
      btn.classList.remove("placeholder");
    } else {
      labelEl.textContent = placeholder;
      btn.classList.add("placeholder");
    }
  }

  function render() {
    panel.innerHTML = options
      .map((o) => `<div class="dd-option${o.value === selected ? " active" : ""}" data-value="${escapeHtml(o.value)}">${escapeHtml(o.label)}</div>`)
      .join("");

    panel.querySelectorAll(".dd-option").forEach((el) => {
      el.addEventListener("click", () => {
        selected = el.dataset.value;
        close();
        updateLabel();
        markActive();
        onChange(selected);
      });
    });
  }

  btn.addEventListener("click", (e) => {
    if (btn.disabled) return;
    e.stopPropagation();
    if (panel.classList.contains("open")) close();
    else open();
  });
  document.addEventListener("click", close);
  panel.addEventListener("click", (e) => e.stopPropagation());

  render();
  updateLabel();

  return {
    setOptions(newOptions, newValue) {
      options = newOptions;
      if (newValue !== undefined) selected = newValue;
      else if (!options.some((o) => o.value === selected)) selected = "";
      render();
      updateLabel();
    },
    setValue(newValue) {
      selected = newValue ?? "";
      updateLabel();
      markActive();
    },
    getValue() {
      return selected;
    },
    setDisabled(disabled) {
      btn.disabled = disabled;
      if (disabled) close();
    },
    setPlaceholder(newPlaceholder) {
      placeholder = newPlaceholder;
      updateLabel();
    },
  };
}
