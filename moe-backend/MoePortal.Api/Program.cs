using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MoePortal.Api.Middleware;
using MoePortal.Core.Domain.Entities;
using MoePortal.Core.Domain.Enums;
using MoePortal.Core.Interfaces;
using MoePortal.Infrastructure.Data;
using MoePortal.Infrastructure.Services;
using Serilog;
using Microsoft.SemanticKernel;
using Microsoft.Identity.Web;
using Hangfire;
using Hangfire.MemoryStorage;

var builder = WebApplication.CreateBuilder(args);

// ── Load .env.agent (two levels up from MoePortal.Api working dir) ──────────
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env.agent");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
    builder.Configuration.AddEnvironmentVariables();
}

// ── Serilog ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/moeportal-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

// ── Database ─────────────────────────────────────────────────────────────────
var sqlitePath = builder.Configuration["Database:SqlitePath"] ?? "Data/moeportal.db";
var dbDir = Path.GetDirectoryName(sqlitePath);
if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
    Directory.CreateDirectory(dbDir);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={sqlitePath}"));

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddScoped<ICpfPayoutService, CpfPayoutService>();
builder.Services.AddScoped<IEligibilityService, EligibilityService>();
builder.Services.AddScoped<IAiAssistantService, AgenticFasService>();

builder.Services.AddHttpClient<IPaymentProviderService, HitPayService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["HitPay:BaseUrl"] ?? "https://api.sandbox.hit-pay.com/v1/");
    var apiKey = builder.Configuration["HITPAY_API_KEY"] ?? builder.Configuration["HitPay:ApiKey"] ?? string.Empty;
    if (!string.IsNullOrEmpty(apiKey))
        client.DefaultRequestHeaders.Add("X-BUSINESS-API-KEY", apiKey);
    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
});

// ── Semantic Kernel (GitHub Models via OpenAI-compatible endpoint) ────────────
#pragma warning disable SKEXP0070
var openAiClient = new OpenAI.OpenAIClient(
    new System.ClientModel.ApiKeyCredential(builder.Configuration["OPENAI_API_KEY"] ?? throw new InvalidOperationException("OPENAI_API_KEY missing")),
    new OpenAI.OpenAIClientOptions { Endpoint = new Uri(builder.Configuration["OPENAI_BASE_URL"] ?? "https://models.github.ai/inference") }
);

builder.Services.AddKernel()
    .AddOpenAIChatCompletion(
        modelId: builder.Configuration["OPENAI_MODEL_NAME"] ?? "openai/gpt-4o",
        openAIClient: openAiClient
    );
#pragma warning restore SKEXP0070

// ── Singpass RSA Key ─────────────────────────────────────────────────────────
var keyPath = builder.Configuration["Singpass:PrivateKeyPath"] ?? "../keys/singpass_private.pem";
var fullKeyPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), keyPath));

RsaSecurityKey? rsaKey = null;
if (File.Exists(fullKeyPath))
{
    var rsa = RSA.Create();
    rsa.ImportFromPem(File.ReadAllText(fullKeyPath));
    rsaKey = new RsaSecurityKey(rsa);
}

SecurityKey issuerKey = rsaKey != null
    ? (SecurityKey)rsaKey
    : new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("this-is-a-very-long-fallback-symmetric-security-key-1234567890"));

// ── Authentication ────────────────────────────────────────────────────────────
// Primary scheme: Entra ID (for admin portal)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var existingOnTokenValidated = options.Events?.OnTokenValidated;
    options.Events ??= new JwtBearerEvents();
    options.Events.OnTokenValidated = async context =>
    {
        if (existingOnTokenValidated != null)
        {
            await existingOnTokenValidated(context);
        }
        
        var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
        if (identity != null)
        {
            var hasRole = identity.HasClaim(c => c.Type == "roles" || c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role");
            if (!hasRole)
            {
                identity.AddClaim(new System.Security.Claims.Claim("roles", "HQ_ADMIN"));
            }
        }
    };
});

// Additional scheme: Singpass mock JWT (for citizen e-Service portal)
builder.Services.AddAuthentication()
    .AddJwtBearer("Singpass", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Singpass:Authority"],
            ValidAudience            = builder.Configuration["AllowedOrigins:Frontend"] ?? "http://localhost:3000",
            IssuerSigningKey         = issuerKey,
            RoleClaimType            = "roles",
            NameClaimType            = "name"
        };
    });

// ── Authorization Policies ────────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("HqAdminOnly", p => 
    {
        p.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        p.AuthenticationSchemes.Add("Singpass");
        p.RequireAssertion(context => context.User.HasClaim(c => (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "roles" || c.Type == "role") && c.Value == "HQ_ADMIN"));
    });
    options.AddPolicy("AnyAdmin", p => 
    {
        p.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        p.AuthenticationSchemes.Add("Singpass");
        p.RequireAssertion(context => context.User.HasClaim(c => (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "roles" || c.Type == "role") && (c.Value == "HQ_ADMIN" || c.Value == "SCHOOL_ADMIN")));
    });
});

// ── CORS (restricted to frontend origin — never wildcard) ─────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(builder.Configuration["AllowedOrigins:Frontend"] ?? "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("SingpassPolicy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ── Hangfire Background Jobs ──────────────────────────────────────────────────
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage());
builder.Services.AddHangfireServer();

var app = builder.Build();

// ── Middleware Pipeline (ORDER IS CRITICAL) ───────────────────────────────────
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();    // 1. Assign correlation ID first
app.UseMiddleware<ExceptionHandlingMiddleware>(); // 2. Catch all exceptions next
app.UseRateLimiter();                            // 3. Rate limiting early
app.UseCors("FrontendPolicy");                   // 4. CORS before auth
// Note: HTTPS redirect removed — running on HTTP locally (no admin access for certs)
app.UseAuthentication();                         // 4. Auth before authorization
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new MoePortal.Api.Middleware.HangfireAuthorizationFilter() } // Allow local access
});

app.MapControllers();

// ── Health Check (anonymous — no auth required) ───────────────────────────────
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status  = report.Status.ToString(),
            entries = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
        });
        await context.Response.WriteAsync(result);
    }
}).AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ── Database Seed ─────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    SeedDatabase(db);
}

app.Run();

// ── Seed Method (Phase B) ─────────────────────────────────────────────────────
static void SeedDatabase(AppDbContext db)
{
    // ── Citizen 1: Alpha Tester — Active, age ~21, has a pending invoice ──────
    if (!db.CitizenRecords.Any(c => c.Nric == "S1234567A"))
    {
        var c1 = new CitizenRecord
        {
            Id                     = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Nric                   = "S1234567A",
            FullName               = "Alpha Tester",
            DateOfBirth            = new DateOnly(2005, 1, 1),
            EducationAccount       = new EducationAccount
            {
                Status = AccountStatus.Active,
                Balance = 1500.50m,
                OpenedDate = new DateOnly(2021, 1, 1)
            }
        };
        db.CitizenRecords.Add(c1);

        db.Invoices.Add(new Invoice
        {
            Id            = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            CitizenRecord = c1,
            InvoiceNumber = "MATH-101",
            TotalAmount   = 500.00m,
            Status        = InvoiceStatus.Pending
        });

        db.Invoices.Add(new Invoice
        {
            Id            = Guid.Parse("20000000-0000-0000-0000-000000000002"),
            CitizenRecord = c1,
            InvoiceNumber = "ENG-201",
            TotalAmount   = 1200.00m,
            Status        = InvoiceStatus.Pending
        });
    }

    // ── Citizen 2: Young Citizen — age ~14, NotYetCreated (no account yet) ────
    if (!db.CitizenRecords.Any(c => c.Nric == "S2345678B"))
    {
        db.CitizenRecords.Add(new CitizenRecord
        {
            Id                     = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Nric                   = "S2345678B",
            FullName               = "Young Citizen",
            DateOfBirth            = new DateOnly(2012, 6, 14), // age ~14
            EducationAccount       = new EducationAccount { Status = AccountStatus.NotYetCreated }
        });
    }

    // ── Citizen 3: Turning Sixteen — should open account on evaluate ──────────
    if (!db.CitizenRecords.Any(c => c.Nric == "S3456789C"))
    {
        db.CitizenRecords.Add(new CitizenRecord
        {
            Id                     = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            Nric                   = "S3456789C",
            FullName               = "Turning Sixteen",
            DateOfBirth            = new DateOnly(2010, 6, 14), // age ~16
            EducationAccount       = new EducationAccount { Status = AccountStatus.NotYetCreated }
        });
    }

    // ── Citizen 4: Senior Citizen — age ~30, should auto-close on evaluate ────
    if (!db.CitizenRecords.Any(c => c.Nric == "S4567890D"))
    {
        db.CitizenRecords.Add(new CitizenRecord
        {
            Id                     = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            Nric                   = "S4567890D",
            FullName               = "Senior Citizen",
            DateOfBirth            = new DateOnly(1996, 1, 1), // age ~30
            EducationAccount       = new EducationAccount
            {
                Status = AccountStatus.Active,
                Balance = 200.00m,
                OpenedDate = new DateOnly(2012, 1, 1)
            }
        });
    }

    // ── Citizen 5: Deceased Citizen — should close immediately on evaluate ─────
    if (!db.CitizenRecords.Any(c => c.Nric == "S5678901E"))
    {
        db.CitizenRecords.Add(new CitizenRecord
        {
            Id                     = Guid.Parse("10000000-0000-0000-0000-000000000005"),
            Nric                   = "S5678901E",
            FullName               = "Deceased Citizen",
            DateOfBirth            = new DateOnly(2000, 3, 15),
            DateOfDeath            = new DateOnly(2026, 6, 1),
            EducationAccount       = new EducationAccount
            {
                Status = AccountStatus.Active,
                OpenedDate = new DateOnly(2016, 3, 15)
            }
        });
    }

    db.SaveChanges();
}
