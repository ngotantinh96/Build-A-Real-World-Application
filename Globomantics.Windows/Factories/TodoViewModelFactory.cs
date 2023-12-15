using Globomantics.Domain;
using Globomantics.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Globomantics.Windows.Factories;
public class TodoViewModelFactory
{
    public static IEnumerable<string> TodoTypes = new[]
    {
        nameof(Feature),
        nameof(Bug)
    };
    private readonly IServiceProvider serviceProvider;

    public TodoViewModelFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public ITodoViewModel CreateViewModel(
            string type,
            IEnumerable<Todo> parentTasks,
            Todo? model = default
        )
    {
        ITodoViewModel? viewModel = type switch
        {
            nameof(Feature) => serviceProvider.GetService<FeatureViewModel>(),
            nameof(Bug) => serviceProvider.GetService<BugViewModel>(),
            _ => throw new NotImplementedException()
        };

        ArgumentNullException.ThrowIfNull(viewModel);

        if (parentTasks is not null && parentTasks.Any())
        {
            viewModel.AvailableParentTasks = parentTasks;
        }

        if (model is not null)
        {
            viewModel.UpdateModel(model);
        }

        return viewModel;
    }
}
