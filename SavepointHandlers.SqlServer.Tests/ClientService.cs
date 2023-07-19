using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace SavepointHandlers.SqlServer.Tests
{
    public class ClientService : IClientService
    {
        private static readonly AsyncLocal<Data> _current = new();
        private static readonly TransactionObserver _transactionObserver = new();
        private static readonly Func<Action> _ambientTransactionDataFunc = () =>
        {
            var current = Current;
            return () => Current = current;
        };

        public static void Subscribe()
        {
            LocalTransactionScope.AddTransactionObserver(_transactionObserver);
            AmbientTransactionData.Add(_ambientTransactionDataFunc);
        }

        private class TransactionObserver : ITransactionObserver
        {
            public void OnBegin() => Current = new Data();

            public void OnSetSavepoint(string savepointName)
            {
                Current.ClientsBySavepointName.Add(savepointName, Clients);
            }

            public void OnRollbackToSavepoint(string savepointName)
            {
                Clients = Current.ClientsBySavepointName[savepointName];
            }
        }
        
        private static Data Current
        {
            get => _current.Value!;
            set => _current.Value = value;
        }
        
        private class Data
        {
            public ImmutableList<ClientTuple> Clients = ImmutableList.Create<ClientTuple>();

            /// <summary>
            /// Используем обычный Dictionary (не ConcurrentDictionary) по следующей причине. Мы эмулируем в памяти работу с БД.
            /// В рамках одного теста работа с БД идет в одной транзакции. Транзакция одна, поэтому и коннекшен к БД один.
            /// На одном коннекшене нельзя выполнять запросы к БД в параллельном режиме. Поэтому и в нашем эмуляторе параллельный режим не нужен.
            /// При этом экземпляр сервиса Current хранится в AsyncLocal, поэтому экземпляр сервиса свой для каждого теста.
            /// </summary>
            public readonly Dictionary<string, ImmutableList<ClientTuple>> ClientsBySavepointName = new();
        }
        
        private static ImmutableList<ClientTuple> Clients
        {
            get => Current.Clients;
            set => Current.Clients = value;
        }
        
        public void Create(Client client)
        {
            Clients = Clients.Add(ToClientTuple(client));
        }
        
        public void Update(int id, Action<Client> action)
        {
            var item = Clients.Select((value, index) => new {value, index}).FirstOrDefault(x => x.value.Id == id);
            if (item != null)
            {
                var client = ToClient(item.value);
                action(client);
                Clients = Clients.RemoveAt(item.index).Add(ToClientTuple(client));
            }
        }

        public Client GetById(int id)
        {
            return ToClient(Clients.Find(tuple => tuple.Id == id)!);
        }

        private static ClientTuple ToClientTuple(Client client)
        {
            return new ClientTuple
            {
                Id = client.Id,
                Name = client.Name
            };
        }

        private static Client ToClient(ClientTuple tuple)
        {
            return new Client
            {
                Id = tuple.Id,
                Name = tuple.Name
            };
        }
        
        private class ClientTuple
        {
            public int Id { get; init; }
            public string? Name { get; init; }
        }
    }
}
