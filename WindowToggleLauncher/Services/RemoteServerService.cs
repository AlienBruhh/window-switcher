using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WindowToggleLauncher.Services;

public class AppDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Hotkey { get; set; }
    public string? IconBase64 { get; set; }
    public bool IsRunning { get; set; }
    public bool IsMinimized { get; set; }
}

public class PairRequest
{
    public string? Token { get; set; }
    public string? ClientInfo { get; set; }
}

public class ActivateRequest
{
    public string? AppId { get; set; }
}

public class RemoteServerService : IAsyncDisposable
{
    private readonly RemoteAuthService _authService;
    private readonly Func<List<AppDto>> _getAppsCallback;
    private readonly Func<string, Task<bool>> _activateAppCallback;
    private readonly ConcurrentDictionary<Guid, WebSocket> _connectedSockets = new();
    private WebApplication? _app;
    private CancellationTokenSource? _cts;

    public int Port { get; private set; }
    public bool IsRunning { get; private set; }
    public int ConnectedClientCount => _connectedSockets.Count;

    public event Action? ClientCountChanged;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RemoteServerService(
        RemoteAuthService authService,
        Func<List<AppDto>> getAppsCallback,
        Func<string, Task<bool>> activateAppCallback)
    {
        _authService = authService;
        _getAppsCallback = getAppsCallback;
        _activateAppCallback = activateAppCallback;
    }

    public async Task StartAsync(int port)
    {
        Port = port;
        _cts = new CancellationTokenSource();

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>()
        });

        // Use Kestrel server on 0.0.0.0 to allow LAN connections without Admin URL reservations
        builder.WebHost.UseKestrel(options =>
        {
            options.ListenAnyIP(port);
        });

        builder.Services.AddRouting();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        builder.Logging.ClearProviders();

        _app = builder.Build();
        _app.UseCors();
        _app.UseWebSockets();

        // Endpoints
        _app.MapGet("/", async context =>
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(WebAssets.GetIndexHtml());
        });

        _app.MapGet("/connect", async context =>
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(WebAssets.GetIndexHtml());
        });

        _app.MapGet("/manifest.json", async context =>
        {
            context.Response.ContentType = "application/manifest+json";
            await context.Response.WriteAsync(WebAssets.GetManifestJson());
        });

        _app.MapGet("/sw.js", async context =>
        {
            context.Response.ContentType = "application/javascript";
            await context.Response.WriteAsync(WebAssets.GetServiceWorkerJs());
        });

        _app.MapGet("/icon.png", async context =>
        {
            // Simple 1x1 or small PNG icon
            context.Response.ContentType = "image/png";
            var iconBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkWPjfDwAEeQHzpYj0rwAAAABJRU5ErkJggg==");
            await context.Response.Body.WriteAsync(iconBytes);
        });

        _app.MapPost("/api/pair", async (HttpContext context) =>
        {
            try
            {
                var req = await JsonSerializer.DeserializeAsync<PairRequest>(context.Request.Body, JsonOptions);
                if (req == null || string.IsNullOrWhiteSpace(req.Token))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { message = "Pairing token is required." });
                    return;
                }

                if (_authService.TryPair(req.Token, req.ClientInfo, out var sessionToken))
                {
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = true,
                        sessionToken,
                        serverName = Environment.MachineName
                    });
                }
                else
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Invalid or expired pairing code. Please rescan the QR code on your PC."
                    });
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { message = ex.Message });
            }
        });

        _app.MapGet("/api/apps", async (HttpContext context) =>
        {
            var token = ExtractBearerToken(context);
            if (!_authService.ValidateSession(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Unauthorized" });
                return;
            }

            var apps = _getAppsCallback();
            await context.Response.WriteAsJsonAsync(new { apps });
        });

        _app.MapPost("/api/activate", async (HttpContext context) =>
        {
            var token = ExtractBearerToken(context);
            if (!_authService.ValidateSession(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Unauthorized" });
                return;
            }

            var req = await JsonSerializer.DeserializeAsync<ActivateRequest>(context.Request.Body, JsonOptions);
            if (req == null || string.IsNullOrWhiteSpace(req.AppId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { message = "appId is required." });
                return;
            }

            var success = await _activateAppCallback(req.AppId);
            await context.Response.WriteAsJsonAsync(new { success });
        });

        _app.MapGet("/api/status", async (HttpContext context) =>
        {
            await context.Response.WriteAsJsonAsync(new
            {
                serverName = Environment.MachineName,
                connectedClients = _connectedSockets.Count,
                version = "1.0.0"
            });
        });

        _app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var token = context.Request.Query["token"].ToString();
            if (!_authService.ValidateSession(token))
            {
                context.Response.StatusCode = 401;
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var socketId = Guid.NewGuid();
            _connectedSockets[socketId] = webSocket;
            ClientCountChanged?.Invoke();

            try
            {
                // Send initial state
                var initialApps = _getAppsCallback();
                await SendJsonAsync(webSocket, new { type = "apps_update", apps = initialApps });

                var buffer = new byte[1024 * 4];
                while (webSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                }
            }
            catch
            {
                // Client disconnected
            }
            finally
            {
                _connectedSockets.TryRemove(socketId, out _);
                ClientCountChanged?.Invoke();
            }
        });

        await _app.StartAsync(_cts.Token);
        IsRunning = true;
    }

    public async Task BroadcastAppsAsync(IEnumerable<AppDto> apps)
    {
        var json = JsonSerializer.Serialize(new { type = "apps_update", apps }, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var (id, socket) in _connectedSockets)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    _connectedSockets.TryRemove(id, out _);
                }
            }
        }
    }

    public async Task BroadcastUnpairAsync()
    {
        var json = JsonSerializer.Serialize(new { type = "unpaired" }, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var (id, socket) in _connectedSockets)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Unpaired", CancellationToken.None);
                }
                catch
                {
                }
            }
        }
        _connectedSockets.Clear();
        ClientCountChanged?.Invoke();
    }

    private static string? ExtractBearerToken(HttpContext context)
    {
        string? authHeader = context.Request.Headers["Authorization"];
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring(7).Trim();
        }

        if (context.Request.Query.TryGetValue("token", out var queryToken))
        {
            return queryToken.ToString();
        }

        return null;
    }

    private static async Task SendJsonAsync(WebSocket socket, object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task StopAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
        }

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }

        IsRunning = false;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
    }
}
