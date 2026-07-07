const workspace = document.querySelector("[data-chat-workspace]");

if (workspace) {
  if ("scrollRestoration" in window.history) {
    window.history.scrollRestoration = "manual";
  }

  const input = workspace.querySelector("[data-chat-input]");
  const form = workspace.querySelector("[data-chat-form]");
  const submit = workspace.querySelector("[data-chat-submit]");
  const status = workspace.querySelector("[data-chat-status]");
  const stream = workspace.querySelector("[data-message-stream]");
  const sessionRail = workspace.querySelector(".chat-session-rail");
  const sourcePanel = workspace.querySelector("[data-source-panel]");
  const sourceCount = workspace.querySelector("[data-source-count]");
  const backdrop = workspace.querySelector("[data-chat-backdrop]");
  let connection;
  let isStreaming = false;

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

  const setComposerBusy = (busy, message = "") => {
    isStreaming = busy;
    if (submit) submit.disabled = busy;
    if (input) input.readOnly = busy;
    if (status) status.textContent = message;
  };

  const createMessage = (role, content) => {
    if (!stream) return null;
    stream.querySelector(".chat-welcome")?.remove();
    const article = document.createElement("article");
    article.className = `chat-message chat-message--${role}`;
    const identity = document.createElement("div");
    identity.className = "chat-message__identity";
    identity.setAttribute("aria-hidden", "true");
    identity.textContent = role === "user" ? "B" : "E";
    const body = document.createElement("div");
    body.className = "chat-message__body";
    const meta = document.createElement("div");
    meta.className = "chat-message__meta";
    const author = document.createElement("strong");
    author.textContent = role === "user" ? "Bạn" : "Edu Assistant";
    const time = document.createElement("time");
    time.textContent = new Date().toLocaleTimeString("vi-VN", {
      hour: "2-digit",
      minute: "2-digit"
    });
    const paragraph = document.createElement("p");
    paragraph.textContent = content;
    meta.append(author, time);
    body.append(meta, paragraph);
    article.append(identity, body);
    stream.append(article);
    stream.scrollTop = stream.scrollHeight;
    return paragraph;
  };

  const startConnection = async () => {
    if (!form || !window.signalR) return;
    connection = new window.signalR.HubConnectionBuilder()
      .withUrl("/hubs/chat")
      .withAutomaticReconnect()
      .build();
    connection.onreconnecting(() => {
      if (!isStreaming && status) status.textContent = "Đang nối lại trợ lý...";
    });
    connection.onreconnected(() => {
      if (!isStreaming && status) status.textContent = "";
    });
    connection.onclose(() => {
      if (!isStreaming && status) {
        status.textContent = "SignalR đang ngắt. Lần gửi tiếp theo sẽ dùng chế độ thường.";
      }
    });
    try {
      await connection.start();
    } catch {
      connection = null;
    }
  };

  form?.addEventListener("submit", (event) => {
    if (!form.checkValidity()) return;
    if (!connection || connection.state !== window.signalR.HubConnectionState.Connected) {
      setComposerBusy(true, "Đang gửi bằng chế độ thường...");
      return;
    }

    event.preventDefault();
    if (isStreaming || !input) return;
    const question = input.value.trim();
    const sessionId = form.querySelector('[name="sessionId"]')?.value;
    const requestVerificationToken = form.querySelector(
      '[name="__RequestVerificationToken"]'
    )?.value;
    if (!question || !sessionId || !requestVerificationToken) return;

    createMessage("user", question);
    const answer = createMessage("assistant", "");
    input.value = "";
    resizeInput();
    setComposerBusy(true, "Đang đọc tài liệu và tạo câu trả lời...");
    let completed = false;

    connection.stream(
      "SendMessage",
      sessionId,
      question,
      requestVerificationToken
    ).subscribe({
      next: (item) => {
        if (item.type === "delta" && answer) {
          answer.textContent += item.content || "";
          stream.scrollTop = stream.scrollHeight;
        }
        if (item.type === "completed") completed = true;
      },
      error: () => {
        setComposerBusy(false, "Chưa gửi xong. Qb thử lại giúp xha nhen.");
        input.value = question;
        resizeInput();
      },
      complete: () => {
        if (completed) {
          window.location.reload();
          return;
        }
        setComposerBusy(false, "Luồng trả lời kết thúc sớm. Qb thử lại nhen.");
      }
    });
  });

  void startConnection();

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

  if (stream?.querySelector(".chat-message")) {
    stream.scrollTop = stream.scrollHeight;
  }

  window.requestAnimationFrame(() => {
    window.scrollTo(0, 0);
  });
}
