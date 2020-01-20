using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using WorkerServiceDemo.Database;
using WorkerServiceDemo.Settings;

namespace WorkerServiceDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "..", 
                "Logs", 
                "Log.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(path, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting up the service");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "There was a problem starting the service");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)
                || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                host.UseSystemd();  // Microsoft.Extensions.Hosting.Systemd
                Log.Information("[+] Running on Linux [+]");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                host.UseWindowsService(); // Microsoft.Extensions.Hosting.WindowsServices
                Log.Information("[+] Running on Windows [+]");
            }

            host
               .ConfigureHostConfiguration(config => config.AddEnvironmentVariables())
               .ConfigureAppConfiguration((hostContext, config) =>
               {
                   config.SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                   config.AddJsonFile("appsettings.json", optional: true, false);
                   config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                   config.AddJsonFile($"appsettings.{Environment.MachineName}.json", optional: true);
                   config.AddEnvironmentVariables();
               })
               .ConfigureServices((hostContext, services) =>
               {
                   services.AddDbContext<WorkerContext>(options =>
                   {
                       if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                           || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)
                           || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                       {
                           //options.UseInMemoryDatabase("WorkerServiceDemo");
                           options.UseSqlite("Data Source=WorkerServiceDemo.db");
                       }

                       if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                       {
                           options.UseSqlServer(hostContext.Configuration.GetConnectionString("ConnectionString"));
                       }
                   }, ServiceLifetime.Singleton);

                   services.AddSingleton(provider =>
                       hostContext.Configuration.GetSection("AppSettings").Get<AppSetting>());

                   services.AddHostedService<Worker>();
               })
               .UseSerilog();

            return host;
        }
    }
}
