/* ============================================================
   Deadline Service — app.js
   ============================================================ */

const apiBase = window.location.origin;
let token = localStorage.getItem("token") || "";

/* ── Toast ───────────────────────────────────────────────── */
function showToast(message, type = "info") {
  const container = document.getElementById("toast-container");
  if (!container) return;
  const icons = { success: "ph-check-circle", error: "ph-x-circle", info: "ph-info" };
  const toast = document.createElement("div");
  toast.className = `toast toast--${type}`;
  toast.innerHTML = `<i class="ph ${icons[type] || icons.info} toast-icon"></i>
                     <span>${escapeHtml(message)}</span>`;
  container.appendChild(toast);
  setTimeout(() => {
    toast.style.transition = "opacity .3s, transform .3s";
    toast.style.opacity = "0";
    toast.style.transform = "translateX(40px)";
    setTimeout(() => toast.remove(), 320);
  }, 3200);
}

/* ── Auth ────────────────────────────────────────────────── */
async function register() {
  const email    = document.getElementById("registerEmail")?.value;
  const password = document.getElementById("registerPassword")?.value;
  const response = await fetch(`${apiBase}/api/Auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  });
  const data = await response.json().catch(() => ({}));
  if (response.ok) {
    showToast("Аккаунт создан — войдите", "success");
    setTimeout(() => { window.location.href = "/login.html"; }, 1000);
  } else {
    showToast(typeof data === "string" ? data : "Ошибка регистрации", "error");
  }
}

async function login() {
  const email    = document.getElementById("loginEmail")?.value;
  const password = document.getElementById("loginPassword")?.value;
  const response = await fetch(`${apiBase}/api/Auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  });
  const data = await response.json().catch(() => ({}));
  if (!response.ok) { showToast("Неверный email или пароль", "error"); return; }
  token = data.token;
  localStorage.setItem("token", token);
  window.location.href = "/tasks.html";
}

function logout() {
  localStorage.removeItem("token");
  token = "";
  window.location.href = "/login.html";
}

/* ── Tasks Page ──────────────────────────────────────────── */
function initTasksPage() {
  token = localStorage.getItem("token") || "";
  if (!token) { window.location.href = "/login.html"; return; }
  updateStatus();
  updateAdminButton();
  loadTasks();
}

function updateStatus() {
  const el = document.getElementById("status");
  if (el) el.textContent = token ? "В сети" : "Гость";
}

function updateAdminButton() {
  const btn = document.getElementById("adminPanelBtn");
  if (btn) btn.classList.toggle("hidden", getUserRoleFromToken() !== "Admin");
}

/* ── Deadline Semantics ──────────────────────────────────── */
function getDeadlineStrip(deadlineAt, status) {
  if (status === "Done") return "strip-success";
  const diff = new Date(deadlineAt) - new Date();
  if (diff < 0)              return "strip-danger";
  if (diff < 3 * 3600000)    return "strip-alert";
  if (diff < 2 * 86400000)   return "strip-warning";
  return "strip-info";
}
function isOverdue(deadlineAt, status) {
  return status !== "Done" && new Date(deadlineAt) < new Date();
}

/* ── Skeleton / Empty ────────────────────────────────────── */
function showSkeletons(containerId, count = 3) {
  const el = document.getElementById(containerId);
  if (el) el.innerHTML = Array(count)
    .fill('<div class="skeleton skeleton-card"></div>').join("");
}
function emptyState(message, icon = "ph-clipboard-text") {
  return `<div class="empty-state">
    <i class="ph ${icon} empty-state-icon"></i>
    <p>${message}</p>
  </div>`;
}

/* ── Badges ──────────────────────────────────────────────── */
function statusBadge(status) {
  const map = {
    done:       ["badge-done",       "ph-check-circle", "Готово"],
    inprogress: ["badge-inprogress", "ph-spinner",      "В работе"],
    pending:    ["badge-pending",    "ph-clock",        "Ожидает"],
  };
  const [cls, icon, label] = map[String(status||"").toLowerCase().replace(/\s+/g,"")] || ["badge-pending","ph-clock",escapeHtml(status)];
  return `<span class="badge ${cls}"><i class="ph ${icon}"></i>${label}</span>`;
}
function deliveryBadge(status) {
  const map = {
    sent:    ["badge-sent",           "ph-check",     "Отправлено"],
    failed:  ["badge-failed",         "ph-x",         "Ошибка"],
    pending: ["badge-pending-status", "ph-hourglass", "Ожидает"],
  };
  const [cls, icon, label] = map[String(status||"").toLowerCase()] || ["badge-pending-status","ph-hourglass",escapeHtml(status)];
  return `<span class="badge ${cls}"><i class="ph ${icon}"></i>${label}</span>`;
}
function channelBadge(channel) {
  const isEmail = String(channel||"").toLowerCase().includes("email");
  return isEmail
    ? `<span class="badge badge-email"><i class="ph ph-envelope"></i>Email</span>`
    : `<span class="badge badge-in_app"><i class="ph ph-bell"></i>In-App</span>`;
}
function priorityBadge(priority) {
  const map = {
    high:   ["badge-high",   "ph-arrow-up",   "Высокий"],
    medium: ["badge-medium", "ph-minus",      "Средний"],
    low:    ["badge-low",    "ph-arrow-down", "Низкий"],
  };
  const [cls, icon, label] = map[String(priority||"").toLowerCase()] || ["badge-medium","ph-minus",escapeHtml(priority)];
  return `<span class="badge ${cls}"><i class="ph ${icon}"></i>${label}</span>`;
}

/* ── Task Filters ────────────────────────────────────────── */
function buildTaskFilterQuery() {
  const params  = new URLSearchParams();
  const status  = document.getElementById("filterStatus")?.value;
  const from    = document.getElementById("filterDeadlineFrom")?.value;
  const to      = document.getElementById("filterDeadlineTo")?.value;
  const overdue = document.getElementById("filterOverdue")?.checked;
  if (status)  params.set("status",       status);
  if (from)    params.set("deadlineFrom", new Date(from).toISOString());
  if (to)      params.set("deadlineTo",   new Date(to).toISOString());
  if (overdue) params.set("overdueOnly",  "true");
  const qs = params.toString();
  return qs ? `?${qs}` : "";
}
function resetTaskFilters() {
  ["filterStatus","filterDeadlineFrom","filterDeadlineTo"].forEach(id => {
    const el = document.getElementById(id); if (el) el.value = "";
  });
  const ov = document.getElementById("filterOverdue");
  if (ov) ov.checked = false;
  document.querySelectorAll(".filter-pill").forEach((p,i) => p.classList.toggle("active", i===0));
  loadTasks();
}

/* ── Load Tasks ──────────────────────────────────────────── */
async function loadTasks() {
  if (!token) { showToast("Сначала войдите", "error"); return; }
  const container = document.getElementById("tasks");
  if (!container) return;
  showSkeletons("tasks", 3);

  const response = await fetch(`${apiBase}/api/Tasks/my${buildTaskFilterQuery()}`, {
    headers: { "Authorization": `Bearer ${token}` }
  });
  const tasks = await response.json().catch(() => []);
  container.innerHTML = "";

  if (!Array.isArray(tasks) || tasks.length === 0) {
    container.innerHTML = emptyState("Задач пока нет — создайте первую", "ph-clipboard-text");
    return;
  }

  const sorted = [...tasks].sort((a,b) => {
    const aDone = a.status === "Done", bDone = b.status === "Done";
    if (aDone !== bDone) return aDone ? 1 : -1;
    return new Date(a.deadlineAt) - new Date(b.deadlineAt);
  });

  for (const task of sorted) {
    const strip   = getDeadlineStrip(task.deadlineAt, task.status);
    const overdue = isOverdue(task.deadlineAt, task.status);
    const done    = task.status === "Done";
    const div     = document.createElement("div");
    div.className = `item-card ${strip}${overdue?" overdue":""}${done?" completed":""}`;
    div.innerHTML = `
      <div class="item-card-header">
        <div class="item-card-title">${escapeHtml(task.title)}</div>
        <div class="item-card-actions">
          ${!done ? `<button class="btn-icon btn-icon--success" title="Завершить"
            onclick="markTaskDone('${task.id}')"><i class="ph ph-check"></i></button>` : ""}
          <button class="btn-icon" title="Редактировать"
            onclick="openEdit('${task.id}')"><i class="ph ph-pencil-simple"></i></button>
          <button class="btn-icon btn-icon--danger" title="Удалить"
            onclick="deleteTask('${task.id}')"><i class="ph ph-trash"></i></button>
        </div>
      </div>
      <div class="item-meta">
        <i class="ph ph-calendar-blank"></i>
        <span class="deadline-time">${formatDate(task.deadlineAt)}</span>
        ${statusBadge(task.status)} ${priorityBadge(task.priority)}
      </div>
      ${task.description ? `<div class="item-meta" style="margin-top:4px">
        <i class="ph ph-text-align-left"></i>${escapeHtml(task.description)}</div>` : ""}
      <div id="edit-${task.id}" class="edit-panel hidden">
        <h3><i class="ph ph-pencil-simple"></i> Редактирование</h3>
        <label>Название</label>
        <input id="title-${task.id}" value="${escapeHtml(task.title)}" />
        <label>Описание</label>
        <textarea id="desc-${task.id}">${escapeHtml(task.description ?? "")}</textarea>
        <div class="form-row">
          <div>
            <label>Дедлайн</label>
            <input id="deadline-${task.id}" value="${toLocalInputValue(task.deadlineAt)}" type="datetime-local" />
          </div>
          <div>
            <label>Статус</label>
            <select id="status-${task.id}">
              <option value="Pending"    ${task.status==="Pending"    ?"selected":""}>Ожидает</option>
              <option value="InProgress" ${task.status==="InProgress" ?"selected":""}>В работе</option>
              <option value="Done"       ${task.status==="Done"       ?"selected":""}>Готово</option>
            </select>
          </div>
        </div>
        <label>Приоритет</label>
        <select id="priority-${task.id}">
          <option value="High"   ${task.priority==="High"   ?"selected":""}>🔴 Высокий</option>
          <option value="Medium" ${task.priority==="Medium" ?"selected":""}>🟡 Средний</option>
          <option value="Low"    ${task.priority==="Low"    ?"selected":""}>🟢 Низкий</option>
        </select>

        <div class="reminder-box" style="margin-top:4px">
          <h3 style="margin-bottom:10px"><i class="ph ph-bell-ringing"></i> Напоминание</h3>
          <div class="form-row" style="margin-bottom:8px">
            <div>
              <label>За сколько</label>
              <input id="remind-val-${task.id}" type="number" min="1" value="1" style="margin-bottom:0" />
            </div>
            <div>
              <label>Единица</label>
              <select id="remind-unit-${task.id}" style="margin-bottom:0">
                <option value="Minutes">Минут</option>
                <option value="Hours" selected>Часов</option>
                <option value="Days">Дней</option>
              </select>
            </div>
          </div>
          <div class="checkbox-row" style="margin-bottom:0">
            <label><input id="remind-email-${task.id}" type="checkbox" checked />
              <i class="ph ph-envelope"></i> Email</label>
            <label><input id="remind-inapp-${task.id}" type="checkbox" checked />
              <i class="ph ph-bell"></i> В приложении</label>
          </div>
          <p id="remind-loading-${task.id}" style="font-size:11px;color:var(--muted);margin-top:6px">
            Загрузка текущих настроек…
          </p>
        </div>

        <div class="btn-group" style="margin-top:12px">
          <button onclick="updateTask('${task.id}')">
            <i class="ph ph-floppy-disk"></i> Сохранить
          </button>
          <button class="btn-ghost" onclick="toggleEdit('${task.id}')">
            <i class="ph ph-x"></i> Отмена
          </button>
        </div>
      </div>`;
    container.appendChild(div);
  }
}

function toggleEdit(taskId) {
  const p = document.getElementById(`edit-${taskId}`);
  if (p) p.classList.toggle("hidden");
}

async function openEdit(taskId) {
  toggleEdit(taskId);
  const panel = document.getElementById(`edit-${taskId}`);
  if (!panel || panel.classList.contains("hidden")) return;

  // Load current notification settings
  const loadingEl = document.getElementById(`remind-loading-${taskId}`);
  try {
    const resp = await fetch(`${apiBase}/api/Tasks/${taskId}/notification-settings`, {
      headers: { "Authorization": `Bearer ${token}` }
    });
    if (resp.ok) {
      const settings = await resp.json();
      // settings may be array or single object
      const s = Array.isArray(settings) ? settings[0] : settings;
      if (s) {
        const valEl  = document.getElementById(`remind-val-${taskId}`);
        const unitEl = document.getElementById(`remind-unit-${taskId}`);
        if (valEl)  valEl.value  = s.remindBeforeValue ?? 1;
        if (unitEl) unitEl.value = s.remindBeforeUnit  ?? "Hours";
      }
    }
    if (loadingEl) loadingEl.remove();
  } catch {
    if (loadingEl) loadingEl.textContent = "Не удалось загрузить настройки";
  }
}

/* ── CRUD ────────────────────────────────────────────────── */
async function createTask() {
  if (!token) { showToast("Сначала войдите", "error"); return; }
  const title       = document.getElementById("taskTitle").value;
  const description = document.getElementById("taskDescription").value;
  const deadlineAt  = document.getElementById("taskDeadline").value;
  const priority    = document.getElementById("taskPriority").value;
  const remindValue = parseInt(document.getElementById("remindValue")?.value || "1", 10);
  const remindUnit  = document.getElementById("remindUnit")?.value || "Hours";
  const notifyByEmail = document.getElementById("notifyEmail")?.checked ?? true;
  const notifyInApp   = document.getElementById("notifyInApp")?.checked ?? true;

  if (!title.trim()) { showToast("Введите название задачи", "error"); return; }
  if (!deadlineAt)   { showToast("Укажите дедлайн", "error"); return; }

  const response = await fetch(`${apiBase}/api/Tasks`, {
    method: "POST",
    headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
    body: JSON.stringify({
      title, description,
      deadlineAt: new Date(deadlineAt).toISOString(),
      priority,
      reminder: { remindBeforeValue: remindValue, remindBeforeUnit: remindUnit, notifyByEmail, notifyInApp }
    })
  });
  if (!response.ok) { showToast("Ошибка создания задачи", "error"); return; }

  document.getElementById("taskTitle").value       = "";
  document.getElementById("taskDescription").value = "";
  document.getElementById("taskDeadline").value    = "";
  document.getElementById("taskPriority").value    = "Medium";
  showToast("Задача создана", "success");
  loadTasks();
}

async function markTaskDone(taskId) {
  const allResp = await fetch(`${apiBase}/api/Tasks/my`, {
    headers: { "Authorization": `Bearer ${token}` }
  });
  const tasks = await allResp.json().catch(() => []);
  const task  = tasks.find(t => t.id === taskId);
  if (!task) { showToast("Задача не найдена", "error"); return; }

  const response = await fetch(`${apiBase}/api/Tasks/${taskId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
    body: JSON.stringify({
      title: task.title, description: task.description,
      deadlineAt: task.deadlineAt, status: "Done", priority: task.priority
    })
  });
  if (!response.ok) { showToast("Ошибка обновления задачи", "error"); return; }
  showToast("Задача завершена ✓", "success");
  loadTasks();
}

async function updateTask(taskId) {
  const title       = document.getElementById(`title-${taskId}`).value;
  const description = document.getElementById(`desc-${taskId}`).value;
  const deadlineAt  = document.getElementById(`deadline-${taskId}`).value;
  const status      = document.getElementById(`status-${taskId}`).value;
  const priority    = document.getElementById(`priority-${taskId}`).value;

  // Save task
  const response = await fetch(`${apiBase}/api/Tasks/${taskId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
    body: JSON.stringify({ title, description, deadlineAt: new Date(deadlineAt).toISOString(), status, priority })
  });
  if (!response.ok) { showToast("Ошибка обновления задачи", "error"); return; }

  // Save notification settings
  const remindVal   = parseInt(document.getElementById(`remind-val-${taskId}`)?.value  || "1", 10);
  const remindUnit  = document.getElementById(`remind-unit-${taskId}`)?.value  || "Hours";
  const notifyEmail = document.getElementById(`remind-email-${taskId}`)?.checked ?? true;
  const notifyInApp = document.getElementById(`remind-inapp-${taskId}`)?.checked ?? true;

  if (remindVal > 0) {
    await fetch(`${apiBase}/api/Tasks/${taskId}/notification-settings`, {
      method: "PUT",
      headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
      body: JSON.stringify({
        remindBeforeValue: remindVal,
        remindBeforeUnit:  remindUnit,
        notifyByEmail:     notifyEmail,
        notifyInApp:       notifyInApp
      })
    });
  }

  showToast("Задача обновлена", "success");
  loadTasks();
}

async function deleteTask(taskId) {
  if (!confirm("Удалить задачу? Это действие нельзя отменить.")) return;
  const response = await fetch(`${apiBase}/api/Tasks/${taskId}`, {
    method: "DELETE",
    headers: { "Authorization": `Bearer ${token}` }
  });
  if (!response.ok) { showToast("Ошибка удаления задачи", "error"); return; }
  showToast("Задача удалена", "info");
  loadTasks();
}

/* ── Notifications Page ──────────────────────────────────── */

// Persist read notification IDs in localStorage
function getReadNotifIds() {
  try { return new Set(JSON.parse(localStorage.getItem("readNotifIds") || "[]")); }
  catch { return new Set(); }
}
function markNotifRead(id) {
  const ids = getReadNotifIds();
  ids.add(id);
  localStorage.setItem("readNotifIds", JSON.stringify([...ids]));
}
function markAllNotifsRead(ids) {
  const existing = getReadNotifIds();
  ids.forEach(id => existing.add(id));
  localStorage.setItem("readNotifIds", JSON.stringify([...existing]));
}

function initNotificationsPage() {
  token = localStorage.getItem("token") || "";
  if (!token) { window.location.href = "/login.html"; return; }
  loadNotifications();
}

async function loadNotifications() {
  const container = document.getElementById("notificationsList");
  if (!container) return;
  showSkeletons("notificationsList", 4);

  const response = await fetch(`${apiBase}/api/Notifications/my`, {
    headers: { "Authorization": `Bearer ${token}` }
  });
  const items = await response.json().catch(() => []);
  container.innerHTML = "";

  if (!Array.isArray(items) || items.length === 0) {
    container.innerHTML = emptyState("Пока пусто — создайте задачу с напоминанием", "ph-bell-slash");
    return;
  }

  const readIds = getReadNotifIds();

  // Group by day
  const groups = {};
  for (const n of items) {
    const d   = new Date(n.createdAt);
    const now = new Date();
    const yesterday = new Date(now); yesterday.setDate(now.getDate() - 1);
    const label = d.toDateString() === now.toDateString() ? "Сегодня"
      : d.toDateString() === yesterday.toDateString() ? "Вчера"
      : d.toLocaleDateString("ru-RU", { day: "numeric", month: "long" });
    if (!groups[label]) groups[label] = [];
    groups[label].push(n);
  }

  // Expose all IDs for "mark all" button
  container._allIds = items.map(n => n.id);

  for (const [label, notifs] of Object.entries(groups)) {
    const g = document.createElement("div");
    g.innerHTML = `<div class="notif-group-label">${label}</div>`;
    container.appendChild(g);

    for (const n of notifs) {
      const isRead   = readIds.has(n.id);
      const isEmail  = n.channel?.toLowerCase().includes("email");
      const isFailed = n.deliveryStatus?.toLowerCase() === "failed";
      const iconCls  = isFailed ? "notif-icon--danger" : isEmail ? "notif-icon--clock" : "notif-icon--info";
      const iconPh   = isFailed ? "ph-x-circle" : isEmail ? "ph-envelope" : "ph-bell";

      const div = document.createElement("div");
      div.className = `notif-item${isRead ? "" : " unread"}`;
      div.dataset.notifId = n.id;
      div.innerHTML = `
        <div class="notif-icon ${iconCls}"><i class="ph ${iconPh}"></i></div>
        <div class="notif-body">
          <div class="notif-text">${escapeHtml(n.message)}</div>
          <div class="notif-time">
            ${channelBadge(n.channel)} ${deliveryBadge(n.deliveryStatus)}
            · ${formatDate(n.createdAt)}
            ${n.sentAt ? `· Отправлено: ${formatDate(n.sentAt)}` : ""}
          </div>
        </div>
        ${isRead ? "" : '<div class="notif-dot"></div>'}`;

      div.addEventListener("click", () => {
        div.classList.remove("unread");
        div.querySelector(".notif-dot")?.remove();
        markNotifRead(n.id);
      });
      container.appendChild(div);
    }
  }
}

/* ── Admin Page ──────────────────────────────────────────── */
function initAdminPage() {
  token = localStorage.getItem("token") || "";
  if (!token) { window.location.href = "/login.html"; return; }
  const role   = getUserRoleFromToken();
  const status = document.getElementById("adminStatus");
  if (role !== "Admin") {
    if (status) { status.textContent = "Нет доступа: требуется роль Admin"; status.style.color = "var(--danger)"; }
    return;
  }
  if (status) status.innerHTML = '<span class="status-pill">Администратор</span>';
  loadAdminData();
}

async function loadAdminData() {
  await Promise.all([loadAdminUsers(), loadAdminTasks(), loadAdminNotifications()]);
}

async function loadAdminUsers() {
  const container = document.getElementById("adminUsers");
  if (!container) return;
  showSkeletons("adminUsers", 3);
  const response = await fetch(`${apiBase}/api/Admin/users`, { headers: { "Authorization": `Bearer ${token}` } });
  const users = await response.json().catch(() => []);
  container.innerHTML = "";
  if (!Array.isArray(users) || users.length === 0) {
    container.innerHTML = emptyState("Пользователей нет", "ph-users"); return;
  }
  for (const user of users) {
    const div = document.createElement("div");
    div.className = `user-row${user.isBlocked ? " blocked" : ""}`;
    div.innerHTML = `
      <div class="user-info">
        <div class="user-email"><i class="ph ph-user-circle"></i> ${escapeHtml(user.email)}</div>
        <div class="user-meta">Роль: <b>${escapeHtml(user.role)}</b> ·
          ${user.isBlocked
            ? '<span class="badge badge-failed"><i class="ph ph-lock"></i>Заблокирован</span>'
            : '<span class="badge badge-done"><i class="ph ph-check"></i>Активен</span>'}
        </div>
      </div>
      <div>
        ${user.isBlocked
          ? `<button class="btn-secondary" style="height:34px;font-size:12px"
               onclick="setUserBlocked('${user.id}',false)">
               <i class="ph ph-lock-open"></i> Разблокировать</button>`
          : `<button class="btn-danger" style="height:34px;font-size:12px"
               onclick="setUserBlocked('${user.id}',true)">
               <i class="ph ph-lock"></i> Заблокировать</button>`}
      </div>`;
    container.appendChild(div);
  }
}

async function loadAdminTasks() {
  const container = document.getElementById("adminTasks");
  if (!container) return;
  showSkeletons("adminTasks", 3);
  const response = await fetch(`${apiBase}/api/Admin/tasks`, { headers: { "Authorization": `Bearer ${token}` } });
  const tasks = await response.json().catch(() => []);
  container.innerHTML = "";
  if (!Array.isArray(tasks) || tasks.length === 0) {
    container.innerHTML = emptyState("Задач нет", "ph-list-checks"); return;
  }
  for (const task of tasks) {
    const strip = getDeadlineStrip(task.deadlineAt, task.status);
    const div   = document.createElement("div");
    div.className = `item-card ${strip}`;
    div.innerHTML = `
      <div class="item-card-header">
        <div class="item-card-title">${escapeHtml(task.title)}</div>
        <div class="item-card-actions">
          <button class="btn-icon" onclick="toggleAdminEdit('${task.id}')">
            <i class="ph ph-pencil-simple"></i></button>
        </div>
      </div>
      <div class="item-meta">
        <i class="ph ph-user"></i> ${escapeHtml(task.userEmail ?? "")} ·
        <i class="ph ph-calendar-blank"></i>
        <span class="deadline-time">${formatDate(task.deadlineAt)}</span>
        ${statusBadge(task.status)} ${priorityBadge(task.priority)}
      </div>
      <div id="admin-edit-${task.id}" class="edit-panel hidden">
        <h3><i class="ph ph-pencil-simple"></i> Редактирование</h3>
        <label>Название</label>
        <input id="admin-title-${task.id}" value="${escapeHtml(task.title)}" />
        <label>Описание</label>
        <textarea id="admin-desc-${task.id}">${escapeHtml(task.description ?? "")}</textarea>
        <div class="form-row">
          <div><label>Дедлайн</label>
            <input id="admin-deadline-${task.id}" value="${toLocalInputValue(task.deadlineAt)}" type="datetime-local" /></div>
          <div><label>Статус</label>
            <select id="admin-status-${task.id}">
              <option value="Pending"    ${task.status==="Pending"    ?"selected":""}>Ожидает</option>
              <option value="InProgress" ${task.status==="InProgress" ?"selected":""}>В работе</option>
              <option value="Done"       ${task.status==="Done"       ?"selected":""}>Готово</option>
            </select></div>
        </div>
        <label>Приоритет</label>
        <select id="admin-priority-${task.id}">
          <option value="High"   ${task.priority==="High"   ?"selected":""}>🔴 Высокий</option>
          <option value="Medium" ${task.priority==="Medium" ?"selected":""}>🟡 Средний</option>
          <option value="Low"    ${task.priority==="Low"    ?"selected":""}>🟢 Низкий</option>
        </select>
        <div class="btn-group" style="margin-top:4px">
          <button onclick="adminUpdateTask('${task.id}')">
            <i class="ph ph-floppy-disk"></i> Сохранить</button>
          <button class="btn-ghost" onclick="toggleAdminEdit('${task.id}')">
            <i class="ph ph-x"></i> Отмена</button>
        </div>
      </div>`;
    container.appendChild(div);
  }
}

async function loadAdminNotifications() {
  const container = document.getElementById("adminNotifications");
  if (!container) return;
  showSkeletons("adminNotifications", 3);
  const response = await fetch(`${apiBase}/api/Notifications/admin/all`, { headers: { "Authorization": `Bearer ${token}` } });
  const items = await response.json().catch(() => []);
  container.innerHTML = "";
  if (!Array.isArray(items) || items.length === 0) {
    container.innerHTML = emptyState("Уведомлений пока нет", "ph-bell-slash"); return;
  }
  for (const n of items) {
    const div = document.createElement("div");
    div.className = "notif-item";
    div.innerHTML = `
      <div class="notif-icon notif-icon--info"><i class="ph ph-bell"></i></div>
      <div class="notif-body">
        <div class="notif-text">
          <i class="ph ph-user"></i> ${escapeHtml(n.userEmail ?? "")} — ${escapeHtml(n.message)}
        </div>
        <div class="notif-time">
          ${channelBadge(n.channel)} ${deliveryBadge(n.deliveryStatus)} · ${formatDate(n.createdAt)}
        </div>
      </div>`;
    container.appendChild(div);
  }
}

function toggleAdminEdit(taskId) {
  const p = document.getElementById(`admin-edit-${taskId}`);
  if (p) p.classList.toggle("hidden");
}

async function adminUpdateTask(taskId) {
  const title       = document.getElementById(`admin-title-${taskId}`).value;
  const description = document.getElementById(`admin-desc-${taskId}`).value;
  const deadlineAt  = document.getElementById(`admin-deadline-${taskId}`).value;
  const status      = document.getElementById(`admin-status-${taskId}`).value;
  const priority    = document.getElementById(`admin-priority-${taskId}`).value;
  const response = await fetch(`${apiBase}/api/Admin/tasks/${taskId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
    body: JSON.stringify({ title, description, deadlineAt: new Date(deadlineAt).toISOString(), status, priority })
  });
  if (!response.ok) { showToast("Ошибка обновления задачи", "error"); return; }
  showToast("Задача обновлена", "success");
  loadAdminTasks();
}

async function setUserBlocked(userId, isBlocked) {
  const response = await fetch(`${apiBase}/api/Admin/users/${userId}/block`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
    body: JSON.stringify({ isBlocked })
  });
  if (!response.ok) { showToast("Не удалось изменить статус пользователя", "error"); return; }
  showToast(isBlocked ? "Пользователь заблокирован" : "Пользователь разблокирован",
            isBlocked ? "info" : "success");
  loadAdminUsers();
}

/* ── Utilities ───────────────────────────────────────────── */
function toLocalInputValue(isoString) {
  const date   = new Date(isoString);
  const offset = date.getTimezoneOffset();
  return new Date(date.getTime() - offset * 60000).toISOString().slice(0, 16);
}
function formatDate(isoString) {
  return new Date(isoString).toLocaleString("ru-RU", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit"
  });
}
function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;").replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;").replaceAll('"', "&quot;");
}
function getUserRoleFromToken() {
  const t = localStorage.getItem("token") || "";
  if (!t) return null;
  try {
    const payload = JSON.parse(atob(t.split(".")[1]));
    return payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ?? null;
  } catch { return null; }
}
