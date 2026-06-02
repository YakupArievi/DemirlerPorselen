using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Toptanci.Api.Authorization;
using Toptanci.Api.Filters;
using Toptanci.Api.Identity;
using Toptanci.Api.Middleware;
using Toptanci.Application;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Application.Common.Security;
using Toptanci.Infrastructure;
using Toptanci.Infrastructure.Persistence;
using Toptanci.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Yüklenen dosyalar için kök (depolama ile statik servis aynı klasörü kullanır)
var webRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRoot, "uploads"));
builder.Configuration["Storage:RootPath"] = webRoot;

// Katman servisleri
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// O anki kullanıcı (audit alanları için)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// JWT kimlik doğrulama
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, p => p.RequireRole(AppRoles.Admin));
    options.AddPolicy(Policies.PatronOrAdmin, p => p.RequireRole(AppRoles.Patron, AppRoles.Admin));
    options.AddPolicy(Policies.WarehouseStaff, p => p.RequireRole(AppRoles.Depocu, AppRoles.Patron, AppRoles.Admin));
});

// Web API
builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

// Migration + seed (varsayılan admin)
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
    await initializer.InitializeAsync();
    await initializer.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
