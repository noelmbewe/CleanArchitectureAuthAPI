using Domain.Entities;

namespace Application.Interfaces;

public interface IUnitOfWork
{
    IRepository<User> Users { get; }
    Task<int> SaveChangesAsync();
}