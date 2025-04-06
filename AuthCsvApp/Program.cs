
using AuthCsvApp.Managers;
using AuthCsvApp.Repositories;
using AuthCsvApp.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace AuthCsv // Thay đổi namespace từ AuthCsvApp sang ASMAPDP để đồng bộ
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Cấu hình session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Cấu hình distributed memory cache cho session
            builder.Services.AddDistributedMemoryCache();

            // Đăng ký các service
            builder.Services.AddSingleton<CsvRepository>();
            builder.Services.AddSingleton<AdminAccountService>();
            builder.Services.AddSingleton<AuthCsvApp.Services.AuthenticationService>(); // Cập nhật namespace cho AuthenticationService
            builder.Services.AddSingleton<AdminClassManager>(); // Đăng ký AdminClassManager
            builder.Services.AddSingleton<AdminClassService>(); // Đăng ký AdminClassService
            builder.Services.AddScoped<TeacherClassManager>(provider => // Đăng ký TeacherClassManager với Scoped lifetime
                new TeacherClassManager(
                    provider.GetRequiredService<CsvRepository>(),
                    provider.GetRequiredService<IHttpContextAccessor>().HttpContext?.Session.GetString("Username") ?? throw new InvalidOperationException("Username not found in session.")
                )
            );
            builder.Services.AddScoped<StudentClassManager>(provider => // Đăng ký StudentClassManager với Scoped lifetime
                new StudentClassManager(
                    provider.GetRequiredService<CsvRepository>(),
                    provider.GetRequiredService<IHttpContextAccessor>().HttpContext?.Session.GetString("Username") ?? throw new InvalidOperationException("Username not found in session.")
                )
            );

            // Cần thiết để truy cập HttpContext trong AuthenticationService, TeacherClassManager, và StudentClassManager
            builder.Services.AddHttpContextAccessor();

            // Cấu hình logging
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
            });

            // Cấu hình response compression để tối ưu hiệu suất
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Sử dụng response compression
            app.UseResponseCompression();

            app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            // Default route configuration
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}