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
  const citationModalElement = workspace.querySelector("#citationDetailModal");
  const citationModal = citationModalElement && window.bootstrap
    ? new window.bootstrap.Modal(citationModalElement)
    : null;
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

  const escapeHtml = (value) => value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");

  const renderMarkdownInline = (value) => escapeHtml(value)
    .replace(/`([^`]+?)`/g, "<code>$1</code>")
    .replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>");

  const renderMarkdown = (value) => {
    const lines = value.replace(/\r\n/g, "\n").replace(/\r/g, "\n").split("\n");
    const parts = [];
    let activeList = "";
    let paragraphOpen = false;
    let codeBlockOpen = false;

    const closeParagraph = () => {
      if (!paragraphOpen) return;
      parts.push("</p>");
      paragraphOpen = false;
    };
    const closeList = () => {
      if (!activeList) return;
      parts.push(activeList === "ol" ? "</ol>" : "</ul>");
      activeList = "";
    };
    const openList = (kind) => {
      if (activeList === kind) return;
      closeList();
      parts.push(kind === "ol" ? "<ol>" : "<ul>");
      activeList = kind;
    };
    const closeCodeBlock = () => {
      if (!codeBlockOpen) return;
      parts.push("</code></pre>");
      codeBlockOpen = false;
    };

    lines.forEach((rawLine) => {
      const line = rawLine.trimEnd();
      const trimmedLine = line.trim();
      if (trimmedLine.startsWith("```")) {
        if (codeBlockOpen) {
          closeCodeBlock();
        } else {
          closeParagraph();
          closeList();
          const language = trimmedLine.slice(3).trim();
          parts.push("<pre class=\"chat-code-block\"><code");
          if (language) {
            parts.push(` data-code-language="${escapeHtml(language)}"`);
          }
          parts.push(">");
          codeBlockOpen = true;
        }
        return;
      }

      if (codeBlockOpen) {
        parts.push(escapeHtml(rawLine));
        parts.push("\n");
        return;
      }

      if (!line.trim()) {
        closeParagraph();
        closeList();
        return;
      }

      const bulletMatch = line.match(/^\s*[-*]\s+(.+)$/);
      if (bulletMatch) {
        closeParagraph();
        openList("ul");
        parts.push(`<li>${renderMarkdownInline(bulletMatch[1].trim())}</li>`);
        return;
      }

      const numberMatch = line.match(/^\s*\d+[.)]\s+(.+)$/);
      if (numberMatch) {
        closeParagraph();
        openList("ol");
        parts.push(`<li>${renderMarkdownInline(numberMatch[1].trim())}</li>`);
        return;
      }

      closeList();
      if (paragraphOpen) {
        parts.push("<br>");
      } else {
        parts.push("<p>");
        paragraphOpen = true;
      }
      parts.push(renderMarkdownInline(line.trim()));
    });

    closeParagraph();
    closeList();
    closeCodeBlock();
    return parts.join("");
  };

  const highlightCode = (value) => {
    let html = escapeHtml(value);
    html = html.replace(
      /(&quot;.*?&quot;|&#39;.*?&#39;|`.*?`)/g,
      '<span class="chat-code-token chat-code-token--string">$1</span>'
    );
    html = html.replace(
      /\b(await|async|class|const|decimal|else|false|for|foreach|if|int|let|new|null|private|public|return|string|true|using|var|void|while)\b/g,
      '<span class="chat-code-token chat-code-token--keyword">$1</span>'
    );
    html = html.replace(
      /(\/\/.*)$/gm,
      '<span class="chat-code-token chat-code-token--comment">$1</span>'
    );
    return html;
  };

  const decorateCodeBlocks = (root = workspace) => {
    root.querySelectorAll(".chat-code-block:not([data-code-ready])").forEach((block) => {
      const code = block.querySelector("code");
      const language = code?.dataset.codeLanguage;
      const rawCode = code?.textContent || "";
      block.dataset.codeReady = "true";
      if (code) {
        code.dataset.rawCode = rawCode;
        code.innerHTML = highlightCode(rawCode);
      }

      if (language) {
        const badge = document.createElement("span");
        badge.className = "chat-code-block__language";
        badge.textContent = language;
        block.append(badge);
      }

      const button = document.createElement("button");
      button.type = "button";
      button.className = "chat-code-copy";
      button.textContent = "Copy";
      button.addEventListener("click", async () => {
        const text = code?.dataset.rawCode || code?.textContent || "";
        if (!text) return;

        try {
          await navigator.clipboard.writeText(text);
          button.textContent = "Đã copy";
          window.setTimeout(() => {
            button.textContent = "Copy";
          }, 1600);
        } catch {
          button.textContent = "Không copy được";
          window.setTimeout(() => {
            button.textContent = "Copy";
          }, 1600);
        }
      });
      block.append(button);
    });
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
    const contentNode = document.createElement("div");
    contentNode.className = "chat-message__content";
    contentNode.innerHTML = renderMarkdown(content);
    decorateCodeBlocks(contentNode);
    meta.append(author, time);
    body.append(meta, contentNode);
    article.append(identity, body);
    stream.append(article);
    stream.scrollTop = stream.scrollHeight;
    return contentNode;
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
    let streamError = "";
    let streamedAnswer = "";

    connection.stream(
      "SendMessage",
      sessionId,
      question,
      requestVerificationToken
    ).subscribe({
      next: (item) => {
        if (item.type === "delta" && answer) {
          streamedAnswer += item.content || "";
          answer.innerHTML = renderMarkdown(streamedAnswer);
          decorateCodeBlocks(answer);
          stream.scrollTop = stream.scrollHeight;
        }
        if (item.type === "error") {
          streamError = item.content || "Không thể gửi câu hỏi lúc này.";
          if (answer) {
            answer.innerHTML = renderMarkdown(streamError);
            decorateCodeBlocks(answer);
            answer.closest(".chat-message")?.classList.add("chat-message--notice");
            stream.scrollTop = stream.scrollHeight;
          }
        }
        if (item.type === "completed") completed = true;
      },
      error: () => {
        setComposerBusy(false, "Chưa gửi xong. Qb thử lại giúp xha nhen.");
        input.value = question;
        resizeInput();
      },
      complete: () => {
        if (streamError) {
          setComposerBusy(false, streamError);
          input.value = question;
          resizeInput();
          return;
        }
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

  workspace.querySelectorAll("[data-citation-modal]").forEach((button) => {
    button.addEventListener("click", () => {
      if (!citationModalElement || !citationModal) return;

      const meta = citationModalElement.querySelector("[data-citation-modal-meta]");
      const title = citationModalElement.querySelector("[data-citation-modal-title]");
      const location = citationModalElement.querySelector("[data-citation-modal-location]");
      const content = citationModalElement.querySelector("[data-citation-modal-content]");

      if (meta) {
        meta.textContent = `[${button.dataset.citationRank}] ${button.dataset.citationScore}% phù hợp`;
      }
      if (title) {
        title.textContent = button.dataset.citationTitle || "Nguồn tham khảo";
      }
      if (location) {
        location.textContent = button.dataset.citationLocation || "";
      }
      if (content) {
        content.textContent = button.dataset.citationContent || "";
      }

      citationModal.show();
    });
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closePanels();
  });

  if (stream?.querySelector(".chat-message")) {
    decorateCodeBlocks(stream);
    stream.scrollTop = stream.scrollHeight;
  }

  window.requestAnimationFrame(() => {
    window.scrollTo(0, 0);
  });
}
