using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using AEMS_Solution.BaseAction_ValidforController_.Approver.Agenda;
using AEMS_Solution.BaseAction_ValidforController_.Organizer.Event;
using AEMS_Solution.BaseAction_ValidforController_.Organizer.Event.InterfaceEvent;
using AEMS_Solution.Configurations;
using BusinessLogic.Hubs;
using BusinessLogic.Service.Chat.ChatforUser;
using BusinessLogic.Service.Approval;
using BusinessLogic.Service.Auth;
using BusinessLogic.Service.UserActivities;
using BusinessLogic.Service.Chat.ChatforUser.ChatPerMission;
using BusinessLogic.Service.Dashboard;
using BusinessLogic.Service.Event;
using BusinessLogic.Service.Event.EventDepartment;
using BusinessLogic.Service.Event.Semester;
using BusinessLogic.Service.Event.Sub_Service.Location;
using BusinessLogic.Service.Event.Sub_Service.Quiz;
using BusinessLogic.Service.Event.Sub_Service.Quiz.ForAll;
using BusinessLogic.Service.Event.Sub_Service.Semester;
using BusinessLogic.Service.Event.Sub_Service.Feedback;
using BusinessLogic.Service.Event.Sub_Service.Ticket;
using BusinessLogic.Service.Event.Sub_Service.Topic;
using BusinessLogic.Service.Organizer;
using BusinessLogic.Service.Organizer.BudgetProposal;
using BusinessLogic.Service.Organizer.CheckIn;
using BusinessLogic.Service.Student;
using BusinessLogic.Service.System;
using BusinessLogic.Service.User;
using BusinessLogic.Service.ValiDateRole.ValiDateforAdmin.LockAndUnlockLimit;
using BusinessLogic.Service.ValiDateRole.ValidateforOrganizer;
using BusinessLogic.Service.ValidationData.Event;
using BusinessLogic.Service.ValidationData.Loction;
using BusinessLogic.Service.ValidationData.Quiz;
using BusinessLogic.Service.ValidationData.Ticket;
using BusinessLogic.Service.ValidationData.Topic;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ISystemErrorLogService = BusinessLogic.Service.System.ISystemErrorLogService;
using SystemErrorLogService = BusinessLogic.Service.System.SystemErrorLogService;
var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Service Registration (Dependency Injection)
// ==========================================

// Database Context
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AEMSContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Generic Repository
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

// UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services (Business Logic)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISystemErrorLogService, SystemErrorLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, BusinessLogic.Service.User.UserService>();
builder.Services.AddScoped<IUserActivityLogService, UserActivityLogService>();
builder.Services.AddScoped<BusinessLogic.Service.System.ISignalRNotifier, BusinessLogic.Service.System.SignalRNotifier>();
builder.Services.AddSingleton<IChatPresenceTracker, ChatPresenceTracker>();
builder.Services.AddHostedService<BusinessLogic.Service.Admin.UserLockExpirationService>();
builder.Services.AddHostedService<BusinessLogic.Service.Event.EventStatusExpirationService>();
builder.Services.AddScoped<IChatPermissionService, ChatPermissionService>();
builder.Services.AddScoped<IChatUserService, ChatUserService>();

// RAG/Chatbot Services
builder.Services.AddScoped<BusinessLogic.Service.Chat.IChatbotService, BusinessLogic.Service.Chat.ChatbotService>();
// Register refactored services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IDropdownService, DropdownService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
// Event waitlist service
builder.Services.AddScoped<IEventWaitlistService, EventWaitlistService>();
// keep facade for backward compatibility
builder.Services.AddScoped<IOrganizerService, BusinessLogic.Service.Organizer.OrganizerService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IEventValidator, EventValidator>();
// SemesterService
builder.Services.AddScoped<BusinessLogic.Service.Event.Sub_Service.Semester.ISemesterService, BusinessLogic.Service.Event.Semester.SemesterService>();
// Quiz services
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IQuizServiceForAll, QuizSrviceForAll>();
builder.Services.AddScoped<IQuizValidator, QuizValidator>();
builder.Services.AddScoped<ILockAndUnlockLimitValidator, LockAndUnlockLimitValidator>();
builder.Services.AddScoped<ILocationValidator, LocationValidator>();
builder.Services.AddScoped<ITicketValidator, TicketValidator>();
builder.Services.AddScoped<ITopicValidator, TopicValidator>();
// Approver services (query + command)
builder.Services.AddScoped<IApproverQueryService, ApproverService>();
builder.Services.AddScoped<IApproverCommandService, ApproverService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
// Student services
builder.Services.AddScoped<IStudentEventService, StudentEventService>();
builder.Services.AddScoped<IFeedBackService, FeedbackService>();
// Organizer CheckIn service
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<IOrganizerValidator, OrganizerValidator>();
builder.Services.AddScoped<IEventAgendaAction, EventAgendaAction>();
builder.Services.AddScoped<IApproverEventAgendaAction, ApproverEventAgendaAction>();
// Storage Services
builder.Services.Configure<BusinessLogic.Options.CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.Configure<BusinessLogic.Options.StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<BusinessLogic.Storage.StoragePathResolver>();
builder.Services.AddScoped<BusinessLogic.Storage.IFileStorageService, BusinessLogic.Storage.CloudinaryStorageService>();

// TODO: Register RedisService when implemented
// AutoMapper - register all loaded assemblies
builder.Services.AddAutoMapper(cfg => { }, AppDomain.CurrentDomain.GetAssemblies());
// HttpClient (External Services like PayOS, Deep Learning)
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<BusinessLogic.Service.Event.Sub_Service.Feedback.DeepLearningService.DLService>();

// SignalR
builder.Services.AddSignalR();

//Budget
builder.Services.AddScoped<IBudgetProposalService, BudgetProposalService>();

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
        // Allow case-insensitive property names (camelCase to PascalCase mapping)
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

var app = builder.Build();

// ==========================================
// 2. Middleware Pipeline
// ==========================================

// Fix for Azure SSL Termination (Google Auth Redirect URI mismatch)
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

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
app.MapHub<NotificationHub>("/hub/v1/notification");
app.MapHub<ChatHub>("/hub/v1/chat");

// MVC Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Postman Setup Info
// Environment Variable: base_url = https://localhost:7149
// API Endpoints: {{base_url}}/api/v1/[controller]
// MVC Endpoints: {{base_url}}/[controller]/[action]

app.Run();
