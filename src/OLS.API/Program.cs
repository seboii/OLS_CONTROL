using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using OLS.API.Serialization;
using Microsoft.IdentityModel.Tokens;
using OLS.Business.Services.Authentication;
using OLS.API.Filters;
using OLS.API.Localization;
using OLS.API.Middleware;
using OLS.API.Services;
using OLS.Business;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.DataAccess;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console());

// --------------------------------------------------------------------------
// JSON: Eloquent modelleri snake_case alan adlarıyla serileştiriyordu ve
// frontend buna bağımlı (work_type_id, created_at, load_number ...).
// Varsayılan camelCase kullanılırsa arayüz komple bozulur.
// --------------------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // Eloquent'in $hidden ve "yüklenmemiş ilişkiyi yazma" davranışları.
        options.JsonSerializerOptions.TypeInfoResolver =
            new DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    EloquentJsonModifiers.ApplyHiddenProperties,
                    EloquentJsonModifiers.SkipEmptyCollections,
                },
            };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDataAccess(builder.Configuration);
builder.Services.AddBusiness(builder.Configuration);

builder.Services.AddScoped<ITranslator, JsonTranslator>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
// Denetim kaydı bağlamı. HTTP isteği yoksa (arka plan senkronu) UserId null
// döner ve interceptor hiçbir şey yazmaz — bkz. HttpAuditContext.
builder.Services.AddScoped<OLS.DataAccess.Auditing.IAuditContext, OLS.API.Services.HttpAuditContext>();
builder.Services.AddSingleton<FileStorage>();
builder.Services.AddSingleton<IFileStorage>(sp => sp.GetRequiredService<FileStorage>());
builder.Services.AddHostedService<SiberSyncBackgroundService>();

// --------------------------------------------------------------------------
// Kimlik doğrulama: olsold Laravel Passport (OAuth2) kullanıyordu.
// Karşılığı JWT Bearer. İmza anahtarı yapılandırmadan gelir; kaynak koda
// gömülmez (olsold'da config/env.php içinde gömülü sırlar vardı).
// --------------------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key tanımlı değil. .env / User Secrets üzerinden verin.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key en az 32 bayt olmalı (HS256).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        options.Events = new JwtBearerEvents
        {
            // Passport'ta logout jetonu sunucu tarafında iptal ediyordu.
            // JWT durumsuz olduğu için iptal edilen jti'leri burada kontrol ediyoruz.
            OnTokenValidated = async ctx =>
            {
                var jti = ctx.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                if (jti is null)
                {
                    ctx.Fail("Jeton kimliği (jti) yok.");
                    return;
                }

                var authService = ctx.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                if (await authService.IsRevokedAsync(jti, ctx.HttpContext.RequestAborted))
                    ctx.Fail("Jeton iptal edilmiş.");
            },

            // Yetkisiz/yasak yanıtlarında da olsold'un zarfını koru.
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                if (ctx.Response.HasStarted) return;

                var translator = ctx.HttpContext.RequestServices.GetRequiredService<ITranslator>();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                    ApiResponse.Error(translator.Get("Yetkisiz Erişim"))));
            },
        };
    });

builder.Services.AddAuthorization();

// --------------------------------------------------------------------------
// CORS: olsold config/cors.php'de allowed_origins '*' idi ve tüm API'ye açıktı.
// Burada izinli origin listesi yapılandırmadan okunur.
// --------------------------------------------------------------------------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173", "http://localhost:8105"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// --------------------------------------------------------------------------
// Rate limiting: olsnew'de HİÇ yoktu (SEC-009). Giriş ve anonim destek formu
// kaba kuvvet/spam'e karşı sabit pencereli sınırlamayla korunuyor.
// --------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            JsonSerializer.Serialize(ApiResponse.Error("Çok fazla istek, lütfen daha sonra tekrar deneyin.")),
            cancellationToken);
    };

    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));

    options.AddPolicy("public-form", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

// --------------------------------------------------------------------------
// Health checks: olsnew'de yoktu. API + PostgreSQL zorunlu; Siber (legacy
// MSSQL) isteğe bağlı olduğu için ayrıca kontrol edilmiyor (degrade olması
// beklenen bir bağımlılık).
// --------------------------------------------------------------------------
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres tanımlı değil.");

builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnectionString, name: "postgresql", tags: ["ready"]);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<LocalizationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();

// Yüklenen evraklar. nginx `/storage/` isteklerini buraya yönlendiriyor;
// bu olmadan avatar ve yük ekleri 404 döner.
//
// olsold'da bu iş `public/storage` sembolik bağıyla web sunucusunda
// çözülüyordu — .NET'te açıkça tanımlanması gerekiyor.
var storageRoot = builder.Configuration["Storage:PublicPath"] ?? "/app/storage/app/public";
Directory.CreateDirectory(storageRoot);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storageRoot),
    RequestPath = "/storage",

    // Bilinmeyen uzantılar (eml, msg vb.) da indirilebilsin; tarayıcıda
    // çalıştırılmasın diye octet-stream olarak sunulur.
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream",
});

// Temiz kurulumda migration + seed tek komutla (docker compose up) çalışsın
// diye başlangıçta idempotent seed çalıştırılır. Üretim admin şifresi asla
// sabit değildir — bkz. OLS.Business.Seed.DbSeeder.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OLS.DataAccess.Context.OlsDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var defaultUserPassword = scope.ServiceProvider.GetRequiredService<IDefaultUserPassword>();
    var roleService = scope.ServiceProvider.GetRequiredService<OLS.Business.Services.Roles.IRoleService>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await db.Database.MigrateAsync();
    await OLS.Business.Seed.DbSeeder.SeedAsync(
        db, passwordHasher, defaultUserPassword, roleService, app.Configuration, app.Environment, seedLogger);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false, // yalnız process ayakta mı — bağımlılık kontrolü yok
});

app.Run();

public partial class Program;
