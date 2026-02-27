using AEMS_Solution.Configurations;
using BusinessLogic.Service.Auth;
using BusinessLogic.Service.System;
using BusinessLogic.Service.User;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using ISystemErrorLogService = BusinessLogic.Service.System.ISystemErrorLogService;
using SystemErrorLogService = BusinessLogic.Service.System.SystemErrorLogService;
using BusinessLogic.Service.ValidationDataforEvent;
using BusinessLogic.Service.Event;
using BusinessLogic.Service.Organizer;
using BusinessLogic.Service.Dashboard;
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
builder.Services.AddScoped<IUserService, BusinessLogic.Service.User.UserService>();
// Register refactored services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IDropdownService, DropdownService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
// Event waitlist service
builder.Services.AddScoped<IEventWaitlistService, EventWaitlistService>();
// keep facade for backward compatibility
builder.Services.AddScoped<IOrganizerService, BusinessLogic.Service.Organizer.OrganizerService>();
builder.Services.AddScoped<IEventValidator, BusinessLogic.Service.ValidationDataforEvent.EventValidator>();
// Storage Services
builder.Services.Configure<BusinessLogic.Options.CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.Configure<BusinessLogic.Options.StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<BusinessLogic.Storage.StoragePathResolver>();
builder.Services.AddScoped<BusinessLogic.Storage.IFileStorageService, BusinessLogic.Storage.CloudinaryStorageService>();

// TODO: Register RedisService when implemented

// HttpClient (External Services like PayOS)
builder.Services.AddHttpClient();

// SignalR
builder.Services.AddSignalR(); 

// Authentication (Cookie)
var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// Register Google only when configuration is present to avoid Options validation errors at runtime
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.Events.OnRemoteFailure = context =>
        {
            // Handle "Access denied" (User clicked Cancel) or other remote errors
            context.Response.Redirect("/Auth/Login?error=GoogleLoginFailed");
            context.HandleResponse(); // Suppress the exception
            return Task.CompletedTask;
        };
    });
}

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
        
            // Insert Organizer role if not exists
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT 1 FROM Role WHERE RoleName = 'Organizer')
            BEGIN
                INSERT INTO Role (Id, RoleName, CreatedAt, UpdatedAt, DeletedAt)
                VALUES (NEWID(), 'Organizer', {0}, {0}, NULL)
            END
        ", now);

        // Insert Approver role if not exists
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT 1 FROM Role WHERE RoleName = 'Approver')
            BEGIN
                INSERT INTO Role (Id, RoleName, CreatedAt, UpdatedAt, DeletedAt)
                VALUES (NEWID(), 'Approver', {0}, {0}, NULL)
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

        // Seed StaffProfiles cho các user Organizer chưa có StaffProfile
        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO StaffProfile (Id, UserId, StaffCode, DepartmentId, [Position], CreatedAt, UpdatedAt, DeletedAt)
            SELECT NEWID(), u.Id, 'ORG-' + SUBSTRING(u.Id, 1, 8), NULL, 'Event Organizer', {0}, {0}, NULL
            FROM [User] u
            INNER JOIN Role r ON u.RoleId = r.Id
            WHERE r.RoleName = 'Organizer'
            AND NOT EXISTS (
                SELECT 1 FROM StaffProfile sp WHERE sp.UserId = u.Id
            )
        ", now);

        logger.LogInformation("StaffProfiles seeded successfully for Organizer users.");
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
