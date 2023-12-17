using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Globomantics.Domain;
using Globomantics.Infrastructure.Data.Repositories;
using Globomantics.Windows.Json;
using Globomantics.Windows.Messages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
    private string searchText = "";

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


    public string SearchText 
    { 
        get => searchText; 
        set
        {
            searchText = value;
            OnPropertyChanged(nameof(SearchText));
        }
    }

    public ICommand ExportCommand { get; set; }
    public ICommand ImportCommand { get; set; }
    public ICommand SearchCommand { get; set; }

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

        ExportCommand = new RelayCommand(async () =>
        {
            await ExportAsync();
        });

        ImportCommand = new RelayCommand(async () =>
        {
            await ImportAsync();
        });

        SearchCommand = new RelayCommand(async () =>
        {
            var todo = await todoRepository.AllAsync();
            var query = todo.AsQueryable().Where(x => !x.IsCompleted && !x.IsDeleted);
            if (!string.IsNullOrWhiteSpace(searchText) && !searchText.Equals("*", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            Unfinished.Clear();

            foreach (var item in query)
            {
                Unfinished.Add(item);
            }
        });
    }

    private async Task ImportAsync()
    {
        var fileNames = ShowOpenFileDialog?.Invoke();

        if (fileNames is null || !fileNames.Any()) 
        {
            return;
        }

        var fileName = fileNames.ElementAt(0);

        if(string.IsNullOrWhiteSpace(fileName))
        {
            ShowError?.Invoke("Please select a file to import");
        }

        var json = await File.ReadAllTextAsync(fileName);

        var items = JsonConvert.DeserializeObject<IEnumerable<TodoTask>>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            SerializationBinder = new SerializationBinder()
        });

        if(items is null|| !items.Any())
        {
            return;
        }

        foreach (var item in items)
        {
            await todoRepository.AddAsync(item);

            if(item.IsCompleted)
            {
                Completed.Add(item);
            }
            else if(!item.IsDeleted)
            {
                Unfinished.Add(item);
            }
        }

        await todoRepository.SaveChangesAsync();

        IsLoading = true;

        ShowAlert?.Invoke("Data Imported");
        IsLoading = false;
    }

    private async Task ExportAsync()
    {
        var fileName = ShowSaveFileDialog?.Invoke();
        IsLoading = true;
        var items = await todoRepository.AllAsync();
        var json = JsonConvert.SerializeObject(items, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            SerializationBinder = new SerializationBinder()
        });

        await File.WriteAllTextAsync(fileName!, json);
        IsLoading = false;
        ShowAlert?.Invoke("Data Exported");
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