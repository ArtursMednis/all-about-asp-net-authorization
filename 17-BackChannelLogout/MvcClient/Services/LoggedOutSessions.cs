using System.Collections.Concurrent;

namespace MvcClient.Services;

// Session ids ("sid") the auth server told us have ended.
//
// We can't delete a cookie from the server - it lives in the user's browser.
// Instead we remember the ended sessions and reject cookies that belong to them.
//
// Demo only: this lives in memory of a single app instance. With several instances,
// use a shared store (Redis, a database) or server-side sessions.
public class LoggedOutSessions
{
    private readonly ConcurrentDictionary<string, DateTime> sessions = new();

    public void Add(string sessionId) => sessions[sessionId] = DateTime.UtcNow;

    public bool Contains(string sessionId) => sessions.ContainsKey(sessionId);
}
