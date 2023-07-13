namespace SavepointHandlers.SqlServer.Tests;

public interface IClientService
{
    void Create(Client client);
    void Update(int id, Client client);
    Client GetById(int id);
}