namespace Globomantics.Infrastructure.Data.Repositories;

public interface IRepository<T>
{
    Task<T> GetAsync(Guid id);
    Task<T> FindByAsync(string title);
    Task<IEnumerable<T>> AllAsync();
    Task AddAsync(T item);
    Task SaveChangesAsync();
}