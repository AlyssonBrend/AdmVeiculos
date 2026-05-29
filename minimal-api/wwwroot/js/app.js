// ── Estado global ─────────────────────────────────────────────────────────────
const state = {
  lang: localStorage.getItem('lang') || 'pt',
  token: localStorage.getItem('token') || null,
  userName: localStorage.getItem('userName') || null,
};

const API = '';  // mesma origem

// ── i18n ──────────────────────────────────────────────────────────────────────
const t = key => i18n[state.lang]?.[key] ?? key;

function applyLang() {
  document.querySelectorAll('[data-i18n]').forEach(el => {
    const k = el.dataset.i18n;
    if (el.placeholder !== undefined && el.tagName === 'INPUT') el.placeholder = t(k);
    else el.textContent = t(k);
  });
  document.querySelectorAll('[data-i18n-ph]').forEach(el => el.placeholder = t(el.dataset.i18nPh));
  document.title = t('appTitle');
}

document.getElementById('langSel').addEventListener('change', e => {
  state.lang = e.target.value;
  localStorage.setItem('lang', state.lang);
  applyLang();
});

// ── Navegação ─────────────────────────────────────────────────────────────────
function showPage(id) {
  document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
  document.getElementById('page-' + id)?.classList.add('active');
  document.querySelectorAll('.nav-btn').forEach(b => b.classList.toggle('active', b.dataset.page === id));
  if (id === 'history') loadHistory(true);
  if (id === 'dashboard') loadDashboard();
}

document.querySelectorAll('.nav-btn[data-page]').forEach(btn =>
  btn.addEventListener('click', () => showPage(btn.dataset.page)));

// ── Auth helpers ──────────────────────────────────────────────────────────────
function authHeaders() {
  const h = { 'Content-Type': 'application/json' };
  if (state.token) h['Authorization'] = 'Bearer ' + state.token;
  return h;
}

function updateAuthUI() {
  const badge = document.getElementById('userBadge');
  const loginBtn = document.getElementById('navLoginBtn');
  if (state.token) {
    badge.textContent = (state.userName || 'U')[0].toUpperCase();
    badge.classList.remove('hidden');
    loginBtn.textContent = t('logoutBtn');
    loginBtn.onclick = logout;
  } else {
    badge.classList.add('hidden');
    loginBtn.textContent = t('navLogin');
    loginBtn.onclick = () => showPage('login');
  }
}

function logout() {
  state.token = null; state.userName = null;
  localStorage.removeItem('token'); localStorage.removeItem('userName');
  updateAuthUI();
  showPage('login');
}

// ── Login ─────────────────────────────────────────────────────────────────────
document.getElementById('loginForm').addEventListener('submit', async e => {
  e.preventDefault();
  const email = document.getElementById('loginEmail').value;
  const pass  = document.getElementById('loginPass').value;

  try {
    const res = await fetch(`${API}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password: pass })
    });
    if (!res.ok) { showAlert('loginAlert', t('loginError'), 'error'); return; }

    const data = await res.json();
    const payload = JSON.parse(atob(data.token.split('.')[1]));
    state.token = data.token;
    state.userName = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || email;
    localStorage.setItem('token', state.token);
    localStorage.setItem('userName', state.userName);
    updateAuthUI();
    showPage('dashboard');
  } catch { showAlert('loginAlert', t('loginError'), 'error'); }
});

// ── Dashboard ─────────────────────────────────────────────────────────────────
async function loadDashboard() {
  try {
    const [active, vehicles] = await Promise.all([
      fetch(`${API}/api/parking/active`, { headers: authHeaders() }).then(r => r.ok ? r.json() : []),
      fetch(`${API}/api/vehicles`, { headers: authHeaders() }).then(r => r.ok ? r.json() : []),
    ]);
    document.getElementById('statActive').textContent = active.length;
    document.getElementById('statVehicles').textContent = vehicles.length;

    const today = active.filter(r => new Date(r.entryTime).toDateString() === new Date().toDateString());
    document.getElementById('statToday').textContent = today.length;

    // Tabela de ativos
    const tbody = document.getElementById('activeTableBody');
    tbody.innerHTML = '';
    if (!active.length) {
      tbody.innerHTML = `<tr><td colspan="5" class="no-records">${t('noRecords')}</td></tr>`;
      return;
    }
    for (const r of active) {
      const elapsed = msToHMS(Date.now() - new Date(r.entryTime).getTime());
      tbody.innerHTML += `<tr>
        <td><span class="plate-display">${r.plate}</span></td>
        <td>${r.vehicle?.model || '-'}</td>
        <td>${r.vehicle?.color || '-'}</td>
        <td>${fmtDate(r.entryTime)}</td>
        <td>${elapsed}</td>
      </tr>`;
    }
  } catch(e) { console.error(e); }
}

// ── Veículos ──────────────────────────────────────────────────────────────────
document.getElementById('vehicleForm').addEventListener('submit', async e => {
  e.preventDefault();
  const body = {
    plate: document.getElementById('vPlate').value,
    model: document.getElementById('vModel').value,
    color: document.getElementById('vColor').value,
    ownerName: document.getElementById('vOwner').value,
    ownerPhone: document.getElementById('vPhone').value,
  };
  try {
    const res = await fetch(`${API}/api/vehicles`, {
      method: 'POST', headers: authHeaders(), body: JSON.stringify(body)
    });
    const data = await res.json();
    if (res.ok) {
      showAlert('vehicleAlert', `✓ ${data.plate}`, 'success');
      e.target.reset();
    } else {
      showAlert('vehicleAlert', data.error || 'Error', 'error');
    }
  } catch { showAlert('vehicleAlert', 'Error', 'error'); }
});

document.getElementById('btnSearchVehicle').addEventListener('click', async () => {
  const plate = document.getElementById('searchPlate').value.trim().toUpperCase().replace('-','');
  if (!plate) return;
  try {
    const res = await fetch(`${API}/api/vehicles/${plate}`, { headers: authHeaders() });
    const box = document.getElementById('vehicleResult');
    if (!res.ok) { box.innerHTML = `<div class="alert alert-error">${t('noRecords')}</div>`; return; }
    const v = await res.json();
    box.innerHTML = `<div class="alert alert-info">
      <strong><span class="plate-display">${v.plate}</span></strong>
      &nbsp; ${v.model} · ${v.color} · ${v.ownerName || '-'}
    </div>`;
  } catch {}
});

// ── Entrada / Saída ───────────────────────────────────────────────────────────
document.getElementById('btnEntry').addEventListener('click', async () => {
  const plate = document.getElementById('parkingPlate').value.trim();
  if (!plate) return;
  try {
    const res = await fetch(`${API}/api/parking/entry`, {
      method: 'POST', headers: authHeaders(),
      body: JSON.stringify({ plate })
    });
    const data = await res.json();
    if (res.ok) showAlert('parkingAlert', `✓ Entrada registrada — ${plate.toUpperCase()}`, 'success');
    else showAlert('parkingAlert', data.error || 'Error', 'error');
  } catch { showAlert('parkingAlert', 'Error', 'error'); }
});

document.getElementById('btnCheck').addEventListener('click', async () => {
  const plate = document.getElementById('parkingPlate').value.trim();
  if (!plate) return;
  try {
    const res = await fetch(`${API}/api/parking/status/${plate}`, { headers: authHeaders() });
    const data = await res.json();
    const box = document.getElementById('statusBox');
    if (!res.ok) { box.innerHTML = `<div class="alert alert-error">${data.error}</div>`; return; }
    box.innerHTML = `<div class="status-box active">
      <div class="flex">
        <span class="plate-display">${data.plate}</span>
        <span class="badge badge-active">${t('statusActive')}</span>
      </div>
      <div class="amount mt-1">R$ ${Number(data.currentAmount).toFixed(2)}</div>
      <div class="duration text-muted">${t('elapsed')}: ${msToHMS(data.elapsedMinutes * 60000)}</div>
      ${data.vehicle ? `<div class="text-muted mt-1">${data.vehicle.model} · ${data.vehicle.color}</div>` : ''}
    </div>`;
  } catch {}
});

document.getElementById('btnExit').addEventListener('click', async () => {
  const plate = document.getElementById('parkingPlate').value.trim();
  if (!plate) return;
  try {
    const res = await fetch(`${API}/api/parking/exit/${plate}`, {
      method: 'POST', headers: authHeaders()
    });
    const data = await res.json();
    if (res.ok) {
      document.getElementById('statusBox').innerHTML = `<div class="status-box">
        <div class="flex"><span class="plate-display">${data.plate}</span><span class="badge badge-done">${t('statusDone')}</span></div>
        <div class="amount mt-1">R$ ${Number(data.totalAmount).toFixed(2)}</div>
        <div class="duration text-muted">${t('elapsed')}: ${data.duration}</div>
      </div>`;
    } else showAlert('parkingAlert', data.error || 'Error', 'error');
  } catch {}
});

// ── Histórico ─────────────────────────────────────────────────────────────────
let histPage = 1;
async function loadHistory(reset = true) {
  if (reset) { histPage = 1; document.getElementById('histTableBody').innerHTML = ''; }
  try {
    const res = await fetch(`${API}/api/parking/history?page=${histPage}&pageSize=20`, { headers: authHeaders() });
    if (!res.ok) return;
    const records = await res.json();
    const tbody = document.getElementById('histTableBody');
    if (!records.length && histPage === 1) {
      tbody.innerHTML = `<tr><td colspan="6" class="no-records">${t('noRecords')}</td></tr>`;
      return;
    }
    for (const r of records) {
      const duration = r.exitTime ? msToHMS(new Date(r.exitTime) - new Date(r.entryTime)) : '-';
      tbody.innerHTML += `<tr>
        <td><span class="plate-display">${r.plate}</span></td>
        <td>${r.vehicle?.model || '-'}</td>
        <td>${fmtDate(r.entryTime)}</td>
        <td>${r.exitTime ? fmtDate(r.exitTime) : '-'}</td>
        <td>${duration}</td>
        <td><strong>R$ ${Number(r.totalAmount || 0).toFixed(2)}</strong></td>
      </tr>`;
    }
    document.getElementById('loadMoreHist').classList.toggle('hidden', records.length < 20);
  } catch {}
}

document.getElementById('loadMoreHist').addEventListener('click', () => { histPage++; loadHistory(false); });

// ── Câmera / OCR ──────────────────────────────────────────────────────────────
document.getElementById('btnCaptureRtsp').addEventListener('click', async () => {
  const url = document.getElementById('rtspUrl').value.trim();
  if (!url) return;
  setOcrResult('⏳ Processando...');
  try {
    const res = await fetch(`${API}/api/camera/rtsp`, {
      method: 'POST', headers: authHeaders(),
      body: JSON.stringify({ rtspUrl: url })
    });
    const data = await res.json();
    if (res.ok && data.success) {
      setOcrResult(`✅ ${t('plateDetected')}: <strong><span class="plate-display">${data.plate}</span></strong>`);
      document.getElementById('parkingPlate').value = data.plate;
    } else {
      setOcrResult(`⚠️ ${t('noPlate')}${data.error ? ': ' + data.error : ''}`, 'error');
    }
  } catch { setOcrResult('❌ Error', 'error'); }
});

document.getElementById('btnUploadImg').addEventListener('click', () =>
  document.getElementById('fileInput').click());

document.getElementById('fileInput').addEventListener('change', async e => {
  const file = e.target.files[0];
  if (!file) return;

  // Preview
  const reader = new FileReader();
  reader.onload = ev => {
    const img = document.getElementById('imgPreview');
    img.src = ev.target.result;
    img.classList.remove('hidden');
  };
  reader.readAsDataURL(file);

  setOcrResult('⏳ Processando...');
  const form = new FormData();
  form.append('file', file);
  try {
    const res = await fetch(`${API}/api/camera/upload`, {
      method: 'POST',
      headers: state.token ? { 'Authorization': 'Bearer ' + state.token } : {},
      body: form
    });
    const data = await res.json();
    if (res.ok && data.success) {
      setOcrResult(`✅ ${t('plateDetected')}: <strong><span class="plate-display">${data.plate}</span></strong>`);
      document.getElementById('parkingPlate').value = data.plate;
    } else {
      setOcrResult(`⚠️ ${t('noPlate')}`, 'error');
    }
  } catch { setOcrResult('❌ Error', 'error'); }
});

function setOcrResult(html, type = 'info') {
  const el = document.getElementById('ocrResult');
  el.innerHTML = html;
  el.className = `alert alert-${type}`;
  el.classList.remove('hidden');
}

// ── Utilidades ────────────────────────────────────────────────────────────────
function msToHMS(ms) {
  const s = Math.floor(ms / 1000);
  const h = Math.floor(s / 3600).toString().padStart(2, '0');
  const m = Math.floor((s % 3600) / 60).toString().padStart(2, '0');
  const sc = (s % 60).toString().padStart(2, '0');
  return `${h}:${m}:${sc}`;
}

function fmtDate(iso) {
  return new Date(iso).toLocaleString();
}

function showAlert(id, msg, type = 'info') {
  const el = document.getElementById(id);
  if (!el) return;
  el.textContent = msg;
  el.className = `alert alert-${type}`;
  el.classList.remove('hidden');
  setTimeout(() => el.classList.add('hidden'), 4000);
}

// ── Init ──────────────────────────────────────────────────────────────────────
document.getElementById('langSel').value = state.lang;
applyLang();
updateAuthUI();
showPage(state.token ? 'dashboard' : 'login');
document.getElementById('yearSpan').textContent = new Date().getFullYear();

// Atualiza timer dos carros no dashboard a cada 30s
setInterval(() => {
  if (document.getElementById('page-dashboard').classList.contains('active')) loadDashboard();
}, 30000);
