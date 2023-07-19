using System;

namespace SavepointHandlers.SqlServer.Tests;

public interface IClientService
{
    void Create(Client client);
    void Update(int id, Action<Client> action);
    Client GetById(int id);
}