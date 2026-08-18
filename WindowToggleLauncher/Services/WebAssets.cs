namespace WindowToggleLauncher.Services;

public static class WebAssets
{
    public static string GetIndexHtml()
    {
        return """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover">
    <meta name="theme-color" content="#0b0f19">
    <meta name="apple-mobile-web-app-capable" content="yes">
    <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent">
    <title>DeskDeck - Free Touch Macro Pad &amp; Windows App Switcher</title>
    <meta name="description" content="Turn your phone into a free touch Stream Deck and Windows app switcher. Instant QR code connection, real-time app switching over local Wi-Fi.">
    <meta name="keywords" content="stream deck, app switcher, windows remote control, macro pad, pc remote, touch controller, free stream deck">
    <meta property="og:title" content="DeskDeck - Phone Remote &amp; Windows App Switcher">
    <meta property="og:description" content="Turn your phone into a free touch Stream Deck for Windows with instant QR pairing.">
    <link rel="manifest" href="/manifest.json">
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
    <style>
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            -webkit-tap-highlight-color: transparent;
            user-select: none;
            -webkit-user-select: none;
        }

        body {
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background-color: #0b0f19;
            color: #f1f5f9;
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            padding-bottom: env(safe-area-inset-bottom, 20px);
            overflow-x: hidden;
        }

        header {
            position: sticky;
            top: 0;
            z-index: 100;
            background: rgba(11, 15, 25, 0.9);
            backdrop-filter: blur(16px);
            -webkit-backdrop-filter: blur(16px);
            border-bottom: 1px solid rgba(255, 255, 255, 0.08);
            padding: 16px 20px;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .header-title {
            display: flex;
            flex-direction: column;
        }

        .header-title h1 {
            font-size: 1.15rem;
            font-weight: 700;
            letter-spacing: -0.02em;
            color: #ffffff;
        }

        .header-subtitle {
            font-size: 0.8rem;
            color: #94a3b8;
            margin-top: 2px;
        }

        .status-badge {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 5px 12px;
            border-radius: 9999px;
            font-size: 0.75rem;
            font-weight: 600;
            background: rgba(255, 255, 255, 0.05);
            border: 1px solid rgba(255, 255, 255, 0.1);
            transition: all 0.3s ease;
        }

        .status-dot {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background-color: #94a3b8;
            transition: background-color 0.3s ease;
        }

        .status-badge.connected {
            background: rgba(16, 185, 129, 0.15);
            border-color: rgba(16, 185, 129, 0.3);
            color: #34d399;
        }
        .status-badge.connected .status-dot {
            background-color: #10b981;
            box-shadow: 0 0 10px #10b981;
            animation: pulse-dot 2s infinite;
        }

        .status-badge.connecting {
            background: rgba(245, 158, 11, 0.15);
            border-color: rgba(245, 158, 11, 0.3);
            color: #fbbf24;
        }
        .status-badge.connecting .status-dot {
            background-color: #f59e0b;
        }

        .status-badge.disconnected {
            background: rgba(239, 68, 68, 0.15);
            border-color: rgba(239, 68, 68, 0.3);
            color: #f87171;
        }
        .status-badge.disconnected .status-dot {
            background-color: #ef4444;
        }

        @keyframes pulse-dot {
            0% { transform: scale(0.95); opacity: 0.8; }
            50% { transform: scale(1.15); opacity: 1; }
            100% { transform: scale(0.95); opacity: 0.8; }
        }

        main {
            flex: 1;
            padding: 20px;
            max-width: 600px;
            width: 100%;
            margin: 0 auto;
            display: flex;
            flex-direction: column;
            gap: 16px;
        }

        .section-label {
            font-size: 0.75rem;
            text-transform: uppercase;
            letter-spacing: 0.08em;
            color: #64748b;
            font-weight: 700;
            margin-bottom: 4px;
        }

        .app-grid {
            display: grid;
            grid-template-columns: 1fr;
            gap: 12px;
        }

        @media (min-width: 480px) {
            .app-grid {
                grid-template-columns: repeat(2, 1fr);
            }
        }

        .app-card {
            background: rgba(22, 31, 48, 0.75);
            border: 1px solid rgba(255, 255, 255, 0.08);
            border-radius: 16px;
            padding: 14px 16px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            cursor: pointer;
            position: relative;
            overflow: hidden;
            transition: transform 0.15s cubic-bezier(0.4, 0, 0.2, 1), background-color 0.15s, border-color 0.15s, box-shadow 0.15s;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.2);
        }

        .app-card:active {
            transform: scale(0.96);
            background: rgba(30, 43, 67, 0.95);
            border-color: rgba(59, 130, 246, 0.6);
        }

        .app-card.activating {
            background: rgba(59, 130, 246, 0.3) !important;
            border-color: #3b82f6 !important;
            box-shadow: 0 0 25px rgba(59, 130, 246, 0.5);
        }

        .app-info {
            display: flex;
            align-items: center;
            gap: 14px;
            min-width: 0;
            flex: 1;
        }

        .app-icon-wrap {
            width: 44px;
            height: 44px;
            border-radius: 12px;
            background: linear-gradient(135deg, #1e293b, #0f172a);
            border: 1px solid rgba(255, 255, 255, 0.1);
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.25rem;
            font-weight: 700;
            color: #60a5fa;
            flex-shrink: 0;
            overflow: hidden;
            box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.1);
        }

        .app-icon-img {
            width: 32px;
            height: 32px;
            object-fit: contain;
            border-radius: 4px;
        }

        .app-details {
            min-width: 0;
            flex: 1;
        }

        .app-name {
            font-size: 1.05rem;
            font-weight: 600;
            color: #f8fafc;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            line-height: 1.3;
        }

        .app-state-text {
            font-size: 0.75rem;
            color: #94a3b8;
            display: flex;
            align-items: center;
            gap: 5px;
            margin-top: 2px;
        }

        .state-indicator {
            width: 6px;
            height: 6px;
            border-radius: 50%;
            background-color: #64748b;
        }
        .state-indicator.running {
            background-color: #10b981;
            box-shadow: 0 0 6px #10b981;
        }
        .state-indicator.minimized {
            background-color: #f59e0b;
        }

        .app-hotkey-badge {
            background: rgba(255, 255, 255, 0.06);
            border: 1px solid rgba(255, 255, 255, 0.12);
            color: #93c5fd;
            font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
            font-size: 0.8rem;
            font-weight: 600;
            padding: 4px 8px;
            border-radius: 8px;
            flex-shrink: 0;
            margin-left: 8px;
        }

        /* Screen States */
        .state-screen {
            display: none;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            text-align: center;
            padding: 40px 20px;
            min-height: 50vh;
        }

        .state-screen.active {
            display: flex;
        }

        .state-icon {
            font-size: 3rem;
            margin-bottom: 16px;
        }

        .state-screen h2 {
            font-size: 1.25rem;
            font-weight: 700;
            margin-bottom: 8px;
            color: #f8fafc;
        }

        .state-screen p {
            font-size: 0.9rem;
            color: #94a3b8;
            max-width: 320px;
            line-height: 1.5;
            margin-bottom: 24px;
        }

        .btn-action {
            background: #2563eb;
            color: #ffffff;
            border: none;
            padding: 12px 24px;
            border-radius: 12px;
            font-weight: 600;
            font-size: 0.95rem;
            cursor: pointer;
            transition: background-color 0.2s, transform 0.1s;
        }

        .btn-action:active {
            background: #1d4ed8;
            transform: scale(0.97);
        }

        .toast {
            position: fixed;
            bottom: 24px;
            left: 50%;
            transform: translateX(-50%) translateY(100px);
            background: rgba(15, 23, 42, 0.95);
            border: 1px solid rgba(255, 255, 255, 0.15);
            color: #f8fafc;
            padding: 10px 18px;
            border-radius: 9999px;
            font-size: 0.85rem;
            font-weight: 500;
            box-shadow: 0 10px 30px rgba(0,0,0,0.5);
            pointer-events: none;
            transition: transform 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
            z-index: 1000;
        }

        .toast.show {
            transform: translateX(-50%) translateY(0);
        }
    </style>
</head>
<body>
    <header>
        <div class="header-title">
            <h1 id="serverNameTitle">DeskDeck Remote</h1>
            <span class="header-subtitle" id="connectionInfo">Tap an app to open / switch</span>
        </div>
        <div id="statusBadge" class="status-badge connecting">
            <div class="status-dot"></div>
            <span id="statusText">Connecting...</span>
        </div>
    </header>

    <main>
        <!-- Main App Remote Control Area -->
        <div id="remoteContainer" style="display: none;">
            <div class="section-label">Configured Applications</div>
            <div id="appList" class="app-grid"></div>
        </div>

        <!-- Empty Apps State -->
        <div id="emptyScreen" class="state-screen">
            <div class="state-icon">📱</div>
            <h2>No Applications Configured</h2>
            <p>Add applications in the Windows app on your PC to control them from here.</p>
        </div>

        <!-- Disconnected / Error State -->
        <div id="errorScreen" class="state-screen">
            <div class="state-icon" id="errorIcon">⚠️</div>
            <h2 id="errorTitle">Unable to Connect</h2>
            <p id="errorMessage">Make sure your phone and PC are connected to the same Wi-Fi network.</p>
            <button id="retryBtn" class="btn-action" onclick="initConnection()">Retry Connection</button>
        </div>
    </main>

    <div id="toast" class="toast">Action triggered</div>

    <script>
        const STORAGE_KEY = 'wtl_session_token';
        let sessionToken = localStorage.getItem(STORAGE_KEY);
        let socket = null;
        let appsCache = [];
        let reconnectTimer = null;

        function showToast(msg) {
            const toast = document.getElementById('toast');
            toast.textContent = msg;
            toast.classList.add('show');
            setTimeout(() => toast.classList.remove('show'), 1500);
        }

        function triggerHaptic() {
            if (navigator.vibrate) {
                try { navigator.vibrate(35); } catch(e) {}
            }
        }

        function updateStatusBadge(state, text) {
            const badge = document.getElementById('statusBadge');
            const statusText = document.getElementById('statusText');
            badge.className = `status-badge ${state}`;
            statusText.textContent = text;
        }

        function showView(view) {
            document.getElementById('remoteContainer').style.display = view === 'apps' ? 'block' : 'none';
            document.getElementById('emptyScreen').classList.toggle('active', view === 'empty');
            document.getElementById('errorScreen').classList.toggle('active', view === 'error');
        }

        async function pairWithToken(pairingToken) {
            updateStatusBadge('connecting', 'Pairing...');
            try {
                const res = await fetch('/api/pair', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        token: pairingToken,
                        clientInfo: navigator.userAgent.includes('Android') ? 'Android Phone' : navigator.userAgent
                    })
                });

                if (res.ok) {
                    const data = await res.json();
                    sessionToken = data.sessionToken || data.SessionToken;
                    localStorage.setItem(STORAGE_KEY, sessionToken);
                    const serverName = data.serverName || data.ServerName;
                    if (serverName) {
                        document.getElementById('serverNameTitle').textContent = serverName;
                    }
                    // Clean URL without reloading
                    window.history.replaceState({}, document.title, window.location.pathname);
                    connectWebSocket();
                    return true;
                } else {
                    const err = await res.json().catch(() => ({}));
                    showError(
                        'Pairing Code Expired', 
                        err.message || 'This pairing code is invalid or has expired. Please scan the new QR code on your PC.',
                        '🔑'
                    );
                    return false;
                }
            } catch (err) {
                showError(
                    'Connection Failed',
                    'Unable to reach PC. Ensure your phone is connected to the same local Wi-Fi.',
                    '📡'
                );
                return false;
            }
        }

        function showError(title, message, icon = '⚠️') {
            document.getElementById('errorTitle').textContent = title;
            document.getElementById('errorMessage').textContent = message;
            document.getElementById('errorIcon').textContent = icon;
            updateStatusBadge('disconnected', 'Disconnected');
            showView('error');
        }

        function renderApps(apps) {
            appsCache = apps || [];
            const container = document.getElementById('appList');
            container.innerHTML = '';

            if (appsCache.length === 0) {
                showView('empty');
                return;
            }

            showView('apps');

            appsCache.forEach(app => {
                const id = app.id || app.Id || '';
                const name = app.name || app.Name || 'Application';
                const hotkey = app.hotkey || app.Hotkey;
                const iconBase64 = app.iconBase64 || app.IconBase64;
                const isRunning = app.isRunning ?? app.IsRunning ?? false;
                const isMinimized = app.isMinimized ?? app.IsMinimized ?? false;

                const card = document.createElement('div');
                card.className = 'app-card';
                card.id = `app-${id}`;
                card.setAttribute('data-app-id', id);
                card.onclick = () => activateApp(id, card);

                const stateClass = isRunning ? (isMinimized ? 'minimized' : 'running') : '';
                const stateLabel = isRunning ? (isMinimized ? 'Minimized' : 'Active') : 'Ready';

                const iconHtml = iconBase64
                    ? `<img class="app-icon-img" src="${iconBase64}" alt="${escapeHtml(name)}" />`
                    : `<span>${escapeHtml(name.charAt(0).toUpperCase())}</span>`;

                card.innerHTML = `
                    <div class="app-info">
                        <div class="app-icon-wrap">${iconHtml}</div>
                        <div class="app-details">
                            <div class="app-name">${escapeHtml(name)}</div>
                            <div class="app-state-text">
                                <span class="state-indicator ${stateClass}"></span>
                                <span>${stateLabel}</span>
                            </div>
                        </div>
                    </div>
                    ${hotkey ? `<div class="app-hotkey-badge">${escapeHtml(hotkey)}</div>` : ''}
                `;

                container.appendChild(card);
            });
        }

        function escapeHtml(str) {
            if (!str) return '';
            return str.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
        }

        async function activateApp(appId, cardElement) {
            if (!appId) return;
            triggerHaptic();
            if (cardElement) {
                cardElement.classList.add('activating');
                setTimeout(() => cardElement.classList.remove('activating'), 300);
            }

            try {
                const res = await fetch('/api/activate', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${sessionToken}`
                    },
                    body: JSON.stringify({ appId: appId })
                });

                if (res.ok) {
                    const app = appsCache.find(a => (a.id || a.Id) === appId);
                    const name = app ? (app.name || app.Name) : 'Application';
                    showToast(`Switched to ${name}`);
                } else if (res.status === 401) {
                    localStorage.removeItem(STORAGE_KEY);
                    showError('Session Expired', 'Please rescan the QR code on your PC to reconnect.', '🔑');
                }
            } catch (err) {
                showToast('Action failed - PC offline');
            }
        }

        async function fetchApps() {
            if (!sessionToken) return;
            try {
                const res = await fetch('/api/apps', {
                    headers: { 'Authorization': `Bearer ${sessionToken}` }
                });
                if (res.ok) {
                    const data = await res.json();
                    renderApps(data.apps || data.Apps || data);
                } else if (res.status === 401) {
                    localStorage.removeItem(STORAGE_KEY);
                    showError('Session Expired', 'Please rescan the QR code on your PC to reconnect.', '🔑');
                }
            } catch (err) {
                console.error('Fetch apps error:', err);
            }
        }

        function connectWebSocket() {
            if (!sessionToken) return;

            if (socket) {
                try { socket.close(); } catch(e) {}
            }

            const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
            const wsUrl = `${protocol}//${window.location.host}/ws?token=${encodeURIComponent(sessionToken)}`;

            updateStatusBadge('connecting', 'Connecting...');

            try {
                socket = new WebSocket(wsUrl);

                socket.onopen = () => {
                    updateStatusBadge('connected', 'Connected');
                    fetchApps();
                    if (reconnectTimer) {
                        clearTimeout(reconnectTimer);
                        reconnectTimer = null;
                    }
                };

                socket.onmessage = (event) => {
                    try {
                        const msg = JSON.parse(event.data);
                        if (msg.type === 'apps_update' || msg.Type === 'apps_update') {
                            renderApps(msg.apps || msg.Apps);
                        } else if (msg.type === 'unpaired' || msg.Type === 'unpaired') {
                            localStorage.removeItem(STORAGE_KEY);
                            showError('Disconnected', 'The PC disconnected this device.', '🔒');
                            socket.close();
                        }
                    } catch (e) {
                        console.error('WS Parse Error', e);
                    }
                };

                socket.onclose = (event) => {
                    if (event.code === 4001) {
                        localStorage.removeItem(STORAGE_KEY);
                        showError('Session Expired', 'Please rescan the QR code on your PC.', '🔑');
                        return;
                    }

                    updateStatusBadge('disconnected', 'Reconnecting...');
                    scheduleReconnect();
                };

                socket.onerror = () => {
                    updateStatusBadge('disconnected', 'Reconnecting...');
                };
            } catch (err) {
                scheduleReconnect();
            }
        }

        function scheduleReconnect() {
            if (reconnectTimer) return;
            reconnectTimer = setTimeout(() => {
                reconnectTimer = null;
                if (sessionToken) {
                    connectWebSocket();
                }
            }, 3000);
        }

        async function initConnection() {
            const urlParams = new URLSearchParams(window.location.search);
            const tokenFromUrl = urlParams.get('token');

            if (tokenFromUrl) {
                await pairWithToken(tokenFromUrl);
            } else if (sessionToken) {
                connectWebSocket();
                await fetchApps();
            } else {
                showError(
                    'Pairing Required',
                    'Scan the QR code displayed on your PC with your phone camera to connect.',
                    '📷'
                );
            }
        }

        // Register Service Worker for PWA if supported
        if ('serviceWorker' in navigator) {
            window.addEventListener('load', () => {
                navigator.serviceWorker.register('/sw.js').catch(() => {});
            });
        }

        window.addEventListener('DOMContentLoaded', initConnection);
    </script>
</body>
</html>
""";
    }

    public static string GetManifestJson()
    {
        return """
{
  "name": "DeskDeck - Phone Macro Remote",
  "short_name": "DeskDeck",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#0b0f19",
  "theme_color": "#0b0f19",
  "icons": [
    {
      "src": "/icon.png",
      "sizes": "192x192",
      "type": "image/png"
    }
  ]
}
""";
    }

    public static string GetServiceWorkerJs()
    {
        return """
const CACHE_NAME = 'wtl-remote-v1';
self.addEventListener('install', event => {
  self.skipWaiting();
});
self.addEventListener('activate', event => {
  event.waitUntil(self.clients.claim());
});
self.addEventListener('fetch', event => {
  event.respondWith(fetch(event.request).catch(() => new Response('Offline', { status: 503 })));
});
""";
    }
}
