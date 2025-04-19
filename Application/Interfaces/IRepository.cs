namespace Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task AddAsync(T entity);
    Task<T?> GetByEmailAsync(string email);
}