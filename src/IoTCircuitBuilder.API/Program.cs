using IoTCircuitBuilder.Application.Interfaces;
using IoTCircuitBuilder.Application.Services;
using IoTCircuitBuilder.Core.Algorithms;
using IoTCircuitBuilder.Core.Interfaces;
using IoTCircuitBuilder.Core.Validation;
using IoTCircuitBuilder.Infrastructure.Data;
using IoTCircuitBuilder.Infrastructure.Repositories;
using IoTCircuitBuilder.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

// ─── Serilog Bootstrap ─────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/iot-circuit-builder-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ─── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Caching ───────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ─── Repositories ──────────────────────────────────────────────
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();

// ─── Core ──────────────────────────────────────────────────────
builder.Services.AddScoped<IConstraintSolver, ConstraintSolver>();
builder.Services.AddScoped<PinMappingValidator>();

// ─── Infrastructure Services ───────────────────────────────────
builder.Services.AddHttpClient<ILLMService, LLMService>();
builder.Services.AddScoped<ICodeGenerator, CodeGeneratorService>();

// ─── Application Services ──────────────────────────────────────
builder.Services.AddScoped<ICircuitGenerationService, CircuitGenerationService>();

// ─── API ───────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "IoT Circuit Builder API", Version = "v1" });
});

// ─── CORS (for frontend) ──────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ─── Middleware ─────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.MapControllers();

// ─── Auto-migrate in development ───────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

Log.Information("IoT Circuit Builder API started");
app.Run();
