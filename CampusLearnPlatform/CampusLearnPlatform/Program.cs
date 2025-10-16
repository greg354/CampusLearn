using CampusLearnPlatform.Data;
 Profile_
using Microsoft.AspNetCore.CookiePolicy;
=======
using CampusLearnPlatform.Services;
 main
using Microsoft.EntityFrameworkCore;

namespace CampusLearnPlatform
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ========= DB (PostgreSQL via EF Core) =========
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<CampusLearnDbContext>(o => o.UseNpgsql(connectionString));
            builder.Services.AddDbContext<MinimalRegistrationDBContext>(o => o.UseNpgsql(connectionString));

            // ========= MVC + Views =========
            builder.Services.AddControllersWithViews(options =>
            {
                options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "This field is required.");
            });

            // ========= Session =========
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // ========= Utilities =========
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient();
            Profile_
=======

            // Register Gemini Service
            builder.Services.AddScoped<IGeminiService, GeminiService>();

            // Add logging
            main
            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

            // ========= Cookie policy (for safer defaults) =========
            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = _ => true;
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
            });

            var app = builder.Build();

            // ========= Pipeline =========
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

            app.UseCookiePolicy();
            app.UseSession();          // <-- ensure session middleware is enabled

            // Default route goes to Account/Login
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}