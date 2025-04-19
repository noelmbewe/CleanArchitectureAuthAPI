using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public IRepository<User> Users { get; }

    public UnitOfWork(AppDbContext context, IRepository<User> userRepository)
    {
        _context = context;
        Users = userRepository;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}