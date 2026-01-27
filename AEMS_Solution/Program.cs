using AEMS_Solution.Configurations;
using BusinessLogic.Service;
using BusinessLogic.Service.Interface;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using ISystemErrorLogService = BusinessLogic.Service.Interface.ISystemErrorLogService;
using SystemErrorLogService = BusinessLogic.Service.SystemErrorLogService;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Service Registration (Dependency Injection)
// ==========================================

// Database Context
builder.Services.AddDbContext<AEMSContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Generic Repository
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services (Business Logic)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISystemErrorLogService, SystemErrorLogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
// TODO: Register RedisService when implemented

// HttpClient (External Services like PayOS)
builder.Services.AddHttpClient();

// SignalR
builder.Services.AddSignalR(); 

// Authentication (Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    })
    .AddGoogle(options => 
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.CallbackPath = "/signin-google";
        options.Events.OnRemoteFailure = context =>
        {
            // Handle "Access denied" (User clicked Cancel) or other remote errors
            context.Response.Redirect("/Auth/Login?error=GoogleLoginFailed");
            context.HandleResponse(); // Suppress the exception
            return Task.CompletedTask;
        };
    });

// Controllers with Views + JSON Options
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Handle DateOnly
        options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        // Handle Enum as String
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Ignore Cycles
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// ==========================================
// 1.5. Seed Initial Data (Roles)
// ==========================================
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AEMSContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Ensure database is created and migrated
        await context.Database.MigrateAsync();
        
        // Seed Roles using raw SQL to avoid EF tracking issues
        var now = DataAccess.Helper.DateTimeHelper.GetVietnamTime();
        
        // Insert Admin role if not exists
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT 1 FROM Role WHERE RoleName = 'Admin')
            BEGIN
                INSERT INTO Role (Id, RoleName, CreatedAt, UpdatedAt, DeletedAt)
                VALUES (NEWID(), 'Admin', {0}, {0}, NULL)
            END
        ", now);
        
        // Insert Staff role if not exists
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT 1 FROM Role WHERE RoleName = 'Staff')
            BEGIN
                INSERT INTO Role (Id, RoleName, CreatedAt, UpdatedAt, DeletedAt)
                VALUES (NEWID(), 'Staff', {0}, {0}, NULL)
            END
        ", now);
        
        // Insert Student role if not exists
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT 1 FROM Role WHERE RoleName = 'Student')
            BEGIN
                INSERT INTO Role (Id, RoleName, CreatedAt, UpdatedAt, DeletedAt)
                VALUES (NEWID(), 'Student', {0}, {0}, NULL)
            END
        ", now);
        
        logger.LogInformation("Roles seeded successfully.");
    }
}
catch (Exception ex)
{
    // Log error but don't stop app startup
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Error seeding Roles: {Message}. Inner Exception: {InnerException}", 
        ex.Message, ex.InnerException?.Message);
}

// ==========================================
// 2. Middleware Pipeline
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Global Exception Handler - Place after StaticFiles but before Routing
// This ensures it catches all exceptions from controllers/endpoints
app.UseMiddleware<AEMS_Solution.Middlewares.GlobalExceptionHandler>();

// Routing must be before Auth
app.UseRouting();

// CORS (Configure if front-end is separate, but strict for now)
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// Authentication -> Authorization
app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// 3. Endpoint Mapping (Hybrid Strategy)
// ==========================================

// SignalR Hubs
// app.MapHub<NotificationHub>("/hub/v1/notification");
// app.MapHub<ChatHub>("/hub/v1/chat");

// MVC Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Postman Setup Info
// Environment Variable: base_url = https://localhost:7149
// API Endpoints: {{base_url}}/api/v1/[controller]
// MVC Endpoints: {{base_url}}/[controller]/[action]

app.Run();
