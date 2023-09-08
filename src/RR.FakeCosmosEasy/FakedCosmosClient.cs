using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RR.FakeCosmosEasy
{
    public class FakedCosmosClient : CosmosClient
    {
        private readonly Dictionary<string, FakedDatabase> _databases = new Dictionary<string, FakedDatabase>();
        private readonly bool _createMissingContainers;

        public FakedCosmosClient(bool createMissingContainers = false)
        {
            _createMissingContainers = createMissingContainers;
        }

        public void InitContainer<T>(string databaseId, string containerId, string partitionKey, IList<T> items)
        {
            if (!_databases.TryGetValue(databaseId, out var database)) {
                database = new FakedDatabase();
                _databases.Add(databaseId, database);
            }

            var objects = items.Select(x => JObject.FromObject(x));
            var container = new FakedContainer(containerId, database, partitionKey, objects);
            database.Containers.Add(containerId, container);
        }

        public override Container GetContainer(string databaseId, string containerId)
        {
            if (_databases[databaseId].Containers.TryGetValue(containerId, out var container))
            {
                return container;
            }
            return new FakedContainer(containerId, null, "partitionKey", new JObject[] {});
        }

        public override Database GetDatabase(string id)
        {
            return _databases[id];
        }
    }
}
