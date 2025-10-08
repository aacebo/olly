using OS.Agent.Models;

namespace OS.Agent.Stores;

public interface IInstallStorage
{
    Task<Installation?> GetById(long id);
    Task<Installation> Create(Installation.Create create);
    Task<Installation> Update(long id, Installation.Update update);
    Task Delete(long id);
}