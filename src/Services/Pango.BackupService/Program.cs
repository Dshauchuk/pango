using Pango.Application.Common.Interfaces.Services;
using Pango.BackupService;
using Pango.Infrastructure.Services;
using System.Runtime.InteropServices;

/// <summary>
/// Entry point for the background Backup Service.
/// Hides the console window on Windows OS upon startup.
/// </summary>
try
{
    if (OperatingSystem.IsWindows())
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, 0);
    }

    IHost host = Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IBackupManager, BackupManager>();
            services.AddHostedService<Worker>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Critical failure in Backup Service: {ex.Message}");
}

[DllImport("kernel32.dll")]
static extern IntPtr GetConsoleWindow();

[DllImport("user32.dll")]
static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);