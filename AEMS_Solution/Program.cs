using AEMS_Solution.Configurations;
using BusinessLogic.Service;
using BusinessLogic.Service.Interface;
using DataAccess.Entities;
using DataAccess.Repositories;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

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
// TODO: Register EmailService, RedisService when implemented

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
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
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
// 2. Middleware Pipeline
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
// Add Global Exception Handler for all environments or just Prod? 
// User requested it generally.
app.UseMiddleware<AEMS_Solution.Middlewares.GlobalExceptionHandler>();

app.UseHttpsRedirection();
app.UseStaticFiles();

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
