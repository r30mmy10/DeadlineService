const apiBase = "http://localhost:5283";
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
    status.textContent = token ? "Авторизован" : "Не авторизован";
  }
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

async function loadTasks() {
  if (!token) {
    alert("Сначала войди");
    return;
  }

  const response = await fetch(`${apiBase}/api/Tasks/my`, {
    headers: {
      "Authorization": `Bearer ${token}`
    }
  });

  const tasks = await response.json().catch(() => []);
  const container = document.getElementById("tasks");

  if (!container) return;

  container.innerHTML = "";

  if (!Array.isArray(tasks) || tasks.length === 0) {
    container.innerHTML = "<p>Задач пока нет.</p>";
    return;
  }

  for (const task of tasks) {
    const div = document.createElement("div");
    div.className = "task";

    div.innerHTML = `
      <div class="task-title">${escapeHtml(task.title)}</div>
      <div class="task-meta"><b>Описание:</b> ${escapeHtml(task.description ?? "")}</div>
      <div class="task-meta"><b>Дедлайн:</b> ${formatDate(task.deadlineAt)}</div>
      <div class="task-meta"><b>Статус:</b> ${escapeHtml(task.status)}</div>
      <div class="task-meta"><b>Приоритет:</b> ${escapeHtml(task.priority)}</div>

      <div class="task-actions">
        <button onclick="toggleEdit('${task.id}')">Редактировать</button>
        <button class="danger-btn" onclick="deleteTask('${task.id}')">Удалить</button>
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
      priority
    })
  });

  if (!response.ok) {
    alert("Ошибка создания задачи");
    return;
  }

  document.getElementById("taskTitle").value = "";
  document.getElementById("taskDescription").value = "";
  document.getElementById("taskDeadline").value = "";
  document.getElementById("taskPriority").value = "High";

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

  if (status) status.textContent = "Вы вошли как администратор";
  loadAdminData();
}

async function loadAdminData() {
  await loadAdminUsers();
  await loadAdminTasks();
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
    container.innerHTML = "<p>Пользователей пока нет.</p>";
    return;
  }

  for (const user of users) {
    const div = document.createElement("div");
    div.className = "item";
    div.innerHTML = `
      <div class="item-title">${escapeHtml(user.email)}</div>
      <div class="item-meta"><b>ID:</b> ${user.id}</div>
      <div class="item-meta"><b>Роль:</b> ${escapeHtml(user.role)}</div>
      <div class="item-meta"><b>Заблокирован:</b> ${user.isBlocked ? "Да" : "Нет"}</div>
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
    container.innerHTML = "<p>Задач пока нет.</p>";
    return;
  }

  for (const task of tasks) {
    const div = document.createElement("div");
    div.className = "item";

    div.innerHTML = `
      <div class="item-title">${escapeHtml(task.title)}</div>
      <div class="item-meta"><b>Пользователь:</b> ${escapeHtml(task.userEmail ?? "")}</div>
      <div class="item-meta"><b>Описание:</b> ${escapeHtml(task.description ?? "")}</div>
      <div class="item-meta"><b>Дедлайн:</b> ${formatDate(task.deadlineAt)}</div>
      <div class="item-meta"><b>Статус:</b> ${escapeHtml(task.status)}</div>
      <div class="item-meta"><b>Приоритет:</b> ${escapeHtml(task.priority)}</div>

      <div class="actions">
        <button onclick="toggleAdminEdit('${task.id}')">Редактировать как админ</button>
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