const workspace = document.querySelector("[data-chat-workspace]");

if (workspace) {
  const input = workspace.querySelector("[data-chat-input]");
  const form = workspace.querySelector("[data-chat-form]");
  const submit = workspace.querySelector("[data-chat-submit]");
  const status = workspace.querySelector("[data-chat-status]");
  const stream = workspace.querySelector("[data-message-stream]");
  const sessionRail = workspace.querySelector(".chat-session-rail");
  const sourcePanel = workspace.querySelector("[data-source-panel]");
  const sourceCount = workspace.querySelector("[data-source-count]");
  const backdrop = workspace.querySelector("[data-chat-backdrop]");

  const resizeInput = () => {
    if (!input) return;
    input.style.height = "auto";
    input.style.height = `${Math.min(input.scrollHeight, 160)}px`;
  };

  const closePanels = () => {
    sessionRail?.classList.remove("is-open");
    sourcePanel?.classList.remove("is-open");
    if (backdrop) backdrop.hidden = true;
    document.body.classList.remove("overflow-hidden");
  };

  const openPanel = (panel) => {
    closePanels();
    panel?.classList.add("is-open");
    if (backdrop) backdrop.hidden = false;
    document.body.classList.add("overflow-hidden");
  };

  input?.addEventListener("input", resizeInput);
  input?.addEventListener("keydown", (event) => {
    if (event.key !== "Enter" || event.shiftKey || event.isComposing) return;
    event.preventDefault();
    if (input.value.trim()) form?.requestSubmit();
  });
  resizeInput();

  workspace.querySelectorAll("[data-chat-suggestion]").forEach((button) => {
    button.addEventListener("click", () => {
      if (!input) return;
      input.value = button.dataset.chatSuggestion || "";
      resizeInput();
      input.focus();
    });
  });

  form?.addEventListener("submit", () => {
    if (!form.checkValidity()) return;
    if (submit) submit.disabled = true;
    if (input) input.readOnly = true;
    if (status) status.textContent = "Đang đọc tài liệu và tạo câu trả lời...";
  });

  workspace.querySelector("[data-open-sessions]")?.addEventListener("click", () => {
    openPanel(sessionRail);
  });
  workspace.querySelector("[data-open-sources]")?.addEventListener("click", () => {
    openPanel(sourcePanel);
  });
  workspace.querySelector("[data-close-sessions]")?.addEventListener("click", closePanels);
  workspace.querySelector("[data-close-sources]")?.addEventListener("click", closePanels);
  backdrop?.addEventListener("click", closePanels);

  workspace.querySelectorAll("[data-source-target]").forEach((button) => {
    button.addEventListener("click", () => {
      const id = button.dataset.sourceTarget;
      const groupId = button.dataset.sourceGroupTarget;
      const target = id ? document.getElementById(id) : null;
      const group = groupId ? document.getElementById(groupId) : null;
      if (!target) return;

      workspace.querySelectorAll("[data-source-group]").forEach((item) => {
        item.hidden = item !== group;
      });
      if (sourceCount && group) {
        sourceCount.textContent = `${group.dataset.sourceGroupCount || 0} đoạn được dùng`;
      }

      if (window.matchMedia("(max-width: 1199.98px)").matches) {
        openPanel(sourcePanel);
      }

      workspace.querySelectorAll(".chat-source-card.is-highlighted").forEach((card) => {
        card.classList.remove("is-highlighted");
      });
      target.classList.add("is-highlighted");
      window.setTimeout(() => {
        target.scrollIntoView({ behavior: "smooth", block: "center" });
        target.focus({ preventScroll: true });
      }, 180);
    });
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closePanels();
  });

  if (stream) {
    stream.scrollTop = stream.scrollHeight;
  }
}
