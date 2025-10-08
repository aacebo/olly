using OS.Agent.Models;

namespace OS.Agent.Stores;

public interface IAccountStorage
{
    Task<Account?> GetById(Guid id);
    Task<IEnumerable<Account>> GetByUserId(Guid userId);
    Task<Account> Create(Account value);
    Task<Account> Update(Account value);
    Task Delete(Guid id);
}