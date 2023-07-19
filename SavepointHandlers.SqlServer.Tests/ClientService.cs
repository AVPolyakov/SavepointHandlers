using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                Current.ClientDictionariesBySavepointName.Add(savepointName, ClientDictionary);
            }

            public void OnRollbackToSavepoint(string savepointName)
            {
                ClientDictionary = Current.ClientDictionariesBySavepointName[savepointName];
            }
        }
        
        private static Data Current
        {
            get => _current.Value!;
            set => _current.Value = value;
        }
        
        private class Data
        {
            public ImmutableDictionary<int, ClientTuple> ClientDictionary = ImmutableDictionary.Create<int, ClientTuple>();

            /// <summary>
            /// Используем обычный Dictionary (не ConcurrentDictionary) по следующей причине. Мы эмулируем в памяти работу с БД.
            /// В рамках одного теста работа с БД идет в одной транзакции. Транзакция одна, поэтому и коннекшен к БД один.
            /// На одном коннекшене нельзя выполнять запросы к БД в параллельном режиме. Поэтому и в нашем эмуляторе параллельный режим не нужен.
            /// При этом экземпляр сервиса Current хранится в AsyncLocal, поэтому экземпляр сервиса свой для каждого теста.
            /// </summary>
            public readonly Dictionary<string, ImmutableDictionary<int, ClientTuple>> ClientDictionariesBySavepointName = new();
        }
        
        private static ImmutableDictionary<int, ClientTuple> ClientDictionary
        {
            get => Current.ClientDictionary;
            set => Current.ClientDictionary = value;
        }
        
        public void Create(Client client)
        {
            ClientDictionary = ClientDictionary.Add(client.Id, ToClientTuple(client));
        }

        public void Update(int id, Client client)
        {
            ClientDictionary = ClientDictionary.Remove(id).Add(id, ToClientTuple(client));
        }

        public Client GetById(int id)
        {
            return ToClient(ClientDictionary[id]);
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
