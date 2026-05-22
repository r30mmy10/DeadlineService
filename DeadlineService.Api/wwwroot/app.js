const apiBase = window.location.origin;
let token = localStorage.getItem("token") || "";

async function register() {
  const email = document.getElementById("registerEmail")?.value;
  const password = document.getElementById("registerPassword")?.value;

  const response = await fetch(`${apiBase}/api/Auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  });

  const data = await response.json().catch(() => ({}));

  if (response.ok) {
    alert("Пользователь зарегистрирован");
  } else {
    alert(typeof data === "string" ? data : "Ошибка регистрации");
  }
}

async function login() {
  const email = document.getElementById("loginEmail")?.value;
  const password = document.getElementById("loginPassword")?.value;

  const response = await fetch(`${apiBase}/api/Auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  });

  const data = await response.json().catch(() => ({}));

  if (!response.ok) {
    alert("Ошибка входа");
    return;
  }

  token = data.token;
  localStorage.setItem("token", token);
  window.location.href = "/tasks.html";
}

function initTasksPage() {
  token = localStorage.getItem("token") || "";

  if (!token) {
    window.location.href = "/";
    return;
  }

  updateStatus();
  updateAdminButton();
  loadTasks();
}

function logout() {
  localStorage.removeItem("token");
  token = "";
  window.location.href = "/";
}

function updateStatus() {
  const status = document.getElementById("status");
  if (status) {
    status.textContent = token ? "В сети" : "Гость";
  }
}

function emptyState(message) {
  return `<div class="empty-state"><div class="empty-state-icon">📋</div><p>${message}</p></div>`;
}

function statusBadge(status) {
  const key = String(status || "").toLowerCase().replace(/\s+/g, "");
  const cls = key === "done" ? "badge-done" : key === "inprogress" ? "badge-inprogress" : "badge-pending";
  return `<span class="badge ${cls}">${escapeHtml(status)}</span>`;
}

function deliveryBadge(status) {
  const key = String(status || "").toLowerCase();
  const cls = key === "sent" ? "badge-sent" : key === "failed" ? "badge-failed" : "badge-pending-status";
  return `<span class="badge ${cls}">${escapeHtml(status)}</span>`;
}

function channelBadge(channel) {
  const key = String(channel || "").toLowerCase().replace(/-/g, "_");
  const cls = key.includes("email") ? "badge-email" : "badge-in_app";
  return `<span class="badge ${cls}">${escapeHtml(channel)}</span>`;
}

function updateAdminButton() {
  const adminBtn = document.getElementById("adminPanelBtn");
  if (!adminBtn) return;

  const role = getUserRoleFromToken();
  if (role === "Admin") {
    adminBtn.classList.remove("hidden");
  } else {
    adminBtn.classList.add("hidden");
  }
}

function buildTaskFilterQuery() {
  const params = new URLSearchParams();
  const status = document.getElementById("filterStatus")?.value;
  const from = document.getElementById("filterDeadlineFrom")?.value;
  const to = document.getElementById("filterDeadlineTo")?.value;
  const overdue = document.getElementById("filterOverdue")?.checked;

  if (status) params.set("status", status);
  if (from) params.set("deadlineFrom", new Date(from).toISOString());
  if (to) params.set("deadlineTo", new Date(to).toISOString());
  if (overdue) params.set("overdueOnly", "true");

  const qs = params.toString();
  return qs ? `?${qs}` : "";
}

function resetTaskFilters() {
  const status = document.getElementById("filterStatus");
  const from = document.getElementById("filterDeadlineFrom");
  const to = document.getElementById("filterDeadlineTo");
  const overdue = document.getElementById("filterOverdue");
  if (status) status.value = "";
  if (from) from.value = "";
  if (to) to.value = "";
  if (overdue) overdue.checked = false;
  loadTasks();
}

async function loadTasks() {
  if (!token) {
    alert("Сначала войди");
    return;
  }

  const response = await fetch(`${apiBase}/api/Tasks/my${buildTaskFilterQuery()}`, {
    headers: {
      "Authorization": `Bearer ${token}`
    }
  });

  const tasks = await response.json().catch(() => []);
  const container = document.getElementById("tasks");

  if (!container) return;

  container.innerHTML = "";

  if (!Array.isArray(tasks) || tasks.length === 0) {
    container.innerHTML = emptyState("Задач пока нет — создайте первую выше");
    return;
  }

  for (const task of tasks) {
    const div = document.createElement("div");
    div.className = "item-card";

    div.innerHTML = `
      <div class="item-card-title">${escapeHtml(task.title)} ${statusBadge(task.status)}</div>
      <div class="item-meta"><b>Дедлайн:</b> ${formatDate(task.deadlineAt)} · <b>Приоритет:</b> ${escapeHtml(task.priority)}</div>
      ${task.description ? `<div class="item-meta">${escapeHtml(task.description)}</div>` : ""}

      <div class="item-actions">
        <button class="btn-ghost" onclick="toggleEdit('${task.id}')">Редактировать</button>
        <button class="btn-danger" onclick="deleteTask('${task.id}')">Удалить</button>
      </div>

      <div id="edit-${task.id}" class="edit-panel hidden">
        <h3>Редактирование</h3>
        <input id="title-${task.id}" value="${escapeHtml(task.title)}" />
        <textarea id="desc-${task.id}">${escapeHtml(task.description ?? "")}</textarea>
        <input id="deadline-${task.id}" value="${toLocalInputValue(task.deadlineAt)}" type="datetime-local" />
        <input id="status-${task.id}" value="${escapeHtml(task.status)}" />
        <input id="priority-${task.id}" value="${escapeHtml(task.priority)}" />
        <button onclick="updateTask('${task.id}')">Сохранить изменения</button>
      </div>
    `;

    container.appendChild(div);
  }
}

function toggleEdit(taskId) {
  const panel = document.getElementById(`edit-${taskId}`);
  if (!panel) return;
  panel.classList.toggle("hidden");
}

async function createTask() {
  if (!token) {
    alert("Сначала войди");
    return;
  }

  const title = document.getElementById("taskTitle").value;
  const description = document.getElementById("taskDescription").value;
  const deadlineAt = document.getElementById("taskDeadline").value;
  const priority = document.getElementById("taskPriority").value;
  const remindValue = parseInt(document.getElementById("remindValue")?.value || "1", 10);
  const remindUnit = document.getElementById("remindUnit")?.value || "Hours";
  const notifyByEmail = document.getElementById("notifyEmail")?.checked ?? true;
  const notifyInApp = document.getElementById("notifyInApp")?.checked ?? true;

  const response = await fetch(`${apiBase}/api/Tasks`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${token}`
    },
    body: JSON.stringify({
      title,
      description,
      deadlineAt: new Date(deadlineAt).toISOString(),
      priority,
      reminder: {
        remindBeforeValue: remindValue,
        remindBeforeUnit: remindUnit,
        notifyByEmail,
        notifyInApp
      }
    })
  });

  if (!response.ok) {
    alert("Ошибка создания задачи");
    return;
  }

  document.getElementById("taskTitle").value = "";
  document.getElementById("taskDescription").value = "";
  document.getElementById("taskDeadline").value = "";
  document.getElementById("taskPriority").value = "Medium";

  alert("Задача создана");
  loadTasks();
}

async function updateTask(taskId) {
  const title = document.getElementById(`title-${taskId}`).value;
  const description = document.getElementById(`desc-${taskId}`).value;
  const deadlineAt = document.getElementById(`deadline-${taskId}`).value;
  const status = document.getElementById(`status-${taskId}`).value;
  const priority = document.getElementById(`priority-${taskId}`).value;

  const response = await fetch(`${apiBase}/api/Tasks/${taskId}`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${token}`
    },
    body: JSON.stringify({
      title,
      description,
      deadlineAt: new Date(deadlineAt).toISOString(),
      status,
      priority
    })
  });

  if (!response.ok) {
    alert("Ошибка обновления задачи");
    return;
  }

  alert("Задача обновлена");
  loadTasks();
}

async function deleteTask(taskId) {
  const response = await fetch(`${apiBase}/api/Tasks/${taskId}`, {
    method: "DELETE",
    headers: {
      "Authorization": `Bearer ${token}`
    }
  });

  if (!response.ok) {
    alert("Ошибка удаления задачи");
    return;
  }

  alert("Задача удалена");
  loadTasks();
}

function toLocalInputValue(isoString) {
  const date = new Date(isoString);
  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60000);
  return local.toISOString().slice(0, 16);
}

function formatDate(isoString) {
  const date = new Date(isoString);
  return date.toLocaleString("ru-RU");
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function getUserRoleFromToken() {
  const currentToken = localStorage.getItem("token") || "";
  if (!currentToken) return null;

  try {
    const payload = JSON.parse(atob(currentToken.split(".")[1]));
    return payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ?? null;
  } catch {
    return null;
  }
}

function initAdminPage() {
  token = localStorage.getItem("token") || "";

  if (!token) {
    window.location.href = "/";
    return;
  }

  const role = getUserRoleFromToken();
  const status = document.getElementById("adminStatus");

  if (role !== "Admin") {
    if (status) status.textContent = "Нет доступа: нужен пользователь с ролью Admin";
    return;
  }

  if (status) {
    status.textContent = "Администратор";
    status.className = "status-pill";
  }
  loadAdminData();
}

async function loadAdminData() {
  await loadAdminUsers();
  await loadAdminTasks();
  await loadAdminNotifications();
}

async function loadAdminNotifications() {
  const response = await fetch(`${apiBase}/api/Notifications/admin/all`, {
    headers: { "Authorization": `Bearer ${token}` }
  });

  const items = await response.json().catch(() => []);
  const container = document.getElementById("adminNotifications");
  if (!container) return;

  container.innerHTML = "";
  if (!Array.isArray(items) || items.length === 0) {
    container.innerHTML = emptyState("Уведомлений пока нет");
    return;
  }

  for (const n of items) {
    const div = document.createElement("div");
    div.className = "item-card";
    div.innerHTML = `
      <div class="item-card-title">${channelBadge(n.channel)} ${deliveryBadge(n.deliveryStatus)}</div>
      <div class="item-meta"><b>Пользователь:</b> ${escapeHtml(n.userEmail ?? "")}</div>
      <div class="item-meta">${escapeHtml(n.message)}</div>
      <div class="item-meta"><b>Создано:</b> ${formatDate(n.createdAt)}</div>
    `;
    container.appendChild(div);
  }
}

async function loadAdminUsers() {
  const response = await fetch(`${apiBase}/api/Admin/users`, {
    headers: {
      "Authorization": `Bearer ${token}`
    }
  });

  const users = await response.json().catch(() => []);
  const container = document.getElementById("adminUsers");
  if (!container) return;

  container.innerHTML = "";

  if (!Array.isArray(users) || users.length === 0) {
    container.innerHTML = emptyState("Пользователей нет");
    return;
  }

  for (const user of users) {
    const div = document.createElement("div");
    div.className = "item-card";
    div.innerHTML = `
      <div class="item-card-title">${escapeHtml(user.email)}</div>
      <div class="item-meta"><b>Роль:</b> ${escapeHtml(user.role)} · <b>Блок:</b> ${user.isBlocked ? "да" : "нет"}</div>
      <div class="item-actions">
        ${user.isBlocked
          ? `<button class="btn-ghost" onclick="setUserBlocked('${user.id}', false)">Разблокировать</button>`
          : `<button class="btn-danger" onclick="setUserBlocked('${user.id}', true)">Заблокировать</button>`}
      </div>
    `;
    container.appendChild(div);
  }
}

async function loadAdminTasks() {
  const response = await fetch(`${apiBase}/api/Admin/tasks`, {
    headers: {
      "Authorization": `Bearer ${token}`
    }
  });

  const tasks = await response.json().catch(() => []);
  const container = document.getElementById("adminTasks");
  if (!container) return;

  container.innerHTML = "";

  if (!Array.isArray(tasks) || tasks.length === 0) {
    container.innerHTML = emptyState("Задач нет");
    return;
  }

  for (const task of tasks) {
    const div = document.createElement("div");
    div.className = "item-card";

    div.innerHTML = `
      <div class="item-card-title">${escapeHtml(task.title)} ${statusBadge(task.status)}</div>
      <div class="item-meta"><b>Пользователь:</b> ${escapeHtml(task.userEmail ?? "")}</div>
      <div class="item-meta"><b>Дедлайн:</b> ${formatDate(task.deadlineAt)} · ${escapeHtml(task.priority)}</div>

      <div class="item-actions">
        <button class="btn-ghost" onclick="toggleAdminEdit('${task.id}')">Редактировать</button>
      </div>

      <div id="admin-edit-${task.id}" class="edit-panel hidden">
        <h3>Редактирование задачи</h3>
        <input id="admin-title-${task.id}" value="${escapeHtml(task.title)}" />
        <textarea id="admin-desc-${task.id}">${escapeHtml(task.description ?? "")}</textarea>
        <input id="admin-deadline-${task.id}" value="${toLocalInputValue(task.deadlineAt)}" type="datetime-local" />
        <input id="admin-status-${task.id}" value="${escapeHtml(task.status)}" />
        <input id="admin-priority-${task.id}" value="${escapeHtml(task.priority)}" />
        <button onclick="adminUpdateTask('${task.id}')">Сохранить изменения</button>
      </div>
    `;

    container.appendChild(div);
  }
}

function toggleAdminEdit(taskId) {
  const panel = document.getElementById(`admin-edit-${taskId}`);
  if (!panel) return;
  panel.classList.toggle("hidden");
}

async function adminUpdateTask(taskId) {
  const title = document.getElementById(`admin-title-${taskId}`).value;
  const description = document.getElementById(`admin-desc-${taskId}`).value;
  const deadlineAt = document.getElementById(`admin-deadline-${taskId}`).value;
  const status = document.getElementById(`admin-status-${taskId}`).value;
  const priority = document.getElementById(`admin-priority-${taskId}`).value;

  const response = await fetch(`${apiBase}/api/Admin/tasks/${taskId}`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${token}`
    },
    body: JSON.stringify({
      title,
      description,
      deadlineAt: new Date(deadlineAt).toISOString(),
      status,
      priority
    })
  });

  if (!response.ok) {
    alert("Ошибка обновления задачи администратором");
    return;
  }

  alert("Задача обновлена");
  loadAdminTasks();
}

function initNotificationsPage() {
  token = localStorage.getItem("token") || "";
  if (!token) {
    window.location.href = "/";
    return;
  }
  loadNotifications();
}

async function loadNotifications() {
  const response = await fetch(`${apiBase}/api/Notifications/my`, {
    headers: { "Authorization": `Bearer ${token}` }
  });

  const items = await response.json().catch(() => []);
  const container = document.getElementById("notificationsList");
  if (!container) return;

  container.innerHTML = "";
  if (!Array.isArray(items) || items.length === 0) {
    container.innerHTML = emptyState("Пока пусто — создайте задачу с напоминанием");
    return;
  }

  for (const n of items) {
    const div = document.createElement("div");
    div.className = "item-card";
    div.innerHTML = `
      <div class="item-card-title">${channelBadge(n.channel)} ${deliveryBadge(n.deliveryStatus)}</div>
      <div class="item-meta">${escapeHtml(n.message)}</div>
      <div class="item-meta"><b>Создано:</b> ${formatDate(n.createdAt)} · <b>Отправлено:</b> ${n.sentAt ? formatDate(n.sentAt) : "—"}</div>
    `;
    container.appendChild(div);
  }
}

async function setUserBlocked(userId, isBlocked) {
  const response = await fetch(`${apiBase}/api/Admin/users/${userId}/block`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${token}`
    },
    body: JSON.stringify({ isBlocked })
  });

  if (!response.ok) {
    alert("Не удалось изменить статус пользователя");
    return;
  }

  alert(isBlocked ? "Пользователь заблокирован" : "Пользователь разблокирован");
  loadAdminUsers();
}