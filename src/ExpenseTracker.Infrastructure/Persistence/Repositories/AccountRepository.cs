using ExpenseTracker.Domain.Account;
using ExpenseTracker.Services.Contracts;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

public class AccountRepository : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Account>> GetActiveAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddOrUpdateAsync(Account account, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}