using Serilog;
using Toptanci.Api.Identity;
using Toptanci.Api.Middleware;
using Toptanci.Application;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Katman servisleri
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// O anki kullanıcı (audit alanları için)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Web API
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
