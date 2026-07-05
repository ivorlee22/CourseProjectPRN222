// Progress UI for the document upload form. The form posts the file via
// XMLHttpRequest so we can stream a progress bar back to the page; the final
// navigation still happens through the standard MVC response.
(function () {
  "use strict";

  const ready = (callback) => {
    if (document.readyState !== "loading") {
      callback();
    } else {
      document.addEventListener("DOMContentLoaded", callback);
    }
  };

  ready(() => {
    const form = document.querySelector("[data-upload-form]");
    if (!form) {
      return;
    }

    const submitButton = form.querySelector("[data-upload-submit]");
    const fileInput = form.querySelector("[data-upload-input]");
    const progress = form.querySelector("[data-upload-progress]");
    const progressBar = form.querySelector("[data-upload-progress-bar]");
    const progressLabel = form.querySelector("[data-upload-progress-label]");

    form.addEventListener("submit", (event) => {
      if (!fileInput || fileInput.files.length === 0) {
        return;
      }

      event.preventDefault();

      submitButton.disabled = true;
      progress.hidden = false;
      progressBar.style.width = "0%";
      progressBar.setAttribute("aria-valuenow", "0");
      progressLabel.textContent = "Đang tải lên (0%)";

      const xhr = new XMLHttpRequest();
      xhr.open(form.method, form.action, true);
      xhr.responseType = "document";

      xhr.upload.addEventListener("progress", (uploadEvent) => {
        if (!uploadEvent.lengthComputable) {
          return;
        }

        const percent = Math.round((uploadEvent.loaded / uploadEvent.total) * 100);
        progressBar.style.width = percent + "%";
        progressBar.setAttribute("aria-valuenow", percent.toString());
        progressLabel.textContent = "Đang tải lên (" + percent + "%)";
      });

      xhr.upload.addEventListener("load", () => {
        progressLabel.textContent = "Đang xử lý tài liệu...";
      });

      xhr.addEventListener("load", () => {
        // Re-render the page from the server response so MVC validation errors
        // are displayed identically to a non-JS submission.
        const responseDocument = xhr.response;
        if (responseDocument && responseDocument.documentElement) {
          document.open();
          document.write(responseDocument.documentElement.outerHTML);
          document.close();
          window.scrollTo({ top: 0, behavior: "smooth" });
        } else {
          window.location.reload();
        }
      });

      xhr.addEventListener("error", () => {
        progressLabel.textContent = "Tải lên thất bại. Vui lòng thử lại.";
        submitButton.disabled = false;
      });

      xhr.addEventListener("abort", () => {
        progressLabel.textContent = "Đã hủy tải lên.";
        submitButton.disabled = false;
      });

      const formData = new FormData(form);
      xhr.send(formData);
    });
  });
})();