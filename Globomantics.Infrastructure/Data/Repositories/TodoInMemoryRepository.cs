﻿using Globomantics.Domain;
using System.Collections.Concurrent;

namespace Globomantics.Infrastructure.Data.Repositories;

public class TodoInMemoryRepository<T> : IRepository<T> where T : Todo
{
    private ConcurrentDictionary<Guid, T> Items { get; } = new();
    public Task AddAsync(T item)
    {
        Items.TryAdd(item.Id, item);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<T>> AllAsync()
    {
        var items = Items.Values.ToList();
        return Task.FromResult<IEnumerable<T>>(items);
    }

    public Task<T> FindByAsync(string title)
    {
        var item = Items.Values.First(x => x.Title == title);
        return Task.FromResult(item);
    }

    public Task<T> GetAsync(Guid id)
    {
        return Task.FromResult(Items[id]);
    }

    public Task SaveChangesAsync()
    {
        return Task.CompletedTask;
    }
}