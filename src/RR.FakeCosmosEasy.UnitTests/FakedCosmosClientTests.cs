using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RR.FakeCosmosEasy.UnitTests
{
    public class FakedCosmosClientTests
    {
        [Fact]
        public async Task Should_initialize_containers_and_query_data()
        {
            // Assert
            var fakeClient = new FakedCosmosClient(createMissingContainers: true);
            fakeClient.InitContainer(databaseId: "SampleDatabase", containerId: "SampleContainer", partitionKey: "_entity", new[]
            {
                new { id = "A1", _entity = "Partition1", property = "Value1" },
                new { id = "B2", _entity = "Partition2", property = "Value2" },
                new { id = "C3", _entity = "Partition3", property = "Value3" },
                // ... Add more sample data as needed
            });

            var container = fakeClient.GetContainer("SampleDatabase", "SampleContainer");
            var queryText = @"SELECT c.id, c._entity FROM c WHERE c.property = @value";

            var query = new QueryDefinition(queryText).WithParameter("@value", "Value1");

            // Act
            var iterator = container.GetItemQueryIterator<JObject>(query);
            
            // Assert
            var result = new List<JObject>();

            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                {
                    result.Add(item);
                }
            }

            Assert.Single(result, x => x["id"].ToString() == "A1");
        }
    }
}
