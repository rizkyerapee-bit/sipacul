using SiPacul.Application.Common.Persistence;

namespace SiPacul.Infrastructure.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SiPaculDbContext _dbContext;

    public UnitOfWork(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
