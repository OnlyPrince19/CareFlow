using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CareFlow.Data;
using CareFlow.Services;
using CareFlow.ViewModels;
using CareFlow.Views;

namespace CareFlow;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Database context configuration with SQLite
        services.AddDbContext<CareFlowDbContext>(options =>
        {
            options.UseSqlite("Data Source=careflow.db");
        });

        // Application Services / Business Logic Layer
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDiagnosticCatalogService, DiagnosticCatalogService>();
        services.AddScoped<IBookingService, BookingService>();

        // Presentation Layer ViewModels
        services.AddTransient<NewBookingViewModel>();
        services.AddTransient<AvailabilityViewModel>();
        services.AddTransient<BookingHistoryViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();

        Services = services.BuildServiceProvider();

        // Reproducible database initialization & seeding using EF Core
        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CareFlowDbContext>();
            await DbInitializer.InitializeAsync(dbContext);
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = Services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm
            };

            // Asynchronously initialize data in ViewModels
            _ = mainVm.InitializeAllAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
