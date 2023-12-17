using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Globomantics.Domain;
using Globomantics.Infrastructure.Data.Repositories;
using Globomantics.Windows.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Globomantics.Windows.ViewModels;

public class MainViewModel : ObservableObject, 
    IViewModel
{
    private string statusText = "Everything is OK!";
    private bool isLoading;
    private bool isInitialized;
    private readonly IRepository<User> userRepository;
    private readonly IRepository<TodoTask> todoRepository;
    private string selectedTodoType;

    public string StatusText 
    {
        get => statusText;
        set
        {
            statusText = value;

            OnPropertyChanged(nameof(StatusText));
        }
    }
    public bool IsLoading
    {
        get => isLoading;
        set
        {
            isLoading = value;

            OnPropertyChanged(nameof(IsLoading));
        }
    }


    public string SelectedTodoType 
    { 
        get => selectedTodoType; 
        set
        {
            selectedTodoType = value;
            OnPropertyChanged(nameof(SelectedTodoType));
        }
    }

    public ICommand ExportCommand { get; set; }
    public ICommand ImportCommand { get; set; }

    public Action<string>? ShowAlert { get; set; }
    public Action<string>? ShowError { get; set; }
    public Func<IEnumerable<string>>? ShowOpenFileDialog { get; set; }
    public Func<string>? ShowSaveFileDialog { get; set; }
    public Func<string, bool>? AskForConfirmation { get; set; }

    public ObservableCollection<Todo> Unfinished { get; set; } = new();
    public ObservableCollection<Todo> Completed { get; set; } = new();


    public MainViewModel(IRepository<User> userRepository, IRepository<TodoTask> todoRepository)
    {
        WeakReferenceMessenger.Default.Register<TodoSavedMessage>(this, (sender, message) =>
        {
            var item = message.Value;

            if(item.IsCompleted)
            {
                var existingItem = Unfinished.FirstOrDefault(x => x.Id == item.Id);

                if(existingItem is not null)
                {
                    Unfinished.Remove(existingItem);
                }
                
                ReplaceOrAdd(Completed, item);
            }
            else
            {
                var existingItem = Completed?.FirstOrDefault(x => x.Id == item.Id);

                if (existingItem is not null)
                {
                    Completed?.Remove(existingItem);
                }

                ReplaceOrAdd(Unfinished, item);
            }
        });

        WeakReferenceMessenger.Default.Register<TodoDeletedMessage>(this, (sender, message) =>
        {
            var item = message.Value;

            var unfinished = Unfinished.FirstOrDefault(x => x.Id == item.Id);

            if (unfinished is not null)
            {
                Unfinished.Remove(unfinished);
            }
        });
        this.userRepository = userRepository;
        this.todoRepository = todoRepository;
    }

    private void ReplaceOrAdd(ObservableCollection<Todo> collection, Todo item)
    {
        var existing = collection.FirstOrDefault(x => x.Id == item.Id);
        if (existing is not null)
        {
            var index = collection.IndexOf(existing);
            collection[index] = item;
        }
        else
        {
            collection.Add(item);
        }
    }

    public async Task InitializeAsync()
    {
        if (isInitialized) return;

        App.CurrentUser = await userRepository.FindByAsync("Otis Ngo");

        var items = await todoRepository.AllAsync();

        var itemsDue = items.Count(i => i.DueDate.ToLocalTime() > DateTimeOffset.Now);

        StatusText = $"Welcome {App.CurrentUser.Name}! " +
            $"You have {itemsDue} items passed due date.";

        foreach (var item in items.Where(item => !item.IsDeleted))
        {
            if (item.IsCompleted)
            {
                Completed.Add(item);
            }
            else
            {
                Unfinished.Add(item);
            }
        }

        isInitialized = true;
    }
}