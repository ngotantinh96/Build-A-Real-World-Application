using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Globomantics.Domain;
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

    public ICommand ExportCommand { get; set; }
    public ICommand ImportCommand { get; set; }

    public Action<string>? ShowAlert { get; set; }
    public Action<string>? ShowError { get; set; }
    public Func<IEnumerable<string>>? ShowOpenFileDialog { get; set; }
    public Func<string>? ShowSaveFileDialog { get; set; }
    public Func<string, bool>? AskForConfirmation { get; set; }

    public ObservableCollection<Todo> Unfinished { get; set; } = new();
    public ObservableCollection<Todo> Completed { get; set; } = new();

    public MainViewModel()
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

        isInitialized = true;
    }
}