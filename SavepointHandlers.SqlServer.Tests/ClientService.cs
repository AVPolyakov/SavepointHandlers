using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace SavepointHandlers.SqlServer.Tests
{
    public class ClientService : IClientService
    {
        private static readonly AsyncLocal<TargetClientService> _current = new();
        
        private static TargetClientService Current
        {
            get => _current.Value!;
            set => _current.Value = value;
        }
        
        public static void Subscribe()
        {
            LocalTransactionScope.AddTransactionObserver(new TransactionObserver());
            
            AmbientTransactionData.Add(() =>
            {
                var current = Current;
                return () => Current = current;
            });
        }

        private class TransactionObserver : ITransactionObserver
        {
            public void OnBegin() => Current = new TargetClientService();

            public void OnSetSavepoint(string savepointName) => Current.OnSetSavepoint(savepointName);

            public void OnRollbackToSavepoint(string savepointName) => Current.OnRollbackToSavepoint(savepointName);
        }

        private class TargetClientService
        {
            private ImmutableDictionary<int, ClientTuple> _clientTuples = ImmutableDictionary.Create<int, ClientTuple>();

            /// <summary>
            /// Используем обычный Dictionary (не ConcurrentDictionary) по следующей причине. Мы эмулируем в памяти работу с БД.
            /// В рамках одного теста работа с БД идет в одной транзакции. Транзакция одна, поэтому и коннекшен к БД один.
            /// На одном коннекшене нельзя выполнять запросы к БД в параллельном режиме. Поэтому и в нашем эмуляторе параллельный режим не нужен.
            /// При этом экземпляр сервиса Current хранится в AsyncLocal, поэтому экземпляр сервиса свой для каждого теста.
            /// </summary>
            private readonly Dictionary<string, ImmutableDictionary<int, ClientTuple>> _clientTuplesBySavepoint = new();

            public void OnSetSavepoint(string savepointName)
            {
                _clientTuplesBySavepoint.Add(savepointName, _clientTuples);
            }

            public void OnRollbackToSavepoint(string savepointName)
            {
                _clientTuples = _clientTuplesBySavepoint[savepointName];
            }
            
            public void Create(Client client)
            {
                _clientTuples = _clientTuples.Add(client.Id, ToClientTuple(client));
            }
            
            public void Update(int id, Client client)
            {
                _clientTuples = _clientTuples.Remove(id).Add(id, ToClientTuple(client));
            }
            
            public Client GetById(int id)
            {
                return ToClient(_clientTuples[id]);
            }

            private record ClientTuple(int Id, string? Name);
            
            private static ClientTuple ToClientTuple(Client client)
            {
                return new ClientTuple(
                    Id: client.Id,
                    Name: client.Name);
            }

            private static Client ToClient(ClientTuple clientTuple)
            {
                return new Client
                {
                    Id = clientTuple.Id,
                    Name = clientTuple.Name
                };
            }
        }

        public void Create(Client client) => Current.Create(client);

        public void Update(int id, Client client) => Current.Update(id, client);
        
        public Client GetById(int id) => Current.GetById(id);
    }
}
