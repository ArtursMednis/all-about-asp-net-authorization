using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

// A client application calling the API. Start TokenAuthApi first.

var api = new HttpClient { BaseAddress = new Uri("http://localhost:5005") };
var stepCount = 0;

Step("1. Call the API without a token");
await CallApi("api/secret", token: null);

Step("2. Get a token with a wrong secret");
await GetToken("client", "wrong-secret", scope: null);

Step("3. Get a token as \"client\"");
var token = await GetToken("client", "client-secret", scope: null);

Step("4. Look inside the token - it is only signed, not encrypted!");
PrintTokenPayload(token!);

Step("5. Call the API with the token");
await CallApi("api/secret", token);

Step("6. Call the admin endpoint - the token has no admin scope");
await CallApi("api/admin", token);

Step("7. Ask for a scope the client isn't allowed to have");
await GetToken("client", "client-secret", scope: "admin");

Step("8. Get a token with the admin scope as \"admin-client\"");
var adminToken = await GetToken("admin-client", "admin-secret", scope: "admin");
await CallApi("api/admin", adminToken);

Step("9. Change one character of the token");
var tampered = adminToken![..^2] + (adminToken[^2] == 'A' ? 'B' : 'A') + adminToken[^1];
await CallApi("api/admin", tampered);

// ---------------------------------------------------------------------------

async Task<string?> GetToken(string clientId, string clientSecret, string? scope)
{
    // OAuth 2.0 client credentials request: a form POST to the token endpoint.
    var form = new Dictionary<string, string>
    {
        ["grant_type"] = "client_credentials",
        ["client_id"] = clientId,
        ["client_secret"] = clientSecret,
        ["scope"] = scope ?? "",
    };

    var response = await api.PostAsync("connect/token", new FormUrlEncodedContent(form));
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();

    Console.WriteLine($"POST connect/token -> {(int)response.StatusCode} {response.StatusCode}");
    Console.WriteLine(Pretty(json));

    return response.IsSuccessStatusCode ? json.GetProperty("access_token").GetString() : null;
}

async Task CallApi(string path, string? token)
{
    var request = new HttpRequestMessage(HttpMethod.Get, path);
    if (token != null)
    {
        // This header is all it takes - no cookies.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    var response = await api.SendAsync(request);
    Console.WriteLine($"GET {path} -> {(int)response.StatusCode} {response.StatusCode}");

    // On 401 the API explains why in the WWW-Authenticate header.
    foreach (var header in response.Headers.WwwAuthenticate)
    {
        Console.WriteLine($"WWW-Authenticate: {header}");
    }

    var body = await response.Content.ReadAsStringAsync();
    if (body.Length > 0)
    {
        Console.WriteLine(Pretty(JsonSerializer.Deserialize<JsonElement>(body)));
    }
}

void PrintTokenPayload(string jwt)
{
    // A JWT is three base64url parts: header.payload.signature
    var parts = jwt.Split('.');
    Console.WriteLine($"Token:     {jwt}");
    Console.WriteLine($"Header:    {Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[0]))}");
    Console.WriteLine($"Payload:   {Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[1]))}");
    Console.WriteLine($"Signature: {parts[2]}");
}

static string Pretty(JsonElement json) =>
    JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true });

void Step(string title)
{
    // Pause between steps (handy when recording), except before the first one.
    if (stepCount++ > 0 && !Console.IsInputRedirected)
    {
        Console.Write("\nPress Enter to continue...");
        Console.ReadLine();
    }

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"=== {title}");
    Console.ResetColor();
}
