using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SinochkuGames.Server.Data;
using SinochkuGames.Server.Endpoints;
using SinochkuGames.Server.Hubs;
using SinochkuGames.Server.Services;

var builder = WebApplication.CreateBuilder(args);

var dataRoot = builder.Configuration["SINOCHKU_DATA_DIR"];
if (string.IsNullOrWhiteSpace(dataRoot))
    dataRoot = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataRoot);

builder.Services.AddDbContext<SocialDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(dataRoot, "sinochku-social.db")}"));

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-";
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 8;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<SocialDbContext>()
    .AddSignInManager();

var jwtKey = builder.Configuration["SINOCHKU_JWT_KEY"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("SINOCHKU_JWT_KEY must be set in non-development environments.");

    jwtKey = "development-only-sinochku-jwt-key-change-me-please";
    builder.Configuration["SINOCHKU_JWT_KEY"] = jwtKey;
}

if (jwtKey.Length < 32)
    throw new InvalidOperationException("SINOCHKU_JWT_KEY must be at least 32 characters.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "SinochkuGames",
            ValidateAudience = true,
            ValidAudience = "SinochkuGamesDesktop",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrWhiteSpace(token)
                    && context.HttpContext.Request.Path.StartsWithSegments("/hub/social"))
                    context.Token = token;

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var stamp = context.Principal?.FindFirst("sstamp")?.Value;
                if (string.IsNullOrWhiteSpace(userId) || stamp is null)
                {
                    context.Fail("Invalid session.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                var user = await userManager.FindByIdAsync(userId);
                if (user is null || !string.Equals(user.SecurityStamp ?? "", stamp, StringComparison.Ordinal))
                    context.Fail("Session has been revoked.");
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 64 * 1024;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("messages", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.Identity?.Name
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 90,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<SocialQueryService>();
builder.Services.AddScoped<MediaStorageService>();
builder.Services.AddScoped<RealtimeNotifier>();

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads"));

var app = builder.Build();

app.UseRateLimiter();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    service = "SinochkuGames.Social",
    status = "ok",
    utc = DateTimeOffset.UtcNow
}));

app.MapHub<SocialHub>("/hub/social");
app.MapSinochkuApi();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SocialDbContext>();
    await db.Database.EnsureCreatedAsync();

    var bootstrap = app.Configuration["SINOCHKU_BOOTSTRAP_INVITE"];
    if (!string.IsNullOrWhiteSpace(bootstrap)
        && !await db.InviteCodes.AnyAsync(x => x.Code == bootstrap))
    {
        db.InviteCodes.Add(new InviteCode
        {
            Code = bootstrap,
            MaxUses = 1
        });
        await db.SaveChangesAsync();
    }
}

await app.RunAsync();
