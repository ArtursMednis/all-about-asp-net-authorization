# 04 - SPA with cookie authentication (React, Angular, Vue)

A single-page app that logs in with a plain **HttpOnly cookie** - no tokens in JavaScript. One ASP.NET Core API, and the same small app built three times: **React**, **Angular** and **Vue**. Each has a login page, a **route guard** with return URL, and pages that load protected data from the API.

The guard is for the user's convenience; the API checks the cookie on every request and answers **401** (not logged in) or **403** (not allowed).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 22 or newer (Angular 21 needs at least 22.12)

## Run

Start the API:

```bash
cd 04-SpaCookieAuth
dotnet run --project Api
```

Then, in a second terminal, start **one** of the clients:

| Client | Commands | Open |
|---|---|---|
| React | `cd react-client` → `npm install` → `npm run dev` | http://localhost:5111 |
| Angular | `cd angular-client` → `npm install` → `npm start` | http://localhost:5112 |
| Vue | `cd vue-client` → `npm install` → `npm run dev` | http://localhost:5113 |

Log in as `user` / `user` or `admin` / `admin`.

Each dev server forwards `/api` to the ASP.NET Core API (http://localhost:5110), so the browser only sees one origin: the cookie just works, no CORS needed. Development runs on plain HTTP to keep the proxy simple; use HTTPS in production.

## What to try

1. Click **Secret**: the route guard sends you to `/login?returnUrl=/secret`. Log in and you are back on Secret, with claims from the API.
2. Developer tools → **Application → Cookies**: the `SpaAuth` cookie is HttpOnly. In the console, `document.cookie` is empty.
3. **Admin** as `user`: the guard lets you in (you are logged in), but the API answers 403 → "Access denied".
4. Reload the page: you are still logged in - the app asks the API who you are.

## Key code - API

### Service configuration (`Api/Program.cs`)

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SpaAuth";
        options.Cookie.HttpOnly = true;                  // JavaScript can't read it
        options.Cookie.SameSite = SameSiteMode.Strict;   // never sent with cross-site requests
    });

app.UseAuthentication();
app.UseAuthorization();
```

For `[ApiController]` endpoints, ASP.NET Core (.NET 10) answers 401 / 403 instead of redirecting to a login page.

### Login endpoint (`Api/Controllers/AccountController.cs`)

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    // verify the password, build the claims...
    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Ok(new { name, isAdmin });
}
```

The response sets the cookie; the body tells the SPA who logged in.

### Who am I?

```csharp
[Authorize]
[HttpGet("me")]
public IActionResult Me() => Ok(new { name, isAdmin });
```

Called when the SPA starts: the cookie survives page reloads, but JavaScript can't read it.

### Protected endpoints (`Api/Controllers/ResourceController.cs`)

```csharp
[Authorize]
[HttpGet("api/secret")]

[Authorize(Policy = "AdminOnly")]
[HttpGet("api/admin")]
```

## Key code - React (`react-client`)

```ts
// vite.config.ts
server: { port: 5111, proxy: { '/api': 'http://localhost:5110' } }

// api.ts - same origin: no token, no credentials option
fetch(url, { method, headers, body })
```

```tsx
// auth.tsx - auth state
const [user, setUser] = useState<User | null>(null)
useEffect(() => {
  api('GET', '/api/account/me').then(r => setUser(r.status === 200 ? r.data : null))
}, [])

// Route guard
function RequireAuth({ children }) {
  const { user } = useAuth()
  if (!user) return <Navigate to={`/login?returnUrl=${encodeURIComponent(location.pathname)}`} />
  return children
}

<Route path="/secret" element={<RequireAuth><ProtectedPage /></RequireAuth>} />
```

## Key code - Angular (`angular-client`)

```jsonc
// proxy.conf.json (wired in angular.json → serve.options.proxyConfig)
{ "/api": { "target": "http://localhost:5110" } }
```

```ts
// auth.ts - auth state, loaded before routing starts
@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly user = signal<User | null>(null);
  async load() {
    this.user.set(await firstValueFrom(this.http.get<User>('/api/account/me')));
  }
}
provideAppInitializer(() => inject(AuthService).load())

// Route guard
export const authGuard: CanActivateFn = (_route, state) =>
  inject(AuthService).user()
    ? true
    : inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });

{ path: 'secret', component: ProtectedPage, canActivate: [authGuard] }

// Interceptor: every HttpClient call that gets 401 goes to the login page
export const authInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(catchError(error => {
    if (error.status === 401) router.navigate(['/login'], ...);
    return throwError(() => error);
  }));
```

## Key code - Vue (`vue-client`)

```ts
// vite.config.ts
server: { port: 5113, proxy: { '/api': 'http://localhost:5110' } }

// auth.ts - a shared ref is all the state we need
export const user = ref<User | null>(null)
export const userLoaded = api('GET', '/api/account/me').then(r => {
  user.value = r.status === 200 ? r.data : null
})

// router.ts - route guard
{ path: '/secret', component: ProtectedPage, meta: { requiresAuth: true } },

router.beforeEach(async to => {
  await userLoaded
  if (to.meta.requiresAuth && !user.value)
    return { path: '/login', query: { returnUrl: to.fullPath } }
})
```

## Key code - all clients

Login, then return only to paths inside the app:

```ts
const r = await api('POST', '/api/account/login', { userName, password })
// store r.data as the current user, then:
navigate(safeReturnUrl(returnUrl))   // only "/..." paths, never "//..." or "https://..."
```

Pages let the API have the last word: **401** → login page, **403** → "Access denied".

## Project structure

| Folder | What it is |
|---|---|
| `Api` | ASP.NET Core API: cookie auth, login/logout/me, protected endpoints |
| `react-client` | React 19 + React Router + Vite |
| `angular-client` | Angular 21 (router, guard, interceptor) |
| `vue-client` | Vue 3 + Vue Router + Vite |
