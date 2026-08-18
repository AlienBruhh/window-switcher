using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace WindowToggleLauncher.Services;

public class PairedSession
{
    public string SessionToken { get; set; } = string.Empty;
    public string? ClientInfo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
}

public class RemoteAuthService
{
    private readonly ConcurrentDictionary<string, PairedSession> _activeSessions = new();
    private readonly object _lock = new();

    public string CurrentPairingToken { get; private set; } = string.Empty;
    public DateTime PairingTokenExpiry { get; private set; } = DateTime.MinValue;

    public event Action? SessionsChanged;

    public int ActiveSessionCount => _activeSessions.Count;

    public RemoteAuthService()
    {
        GenerateNewPairingToken();
    }

    public string GenerateNewPairingToken(TimeSpan? validity = null)
    {
        lock (_lock)
        {
            var bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            CurrentPairingToken = Convert.ToHexString(bytes).ToLowerInvariant();
            PairingTokenExpiry = DateTime.UtcNow.Add(validity ?? TimeSpan.FromMinutes(15));
            return CurrentPairingToken;
        }
    }

    public bool TryPair(string? providedPairingToken, string? clientInfo, out string sessionToken)
    {
        sessionToken = string.Empty;
        if (string.IsNullOrWhiteSpace(providedPairingToken))
            return false;

        lock (_lock)
        {
            if (DateTime.UtcNow > PairingTokenExpiry)
                return false;

            if (!string.Equals(CurrentPairingToken, providedPairingToken.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            // Generate a secure permanent/active session token
            var tokenBytes = new byte[32];
            RandomNumberGenerator.Fill(tokenBytes);
            sessionToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();

            var session = new PairedSession
            {
                SessionToken = sessionToken,
                ClientInfo = clientInfo,
                CreatedAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow
            };

            _activeSessions[sessionToken] = session;

            // Immediately invalidate/rotate the one-time pairing token for security
            GenerateNewPairingToken();
        }

        SessionsChanged?.Invoke();
        return true;
    }

    public bool ValidateSession(string? sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
            return false;

        if (_activeSessions.TryGetValue(sessionToken, out var session))
        {
            session.LastActiveAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    public void RevokeSession(string sessionToken)
    {
        if (_activeSessions.TryRemove(sessionToken, out _))
        {
            SessionsChanged?.Invoke();
        }
    }

    public void RevokeAllSessions()
    {
        _activeSessions.Clear();
        GenerateNewPairingToken();
        SessionsChanged?.Invoke();
    }
}
