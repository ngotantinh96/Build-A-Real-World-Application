﻿using Globomantics.Domain;
using Globomantics.Infrastructure.Data;
using Globomantics.Infrastructure.Data.Repositories;
using Globomantics.Windows.Factories;
using Globomantics.Windows.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace Globomantics.Windows;

public partial class App : Application
{
    public static User CurrentUser { get; set; } = default!;
    public IServiceProvider ServiceProvider { get; init; } 
    public IConfiguration Configuration { get; init; }

    public App()
    {
        var builder = new ConfigurationBuilder();

        Configuration = builder.Build();

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddDbContext<GlobomanticsDbContext>(ServiceLifetime.Scoped);

        serviceCollection.AddSingleton<IRepository<TodoTask>, TodoTaskRepository>();
        serviceCollection.AddSingleton<IRepository<Feature>, FeatureRepository>();
        serviceCollection.AddSingleton<IRepository<Bug>, BugRepository>();
        serviceCollection.AddSingleton<IRepository<User>, UserRepository>();

        serviceCollection.AddTransient<TodoViewModelFactory>();
        serviceCollection.AddTransient<FeatureViewModel>();
        serviceCollection.AddTransient<BugViewModel>();
        serviceCollection.AddTransient<MainViewModel>();
        serviceCollection.AddTransient<MainWindow>();
        serviceCollection.AddTransient(_ => ServiceProvider!);
        
        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        var context = ServiceProvider.GetRequiredService<GlobomanticsDbContext>();
        await context.Database.MigrateAsync();

        var user = await context.Users.FirstOrDefaultAsync();

        if(user is null)
        {
            user = new Infrastructure.Data.Models.User
            {
                Name = "Otis Ngo"
            };

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
        }

        App.CurrentUser = DataToDomainMapping.MapUser(user);

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

        mainWindow?.Show();
    }
}
