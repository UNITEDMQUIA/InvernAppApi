'use strict';

/* InvernApp · Dashboard (HTML + CSS + JavaScript + Bootstrap + Chart.js) */

const API = '/api/v1';
const KEY_STORAGE = 'invernapp_api_key';
const ONLINE_THRESHOLD_SECONDS = 120;
const STATUS_REFRESH_MS = 5000;
const CHARTS_REFRESH_MS = 15000;
const HISTORY_LIMIT = 1000;

const EVENT_LABELS = {
    DeviceRegistered: 'Dispositivo registrado',
    WateringStarted: 'Riego iniciado',
    WateringStopped: 'Riego detenido',
    FanStarted: 'Ventilador activado',
    FanStopped: 'Ventilador desactivado',
    LightStarted: 'Iluminación activada',
    LightStopped: 'Iluminación desactivada',
    CommunicationLost: 'Comunicación perdida',
    CommunicationRestored: 'Comunicación restablecida',
    ConfigurationChanged: 'Configuración modificada',
    CommandRejected: 'Comando rechazado',
    Alert: 'Alerta',
    Error: 'Error'
};

const SEVERITY_CLASS = { Info: 'text-bg-info', Warning: 'text-bg-warning', Error: 'text-bg-danger' };
const SEVERITY_LABEL = { Info: 'Info', Warning: 'Aviso', Error: 'Error' };
const ACTUATOR_IDS = { Pump: 'actPump', Fan: 'actFan', Light: 'actLight' };

// Campos de la configuración: [id, etiqueta, mínimo, máximo, paso]
const CONFIG_GROUPS = [
    {
        title: 'Riego',
        enabled: 'wateringEnabled',
        enabledLabel: 'Riego automático habilitado',
        fields: [
            ['soilHumidityMinimum', 'Humedad mínima del suelo (%)', 0, 100, 0.1],
            ['soilHumidityTarget', 'Humedad objetivo del suelo (%)', 0, 100, 0.1],
            ['maximumWateringSeconds', 'Tiempo máximo de riego (s)', 1, 600, 1]
        ]
    },
    {
        title: 'Ventilación',
        enabled: 'ventilationEnabled',
        enabledLabel: 'Ventilación automática habilitada',
        fields: [
            ['maximumTemperature', 'Temperatura máxima (°C)', 0, 60, 0.1],
            ['temperatureHysteresis', 'Histéresis (°C)', 0.1, 20, 0.1],
            ['criticalTemperature', 'Temperatura crítica (°C)', 0, 80, 0.1]
        ]
    },
    {
        title: 'Iluminación',
        enabled: 'lightingEnabled',
        enabledLabel: 'Iluminación automática habilitada',
        fields: [
            ['minimumLuminosity', 'Luminosidad mínima (%)', 0, 100, 0.1],
            ['targetLuminosity', 'Luminosidad objetivo (%)', 0, 100, 0.1]
        ]
    }
];

// Gráficas: la función thresholds devuelve las líneas de umbral [nombre, valor, color]
const CHART_DEFS = {
    temp: {
        canvas: 'chartTemp', label: 'Temperatura (°C)', color: '#dc3545', percent: false,
        value: (m) => m.temperature,
        thresholds: (c) => [['Máx. ventilación', c.maximumTemperature, '#fd7e14'], ['Crítica', c.criticalTemperature, '#b02a37']]
    },
    air: {
        canvas: 'chartAir', label: 'Humedad del aire (%)', color: '#0d6efd', percent: true,
        value: (m) => m.airHumidity,
        thresholds: () => []
    },
    soil: {
        canvas: 'chartSoil', label: 'Humedad del suelo (%)', color: '#8d6e63', percent: true, soil: true,
        value: (m) => m.soilHumidity,
        thresholds: (c) => [['Mínimo', c.soilHumidityMinimum, '#fd7e14'], ['Objetivo', c.soilHumidityTarget, '#198754']]
    },
    light: {
        canvas: 'chartLight', label: 'Luminosidad relativa (%)', color: '#ffc107', percent: true,
        value: (m) => m.luminosity,
        thresholds: (c) => [['Mínima', c.minimumLuminosity, '#fd7e14'], ['Objetivo', c.targetLuminosity, '#198754']]
    }
};

const state = {
    devices: [],
    deviceId: null,
    rangeMinutes: 60,
    config: null,
    charts: {}
};

const $ = (id) => document.getElementById(id);
const enc = encodeURIComponent;

/* ---------- Utilidades ---------- */

function make(tag, { text, className, attrs } = {}) {
    const el = document.createElement(tag);
    if (text !== undefined) el.textContent = text;
    if (className) el.className = className;
    if (attrs) {
        for (const [name, value] of Object.entries(attrs)) el.setAttribute(name, value);
    }
    return el;
}

function fmt(value, decimals = 1) {
    return value === null || value === undefined ? '—' : Number(value).toFixed(decimals);
}

function fmtTime(value) {
    return new Date(value).toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

function fmtDateTime(value) {
    return new Date(value).toLocaleString('es-CO', {
        day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit'
    });
}

function ageSeconds(value) {
    return Math.max(0, Math.round((Date.now() - new Date(value).getTime()) / 1000));
}

function ageText(seconds) {
    if (seconds < 60) return `hace ${seconds} s`;
    if (seconds < 3600) return `hace ${Math.floor(seconds / 60)} min`;
    return `hace ${Math.floor(seconds / 3600)} h`;
}

function showBanner(text) {
    const banner = $('alertBanner');
    banner.textContent = text;
    banner.classList.remove('d-none');
}

function hideBanner() {
    $('alertBanner').classList.add('d-none');
}

/* ---------- Acceso a la API ---------- */

async function api(path, options = {}) {
    const headers = { Accept: 'application/json', ...(options.headers || {}) };
    if (options.body) headers['Content-Type'] = 'application/json';

    const response = await fetch(API + path, { ...options, headers });
    const text = await response.text();

    let data = null;
    if (text) {
        try { data = JSON.parse(text); } catch { data = null; }
    }
    return { ok: response.ok, status: response.status, data };
}

function getApiKey(askIfMissing) {
    let key = sessionStorage.getItem(KEY_STORAGE);
    if (!key && askIfMissing) {
        key = (window.prompt('API Key de InvernApp (cabecera X-API-Key):') || '').trim();
        if (key) sessionStorage.setItem(KEY_STORAGE, key);
    }
    return key || null;
}

// Petición que requiere API Key: la pide si no la tiene y la olvida si el servidor la rechaza.
async function apiWithKey(path, options = {}) {
    const key = getApiKey(true);
    if (!key) return { ok: false, status: 0, data: null, cancelled: true };

    const result = await api(path, { ...options, headers: { ...(options.headers || {}), 'X-API-Key': key } });
    if (result.status === 401) sessionStorage.removeItem(KEY_STORAGE);
    return result;
}

function changeApiKey() {
    const current = sessionStorage.getItem(KEY_STORAGE) ? 'definida' : 'no definida';
    const key = window.prompt(`API Key (actualmente ${current}). Déjala vacía para borrarla:`);
    if (key === null) return;
    if (key.trim()) sessionStorage.setItem(KEY_STORAGE, key.trim());
    else sessionStorage.removeItem(KEY_STORAGE);
}

/* ---------- Estado actual ---------- */

function updateDeviceList(list) {
    state.devices = list;
    const select = $('deviceSelect');
    const ids = list.map((d) => d.deviceId);
    const shown = Array.from(select.options).map((o) => o.value);

    if (ids.join('|') !== shown.join('|')) {
        select.replaceChildren(
            ...list.map((d) => make('option', { text: `${d.deviceId} · ${d.name}`, attrs: { value: d.deviceId } }))
        );
    }

    if (!ids.includes(state.deviceId)) state.deviceId = ids[0] ?? null;
    if (state.deviceId) select.value = state.deviceId;
}

function setSoilBadge(measurement) {
    const badge = $('badgeSoil');
    if (!measurement || measurement.soilHumidity === null) {
        badge.textContent = 'Sin dato';
        badge.className = 'badge text-bg-secondary';
    } else if (measurement.soilHumiditySimulated) {
        badge.textContent = 'Simulado';
        badge.className = 'badge text-bg-warning';
    } else {
        badge.textContent = 'Lectura real';
        badge.className = 'badge text-bg-success';
    }
}

function renderLatest(result) {
    const age = $('measurementAge');

    if (result.ok) {
        const m = result.data;
        $('valTemp').textContent = fmt(m.temperature);
        $('valAir').textContent = fmt(m.airHumidity);
        $('valSoil').textContent = fmt(m.soilHumidity);
        $('valLight').textContent = fmt(m.luminosity);
        setSoilBadge(m);

        const seconds = ageSeconds(m.timestamp);
        age.textContent = `Última medición #${m.sequenceNumber}: ${fmtDateTime(m.timestamp)} (${ageText(seconds)})`;
        age.classList.toggle('text-danger', seconds > ONLINE_THRESHOLD_SECONDS);
        age.classList.toggle('text-muted', seconds <= ONLINE_THRESHOLD_SECONDS);
        return;
    }

    for (const id of ['valTemp', 'valAir', 'valSoil', 'valLight']) $(id).textContent = '—';
    setSoilBadge(null);
    age.textContent = result.status === 404
        ? 'Este dispositivo todavía no ha enviado mediciones.'
        : 'No se pudo consultar la última medición.';
    age.classList.remove('text-danger');
    age.classList.add('text-muted');
}

function renderActuators(result) {
    for (const [actuator, id] of Object.entries(ACTUATOR_IDS)) {
        const badge = $(id);
        const since = $(`${id}Since`);
        const item = result.ok ? result.data.find((a) => a.actuator === actuator) : null;

        if (!item) {
            badge.textContent = '—';
            badge.className = 'badge text-bg-secondary fs-6';
            since.textContent = '';
            continue;
        }

        badge.textContent = item.isOn ? 'ON' : 'OFF';
        badge.className = `badge fs-6 ${item.isOn ? 'text-bg-success' : 'text-bg-secondary'}`;
        since.textContent = `desde ${fmtDateTime(item.changedAt)}`;
    }
}

function renderDeviceStatus() {
    const badge = $('espStatus');
    const text = $('espLastSeen');
    const device = state.devices.find((d) => d.deviceId === state.deviceId);

    if (!device || !device.lastSeenAt) {
        badge.textContent = 'Sin comunicación aún';
        badge.className = 'badge fs-6 text-bg-secondary';
        text.textContent = 'Registrado, todavía no ha enviado datos.';
        return;
    }

    const seconds = ageSeconds(device.lastSeenAt);
    const online = seconds <= ONLINE_THRESHOLD_SECONDS;
    badge.textContent = online ? 'En línea' : 'Sin comunicación';
    badge.className = `badge fs-6 ${online ? 'text-bg-success' : 'text-bg-danger'}`;
    text.textContent = `Última comunicación: ${fmtDateTime(device.lastSeenAt)} (${ageText(seconds)})`;
}

function rowMessage(text) {
    const tr = make('tr');
    tr.append(make('td', { text, className: 'text-muted text-center py-3', attrs: { colspan: 4 } }));
    return tr;
}

function renderEvents(result) {
    const body = $('eventsBody');
    body.replaceChildren();

    if (!result.ok) {
        body.append(rowMessage('No se pudieron cargar los eventos.'));
        return;
    }
    if (result.data.length === 0) {
        body.append(rowMessage('Todavía no hay eventos.'));
        return;
    }

    for (const e of result.data) {
        const tr = make('tr');
        tr.append(make('td', { text: fmtDateTime(e.createdAt), className: 'text-nowrap' }));

        const typeCell = make('td', { className: 'text-nowrap' });
        typeCell.append(
            make('span', {
                text: SEVERITY_LABEL[e.severity] || e.severity,
                className: `badge ${SEVERITY_CLASS[e.severity] || 'text-bg-secondary'} me-1`
            }),
            document.createTextNode(EVENT_LABELS[e.type] || e.type)
        );
        tr.append(typeCell);

        tr.append(make('td', { text: e.message }));

        const origin = [e.deviceId || 'Sistema', e.commandId ? `cmd #${e.commandId}` : null].filter(Boolean).join(' · ');
        tr.append(make('td', { text: origin, className: 'text-nowrap small text-muted' }));

        body.append(tr);
    }
}

function renderNoDevice() {
    for (const id of ['valTemp', 'valAir', 'valSoil', 'valLight']) $(id).textContent = '—';
    setSoilBadge(null);
    $('measurementAge').textContent = 'No hay dispositivos registrados. Regístralos con POST /api/v1/devices.';
    renderActuators({ ok: false });
    renderDeviceStatus();
    renderEvents({ ok: true, data: [] });
}

async function refreshStatus() {
    try {
        const devices = await api('/devices');
        if (!devices.ok) throw new Error(`devices ${devices.status}`);

        updateDeviceList(devices.data);
        hideBanner();

        if (!state.deviceId) {
            renderNoDevice();
            return;
        }

        const id = enc(state.deviceId);
        const [latest, actuators, events] = await Promise.all([
            api(`/measurements/latest?deviceId=${id}`),
            api(`/devices/${id}/actuators`),
            api('/events?limit=30')
        ]);

        renderLatest(latest);
        renderActuators(actuators);
        renderDeviceStatus();
        renderEvents(events);
        $('lastRefresh').textContent = `Actualizado ${fmtTime(new Date())}`;
    } catch {
        showBanner('No hay conexión con la API de InvernApp. Verifica que esté en ejecución.');
    }
}

/* ---------- Historial (gráficas) ---------- */

function mainDataset(def) {
    return {
        label: def.label,
        data: [],
        simulated: [],
        borderColor: def.color,
        backgroundColor: def.color,
        borderWidth: 2,
        pointRadius: 0,
        tension: 0.2,
        spanGaps: false,
        // Tramo punteado cuando el dato es simulado
        segment: {
            borderDash: (ctx) => (ctx.chart.data.datasets[0].simulated[ctx.p1DataIndex] ? [6, 4] : undefined)
        }
    };
}

function initCharts() {
    for (const [key, def] of Object.entries(CHART_DEFS)) {
        state.charts[key] = new Chart($(def.canvas), {
            type: 'line',
            data: { labels: [], datasets: [mainDataset(def)] },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: false,
                interaction: { mode: 'index', intersect: false },
                scales: {
                    x: { ticks: { maxTicksLimit: 8, maxRotation: 0 } },
                    y: def.percent ? { min: 0, max: 100 } : { beginAtZero: false }
                },
                plugins: { legend: { position: 'bottom', labels: { boxWidth: 12 } } }
            }
        });
    }
}

function timeLabel(value) {
    const options = { hour: '2-digit', minute: '2-digit' };
    if (state.rangeMinutes <= 60) options.second = '2-digit';
    return new Date(value).toLocaleTimeString('es-CO', options);
}

function updateChart(key, items) {
    const def = CHART_DEFS[key];
    const chart = state.charts[key];
    const main = chart.data.datasets[0];

    chart.data.labels = items.map((m) => timeLabel(m.timestamp));
    main.data = items.map((m) => def.value(m));
    main.simulated = items.map((m) => def.soil === true && m.soilHumiditySimulated === true);

    const lines = state.config ? def.thresholds(state.config) : [];
    chart.data.datasets = [
        main,
        ...lines.map(([name, value, color]) => ({
            label: `${name} (${value})`,
            data: items.map(() => value),
            borderColor: color,
            borderDash: [2, 4],
            borderWidth: 1,
            pointRadius: 0,
            fill: false
        }))
    ];

    chart.update();
}

async function refreshCharts() {
    if (!state.deviceId) return;

    try {
        const from = new Date(Date.now() - state.rangeMinutes * 60000).toISOString();
        const result = await api(`/devices/${enc(state.deviceId)}/measurements?from=${enc(from)}&limit=${HISTORY_LIMIT}`);
        if (!result.ok) return;

        // La API devuelve las más recientes primero; la gráfica va de la más antigua a la más reciente.
        const items = [...result.data].reverse();
        for (const key of Object.keys(CHART_DEFS)) updateChart(key, items);

        let note = items.length === 0
            ? 'No hay mediciones en el rango seleccionado.'
            : `${items.length} medición(es) en el rango.`;
        if (items.length >= HISTORY_LIMIT) note += ` Se muestran solo las últimas ${HISTORY_LIMIT}.`;
        $('chartNote').textContent = note;
    } catch {
        // El aviso de conexión lo gestiona refreshStatus().
    }
}

/* ---------- Configuración ---------- */

function buildConfigForm() {
    const container = $('configFields');

    for (const group of CONFIG_GROUPS) {
        const box = make('fieldset', { className: 'border rounded p-3 mb-3' });
        box.append(make('legend', { text: group.title, className: 'float-none w-auto px-2 fs-6 mb-0' }));

        const toggle = make('div', { className: 'form-check form-switch mb-2' });
        toggle.append(
            make('input', { className: 'form-check-input', attrs: { type: 'checkbox', id: group.enabled, role: 'switch' } }),
            make('label', { className: 'form-check-label', text: group.enabledLabel, attrs: { for: group.enabled } })
        );
        box.append(toggle);

        const row = make('div', { className: 'row g-2' });
        for (const [id, label, min, max, step] of group.fields) {
            const col = make('div', { className: 'col-sm-6' });
            col.append(
                make('label', { className: 'form-label small mb-1', text: label, attrs: { for: id } }),
                make('input', { className: 'form-control form-control-sm', attrs: { type: 'number', id, min, max, step } })
            );
            row.append(col);
        }
        box.append(row);
        container.append(box);
    }
}

function fillConfigForm(config) {
    for (const group of CONFIG_GROUPS) {
        $(group.enabled).checked = Boolean(config[group.enabled]);
        for (const [id] of group.fields) $(id).value = config[id];
    }
    $('configUpdatedAt').textContent = `Última modificación: ${fmtDateTime(config.updatedAt)}`;
}

function collectConfig() {
    const payload = {};
    for (const group of CONFIG_GROUPS) {
        payload[group.enabled] = $(group.enabled).checked;
        for (const [id] of group.fields) {
            const raw = $(id).value.trim();
            payload[id] = raw === '' ? null : Number(raw);
        }
    }
    return payload;
}

function showConfigMessage(text, kind, items = []) {
    const box = $('configMessage');
    box.className = `alert alert-${kind} py-2 small`;
    box.replaceChildren(document.createTextNode(text));

    if (items.length > 0) {
        const list = make('ul', { className: 'mb-0 mt-1' });
        for (const item of items) list.append(make('li', { text: item }));
        box.append(list);
    }
}

function errorMessages(problem) {
    if (problem && problem.errors) return Object.values(problem.errors).flat();
    if (problem && problem.detail) return [problem.detail];
    return ['Error desconocido.'];
}

async function loadConfig() {
    try {
        const result = await api('/configuration');
        if (result.ok) {
            state.config = result.data;
            fillConfigForm(result.data);
        }
    } catch {
        // El aviso de conexión lo gestiona refreshStatus().
    }
}

async function saveConfig(event) {
    event.preventDefault();
    const button = $('btnSaveConfig');
    button.disabled = true;
    showConfigMessage('Guardando…', 'secondary');

    try {
        const result = await apiWithKey('/configuration', { method: 'PUT', body: JSON.stringify(collectConfig()) });

        if (result.cancelled) {
            showConfigMessage('Operación cancelada: se necesita la API Key.', 'warning');
        } else if (result.ok) {
            state.config = result.data;
            fillConfigForm(result.data);
            showConfigMessage('Configuración guardada.', 'success');
            await refreshCharts();
        } else if (result.status === 401) {
            showConfigMessage('API Key incorrecta o ausente. Vuelve a intentarlo.', 'danger');
        } else {
            showConfigMessage('No se guardó la configuración:', 'danger', errorMessages(result.data));
        }
    } catch {
        showConfigMessage('No hay conexión con la API.', 'danger');
    } finally {
        button.disabled = false;
    }
}

/* ---------- Eventos de la interfaz e inicio ---------- */

function bindEvents() {
    $('deviceSelect').addEventListener('change', async (event) => {
        state.deviceId = event.target.value;
        await refreshStatus();
        await refreshCharts();
    });

    $('rangeGroup').addEventListener('click', async (event) => {
        const button = event.target.closest('button[data-minutes]');
        if (!button) return;

        state.rangeMinutes = Number(button.dataset.minutes);
        for (const b of $('rangeGroup').querySelectorAll('button')) b.classList.toggle('active', b === button);
        await refreshCharts();
    });

    $('configForm').addEventListener('submit', saveConfig);

    $('btnReloadConfig').addEventListener('click', async () => {
        await loadConfig();
        showConfigMessage('Valores recargados desde el servidor.', 'secondary');
    });

    $('btnApiKey').addEventListener('click', changeApiKey);
}

async function init() {
    buildConfigForm();
    initCharts();
    bindEvents();

    await loadConfig();
    await refreshStatus();
    await refreshCharts();

    setInterval(refreshStatus, STATUS_REFRESH_MS);
    setInterval(refreshCharts, CHARTS_REFRESH_MS);
}

init();