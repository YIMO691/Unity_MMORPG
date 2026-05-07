using System.Net.WebSockets;
using MmoDemo.Application;
using MmoDemo.Contracts;
using MmoDemo.Domain;
using MmoDemo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── In-memory repositories (Phase 1) ──
builder.Services.AddSingleton<IPlayerRepository, InMemoryPlayerStore>();
builder.Services.AddSingleton<IRoleRepository, InMemoryRoleStore>();

// ── Application services (Phase 1) ──
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IRoleService, RoleService>();
builder.Services.AddSingleton<ISceneService, SceneService>();

// ── Application services (Phase 2) ──
builder.Services.AddSingleton<IMovementService, MovementService>();
builder.Services.AddSingleton<ISceneManager, SceneManager>();
builder.Services.AddSingleton<IMessageRouter, MessageRouter>();
builder.Services.AddSingleton<IWebSocketHandler, WebSocketHandler>();

// ── Lua config root ──
var configRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "configs"));

// ── Application services (Phase 3) ──
builder.Services.AddSingleton<ICombatService, CombatService>();
builder.Services.AddSingleton<MonsterService>(sp =>
    new MonsterService(sp.GetRequiredService<ISceneManager>(), Path.Combine(configRoot, "monsters.lua")));
builder.Services.AddSingleton<DropService>();
builder.Services.AddSingleton<InventoryService>();

// ── Application services (Phase 4) ──
var questService = new QuestService(Path.Combine(configRoot, "quests.lua"));
builder.Services.AddSingleton<IQuestService>(questService);
builder.Services.AddSingleton(questService);
builder.Services.AddSingleton<IChatService, ChatService>();

// ── Application services (Phase 6) ──
var resourceRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "resources"));
builder.Services.AddSingleton<IResourceService>(new ResourceService(resourceRoot));

var app = builder.Build();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

// ── Initialize default city scene ──
var sceneManager = app.Services.GetRequiredService<ISceneManager>();
sceneManager.AddScene(new Scene
{
    SceneId = "city_001",
    SceneName = "Main City",
    SpawnX = 0, SpawnY = 0, SpawnZ = 0,
    BoundsMinX = -50, BoundsMaxX = 50,
    BoundsMinZ = -50, BoundsMaxZ = 50
});

// Phase 7: Wilderness map
sceneManager.AddScene(new Scene
{
    SceneId = "field_001",
    SceneName = "Wilderness",
    SpawnX = -30, SpawnY = 0, SpawnZ = -30,
    BoundsMinX = -80, BoundsMaxX = 20,
    BoundsMinZ = -80, BoundsMaxZ = 20
});

// Start monster respawn timer
app.Services.GetRequiredService<MonsterService>().StartRespawnTimer(sceneManager);

// ═══════════════ Phase 1 HTTP Endpoints ═══════════════

app.MapGet("/health", () => Results.Ok(new
{
    Status = "OK", Service = "MmoDemo.Gateway", Phase = "Phase 4"
}));

app.MapPost("/api/auth/guest-login", (GuestLoginRequest r, IAuthService s) =>
    s.GuestLogin(r) is { Code: ErrorCodes.Ok } res ? Results.Ok(res) : Results.BadRequest(s.GuestLogin(r)));

app.MapPost("/api/roles/list", (GetRoleListRequest r, IRoleService s) =>
    s.GetRoleList(r) is { Code: ErrorCodes.Ok } res ? Results.Ok(res) : Results.BadRequest(s.GetRoleList(r)));

app.MapPost("/api/roles/create", (CreateRoleRequest r, IRoleService s) =>
    s.CreateRole(r) is { Code: ErrorCodes.Ok } res ? Results.Ok(res) : Results.BadRequest(s.CreateRole(r)));

app.MapPost("/api/roles/select", (SelectRoleRequest r, IRoleService s) =>
    s.SelectRole(r) is { Code: ErrorCodes.Ok } res ? Results.Ok(res) : Results.BadRequest(s.SelectRole(r)));

app.MapPost("/api/scene/enter-city", (EnterCityRequest r, ISceneService s) =>
    s.EnterCity(r) is { Code: ErrorCodes.Ok } res ? Results.Ok(res) : Results.BadRequest(s.EnterCity(r)));

// ═══════════════ Phase 5: Config Reload ═══════════════

app.MapPost("/api/admin/reload-config", (QuestService qs, MonsterService ms) =>
{
    qs.Reload();
    ms.Reload();
    return Results.Ok(new { Message = "Configs reloaded from Lua" });
});

// ═══════════════ Phase 6: Resource Update ═══════════════

app.MapGet("/api/resources/manifest", (IResourceService rs) =>
    Results.Ok(new { files = rs.GetManifest() }));

app.MapGet("/api/resources/{*path}", (string path, IResourceService rs) =>
{
    var bytes = rs.GetResource(path);
    return bytes != null ? Results.File(bytes, "application/octet-stream") : Results.NotFound();
});

// ═══════════════ Phase 2 WebSocket ═══════════════

var _connId = 0;

app.Map("/ws", async (HttpContext ctx, IWebSocketHandler handler) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    var socket = await ctx.WebSockets.AcceptWebSocketAsync();
    var cid = $"conn_{Interlocked.Increment(ref _connId)}";
    await handler.HandleConnectionAsync(socket, cid, ctx.RequestAborted);
});

app.Run();

public partial class Program;
