using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace RR.FakeCosmosEasy.UnitTests
{
    public class FakedCosmosClientTests
    {
        [Fact]
        public async Task CreateItemAsync_should_create_item()
        {
            // Arrange
            var fakeClient = new FakedCosmosClient(createMissingContainers: true);
            fakeClient.InitContainer(databaseId: "SampleDatabase", containerId: "SampleContainer", partitionKey: "_entity", new JObject[] { });

            var container = fakeClient.GetContainer("SampleDatabase", "SampleContainer");

            // Act
            await container.CreateItemAsync(new { id = "A1", property = "Value1" }, new PartitionKey("Partition1"));

            // Assert
            var result = (await container.ReadItemAsync<JObject>("A1", new PartitionKey("Partition1"))).Resource;
            Assert.Equal("A1", result["id"]);
            Assert.Equal("Value1", result["property"]);
            Assert.Equal("Partition1", result["_entity"]);
        }

        [Fact]
        public async Task CreateItemAsync_should_create_item_with_unspecified_partition()
        {
            // Arrange
            var fakeClient = new FakedCosmosClient(createMissingContainers: true);
            fakeClient.InitContainer(databaseId: "SampleDatabase", containerId: "SampleContainer", partitionKey: "_entity", new JObject[] { });

            var container = fakeClient.GetContainer("SampleDatabase", "SampleContainer");

            // Act
            await container.CreateItemAsync(new { id = "A1", property = "Value1" });

            // Assert
            var result = (await container.ReadItemAsync<JObject>("A1", default)).Resource;
            Assert.Equal("A1", result["id"]);
            Assert.Equal("Value1", result["property"]);
        }

        [Fact]
        public async Task Should_initialize_containers_and_query_data()
        {
            // Arrange
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

        [Fact]
        public async Task Should_Return_Two_Records_With_Specified_Id_And_DateRange()
        {
            // Assert
            var fakeClient = new FakedCosmosClient(createMissingContainers: true);
            Guid targetId = Guid.NewGuid();
            DateTime testDate = new DateTime(2023, 9, 10);

            fakeClient.InitContainer(databaseId: "SampleDatabase", containerId: "SampleContainer", partitionKey: "_entity", new[]
            {
                new { id = targetId.ToString(), _entity = "Partition2", starttime = testDate },
                new { id = targetId.ToString(), _entity = "Partition2", starttime = testDate },
                new { id = Guid.NewGuid().ToString(), _entity = "Partition2", starttime = testDate },  // Different ID
                new { id = targetId.ToString(), _entity = "Partition2", starttime = new DateTime(2023, 9, 9) }  // Different date
            });

            var container = fakeClient.GetContainer("SampleDatabase", "SampleContainer");
            var queryText = @"SELECT c.id, c._entity FROM c WHERE c.id = @id AND c.starttime >= @dateFrom AND c.starttime <= @dateEnd";

            var query = new QueryDefinition(queryText)
                .WithParameter("@id", targetId)
                .WithParameter("@dateFrom", testDate.Date)
                .WithParameter("@dateEnd", testDate.Date.AddDays(1));

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

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task PatchOperation_Should_Set_Property_To_Null()
        {
            // Arrange
            var fakeClient = new FakedCosmosClient(createMissingContainers: true);
            var targetId = Guid.NewGuid().ToString();

            // Create a sample record with some initial data.
            fakeClient.InitContainer(databaseId: "SampleDatabase", containerId: "SampleContainer", partitionKey: "_entity", new[]
            {
                new { id = targetId, _entity = "Partition1", name = "John Doe", age = 30 }
            });

            var container = fakeClient.GetContainer("SampleDatabase", "SampleContainer");

            // Create a patch operation to set 'name' property to null
            var patchOperations = new List<PatchOperation>
            {
                PatchOperation.Set<string>("/name", null)
            };

            // Act
            await container.PatchItemAsync<JObject>(targetId, new PartitionKey("Partition1"), patchOperations);

            // Retrieve the item after patching to check the update.
            var response = await container.ReadItemAsync<JObject>(targetId, new PartitionKey("Partition1"));
            var updatedItem = response.Resource;

            // Assert
            Assert.Null(updatedItem["name"].Value<String>());
            Assert.Equal(30, updatedItem["age"].ToObject<int>());
        }
    }
}
