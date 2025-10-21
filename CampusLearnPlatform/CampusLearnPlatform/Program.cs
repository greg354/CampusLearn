using CampusLearnPlatform.Data;
using CampusLearnPlatform.Services;
using Microsoft.EntityFrameworkCore;

namespace CampusLearnPlatform
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<CampusLearnDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDbContext<MinimalRegistrationDBContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
            {
                // Add custom model binding and validation
                options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
                    _ => "This field is required.");
            });

            builder.Services.AddScoped<IMessageService, MessageService>();

            // Add session support (for temporary data)
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add memory cache
            builder.Services.AddMemoryCache();

            // Add HTTP client for external APIs
            builder.Services.AddHttpClient();

            // Register Gemini Service
            builder.Services.AddScoped<IGeminiService, GeminiService>();

            // Add logging
            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

            // Configure application settings
            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();
            
            // FIXED: Change default route to Account/Login instead of Home/Index
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");
            app.Run();
        }
    }
}