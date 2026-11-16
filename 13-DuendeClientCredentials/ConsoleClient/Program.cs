using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

// A client application calling the API. Start AuthServer and ResourceApi first.

const string AuthServer = "https://localhost:7071";
const string ResourceApi = "https://localhost:7072";

var http = new HttpClient();
var stepCount = 0;

Step("1. Discover the auth server's endpoints");
var tokenEndpoint = await DiscoverTokenEndpoint();

Step("2. Call the API without a token");
await CallApi("api/secret", token: null);

Step("3. Get a token with a wrong secret");
await GetToken("client", "wrong-secret", scope: "api");

Step("4. Get a token as \"client\" with the \"api\" scope");
var token = await GetToken("client", "client-secret", scope: "api");

Step("5. Look inside the token");
PrintTokenPayload(token!);

Step("6. Call the API with the token");
await CallApi("api/secret", token);

Step("7. Call the admin endpoint - the token has no admin scope");
await CallApi("api/admin", token);

Step("8. Ask for a scope the client isn't allowed to have");
await GetToken("client", "client-secret", scope: "api admin");

Step("9. Get a token with the admin scope as \"admin-client\"");
var adminToken = await GetToken("admin-client", "admin-secret", scope: "api admin");
await CallApi("api/admin", adminToken);

// ---------------------------------------------------------------------------

async Task<string> DiscoverTokenEndpoint()
{
    // Every OAuth 2.0 / OpenID Connect server publishes this document.
    var url = $"{AuthServer}/.well-known/openid-configuration";
    var discovery = await http.GetFromJsonAsync<JsonElement>(url);

    Console.WriteLine($"GET {url}");
    Console.WriteLine($"issuer:                {discovery.GetProperty("issuer")}");
    Console.WriteLine($"token_endpoint:        {discovery.GetProperty("token_endpoint")}");
    Console.WriteLine($"jwks_uri:              {discovery.GetProperty("jwks_uri")}");
    Console.WriteLine($"grant_types_supported: {discovery.GetProperty("grant_types_supported")}");

    return discovery.GetProperty("token_endpoint").GetString()!;
}

async Task<string?> GetToken(string clientId, string clientSecret, string scope)
{
    // OAuth 2.0 client credentials request: a form POST to the token endpoint.
    var form = new Dictionary<string, string>
    {
        ["grant_type"] = "client_credentials",
        ["client_id"] = clientId,
        ["client_secret"] = clientSecret,
        ["scope"] = scope,
    };

    var response = await http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form));
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();

    Console.WriteLine($"POST {tokenEndpoint} -> {(int)response.StatusCode} {response.StatusCode}");
    foreach (var property in json.EnumerateObject())
    {
        var value = property.Value.ToString();
        Console.WriteLine($"  {property.Name}: {(value.Length > 60 ? value[..60] + "..." : value)}");
    }

    return response.IsSuccessStatusCode ? json.GetProperty("access_token").GetString() : null;
}

async Task CallApi(string path, string? token)
{
    var request = new HttpRequestMessage(HttpMethod.Get, $"{ResourceApi}/{path}");
    if (token != null)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    var response = await http.SendAsync(request);
    Console.WriteLine($"GET {path} -> {(int)response.StatusCode} {response.StatusCode}");

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
    Console.WriteLine($"Header:  {Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[0]))}");
    Console.WriteLine($"Payload: {Pretty(JsonSerializer.Deserialize<JsonElement>(Base64Url.DecodeFromChars(parts[1])))}");
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
