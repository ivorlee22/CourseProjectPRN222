(function () {
  const region = document.querySelector("[data-course-live]");

  if (!region || !window.signalR) {
    return;
  }

  const listUrl = region.dataset.courseListUrl;
  const totalTarget = document.querySelector("[data-course-total]");

  if (!listUrl) {
    return;
  }

  let refreshInFlight = false;
  let refreshQueued = false;

  async function refreshCourseList() {
    if (refreshInFlight) {
      refreshQueued = true;
      return;
    }

    refreshInFlight = true;

    try {
      const response = await fetch(listUrl, {
        headers: {
          "X-Requested-With": "XMLHttpRequest"
        }
      });

      if (!response.ok) {
        return;
      }

      const html = await response.text();
      region.innerHTML = html;

      const countSource = region.querySelector("[data-course-total-count]");
      if (countSource && totalTarget) {
        totalTarget.textContent = countSource.dataset.courseTotalCount || "0";
      }
    } finally {
      refreshInFlight = false;
      if (refreshQueued) {
        refreshQueued = false;
        await refreshCourseList();
      }
    }
  }

  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/courses")
    .withAutomaticReconnect()
    .build();

  connection.on("CourseChanged", refreshCourseList);
  connection.on("ReceiveInvitation", refreshCourseList);
  connection.on("CancelInvitation", refreshCourseList);
  connection.start().catch(() => {
    // The list still works as a normal server-rendered page if realtime is unavailable.
  });
})();
