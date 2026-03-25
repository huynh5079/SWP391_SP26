using System.IO;
using System.Windows;
using BusinessLogic.Options;
using BusinessLogic.Service.Auth;
using BusinessLogic.Service.Dashboard;
using BusinessLogic.Service.Event;
using BusinessLogic.Service.System;
using BusinessLogic.Service.ValidationData.Event;
using BusinessLogic.Storage;
using AEMS_WPF.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Configuration
            services.AddSingleton<IConfiguration>(Configuration);

            // Database
            services.AddDbContext<AEMSContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Repositories
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IChatRepository, ChatRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISystemErrorLogService, SystemErrorLogService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<ISignalRNotifier, DummySignalRNotifier>();
            
            // Validation
            services.AddScoped<IEventValidator, EventValidator>();

            // Storage
            services.Configure<CloudinaryOptions>(Configuration.GetSection("Cloudinary"));
            services.Configure<StorageOptions>(Configuration.GetSection("Storage"));
            services.AddSingleton<StoragePathResolver>();
            services.AddScoped<IFileStorageService, CloudinaryStorageService>();
            
            // Add other necessary services here as needed for Staff features

            // Views
            services.AddTransient<Views.Auth.LoginWindow>();
            services.AddTransient<MainWindow>(); // Using MainWindow as the Shell
        }
    }
}
