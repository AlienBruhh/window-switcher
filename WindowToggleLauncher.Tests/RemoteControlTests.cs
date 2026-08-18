using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using WindowToggleLauncher.Services;
using Xunit;

namespace WindowToggleLauncher.Tests;

public class RemoteControlTests
{
    [Fact]
    public void NetworkService_DetectsValidIpAndAvailablePort()
    {
        var ip = NetworkService.GetLocalIpAddress();
        Assert.NotNull(ip);
        Assert.True(IPAddress.TryParse(ip, out var parsedIp));

        var port = NetworkService.FindAvailablePort(8765);
        Assert.InRange(port, 8765, 8800);
    }

    [Fact]
    public void RemoteAuthService_PairingAndSessionLifecycle()
    {
        var authService = new RemoteAuthService();
        var token = authService.CurrentPairingToken;
        Assert.False(string.IsNullOrWhiteSpace(token));

        // Attempt pairing with wrong token
        var failResult = authService.TryPair("invalid-token", "Test Device", out var badSession);
        Assert.False(failResult);
        Assert.Empty(badSession);

        // Attempt pairing with valid token
        var successResult = authService.TryPair(token, "Android Phone", out var sessionToken);
        Assert.True(successResult);
        Assert.False(string.IsNullOrWhiteSpace(sessionToken));

        // Validate session
        Assert.True(authService.ValidateSession(sessionToken));
        Assert.False(authService.ValidateSession("fake-session"));
        Assert.Equal(1, authService.ActiveSessionCount);

        // Old pairing token should now be invalidated / rotated
        var reuseResult = authService.TryPair(token, "Another Device", out _);
        Assert.False(reuseResult);

        // Revoke all sessions
        authService.RevokeAllSessions();
        Assert.Equal(0, authService.ActiveSessionCount);
        Assert.False(authService.ValidateSession(sessionToken));
    }

    [Fact]
    public void QrCodeService_GeneratesBase64AndImage()
    {
        var sampleUrl = "http://192.168.1.100:8765/connect?token=abcd1234efgh5678";
        var base64 = QrCodeService.GenerateQrCodeBase64(sampleUrl);
        Assert.StartsWith("data:image/png;base64,", base64);
    }

    [Fact]
    public async Task RemoteServerService_EndToEndApiTest()
    {
        var authService = new RemoteAuthService();
        var testApps = new List<AppDto>
        {
            new AppDto { Id = "chrome", Name = "Chrome", Hotkey = "1", IsRunning = true, IsMinimized = false },
            new AppDto { Id = "vscode", Name = "VS Code", Hotkey = "2", IsRunning = false, IsMinimized = false }
        };

        string? activatedAppId = null;
        var server = new RemoteServerService(
            authService,
            () => testApps,
            appId =>
            {
                activatedAppId = appId;
                return Task.FromResult(true);
            }
        );

        var testPort = NetworkService.FindAvailablePort(9870);
        await server.StartAsync(testPort);
        Assert.True(server.IsRunning);

        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{testPort}") };

        try
        {
            // 1. GET / and /connect HTML
            var connectRes = await client.GetAsync($"/connect?token={authService.CurrentPairingToken}");
            Assert.Equal(HttpStatusCode.OK, connectRes.StatusCode);
            var html = await connectRes.Content.ReadAsStringAsync();
            Assert.Contains("DeskDeck", html);

            // 2. GET /manifest.json
            var manifestRes = await client.GetAsync("/manifest.json");
            Assert.Equal(HttpStatusCode.OK, manifestRes.StatusCode);

            // 3. Unauthorized access to /api/apps without token
            var unauthAppsRes = await client.GetAsync("/api/apps");
            Assert.Equal(HttpStatusCode.Unauthorized, unauthAppsRes.StatusCode);

            // 4. Unauthorized activation
            var unauthActRes = await client.PostAsJsonAsync("/api/activate", new { appId = "chrome" });
            Assert.Equal(HttpStatusCode.Unauthorized, unauthActRes.StatusCode);

            // 5. POST /api/pair with invalid token
            var badPairRes = await client.PostAsJsonAsync("/api/pair", new { token = "wrong-token" });
            Assert.Equal(HttpStatusCode.Unauthorized, badPairRes.StatusCode);

            // 6. POST /api/pair with valid token
            var pairingToken = authService.CurrentPairingToken;
            var pairRes = await client.PostAsJsonAsync("/api/pair", new { token = pairingToken, clientInfo = "Test Runner" });
            Assert.Equal(HttpStatusCode.OK, pairRes.StatusCode);

            var pairJson = await pairRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(pairJson.GetProperty("success").GetBoolean());
            var sessionToken = pairJson.GetProperty("sessionToken").GetString();
            Assert.NotNull(sessionToken);

            // 7. GET /api/apps with valid bearer token
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/apps");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sessionToken);
            var authAppsRes = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, authAppsRes.StatusCode);

            var appsJson = await authAppsRes.Content.ReadFromJsonAsync<JsonElement>();
            var appsArray = appsJson.GetProperty("apps");
            Assert.Equal(2, appsArray.GetArrayLength());
            Assert.Equal("Chrome", appsArray[0].GetProperty("name").GetString());

            // 8. POST /api/activate with valid token
            var actRequest = new HttpRequestMessage(HttpMethod.Post, "/api/activate")
            {
                Content = JsonContent.Create(new { appId = "vscode" })
            };
            actRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sessionToken);
            var actRes = await client.SendAsync(actRequest);
            Assert.Equal(HttpStatusCode.OK, actRes.StatusCode);
            Assert.Equal("vscode", activatedAppId);

            // 9. WebSocket connection with session token
            using var wsCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var ws = new ClientWebSocket();
            var wsUri = new Uri($"ws://127.0.0.1:{testPort}/ws?token={sessionToken}");
            await ws.ConnectAsync(wsUri, wsCts.Token);
            Assert.Equal(WebSocketState.Open, ws.State);

            // Receive initial apps push
            var buffer = new byte[4096];
            var wsResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), wsCts.Token);
            var wsMsg = Encoding.UTF8.GetString(buffer, 0, wsResult.Count);
            Assert.Contains("apps_update", wsMsg);
            Assert.Contains("Chrome", wsMsg);

            // 10. Broadcast update from server
            testApps.Add(new AppDto { Id = "spotify", Name = "Spotify", Hotkey = "3" });
            await server.BroadcastAppsAsync(testApps);

            var wsResult2 = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), wsCts.Token);
            var wsMsg2 = Encoding.UTF8.GetString(buffer, 0, wsResult2.Count);
            Assert.Contains("Spotify", wsMsg2);

            // 11. Broadcast unpair
            authService.RevokeAllSessions();
            await server.BroadcastUnpairAsync();

            try
            {
                var wsResult3 = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), wsCts.Token);
                if (wsResult3.MessageType == WebSocketMessageType.Text)
                {
                    var wsMsg3 = Encoding.UTF8.GetString(buffer, 0, wsResult3.Count);
                    Assert.Contains("unpaired", wsMsg3);
                }
            }
            catch (WebSocketException)
            {
                // Socket was closed by server unpair broadcast
            }

            // 12. Subsequent API requests fail with 401
            var revokedAppsRes = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/apps")
            {
                Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sessionToken) }
            });
            Assert.Equal(HttpStatusCode.Unauthorized, revokedAppsRes.StatusCode);
        }
        finally
        {
            await server.StopAsync();
        }
    }

    [Fact]
    public void RemoteAuthService_ExpiredPairingTokenFails()
    {
        var authService = new RemoteAuthService();
        authService.GenerateNewPairingToken(TimeSpan.FromMilliseconds(-100)); // already expired
        var result = authService.TryPair(authService.CurrentPairingToken, "Expired Test", out var session);
        Assert.False(result);
        Assert.Empty(session);
    }
}
