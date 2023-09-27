using Microsoft.Azure.Cosmos.Scripts;
using Microsoft.Azure.Cosmos;
using RR.FakeCosmosEasy.Helpers;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace RR.FakeCosmosEasy
{
    public class FakedContainer : Container
    {
        private const int PROPERTY_NAME_MAX_LENGTH = 20;

        private List<JObject> Items { get; }

        private readonly string _partitionKey;

        public FakedContainer(string id, FakedDatabase database, string partitionKey, IEnumerable<JObject> items)
            : base()
        {
            Id = id;
            Database = database;
            _partitionKey = EnsureValidPropertyName(partitionKey);
            Items = new List<JObject>(items);
        }

        private static string EnsureValidPropertyName(string input)
        {
            if (input.Length > PROPERTY_NAME_MAX_LENGTH || !Regex.IsMatch(input, @"^[a-zA-Z0-9_-]+$"))
            {
                throw new InvalidOperationException("Invalid property name.");
            }

            return input;
        }

        public override string Id { get; }

        public override Database Database { get; }

        public override Conflicts Conflicts => throw new NotImplementedException();

        public override Scripts Scripts => throw new NotImplementedException();

        public override Task<ItemResponse<T>> CreateItemAsync<T>(T item, PartitionKey? partitionKey = null, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ResponseMessage> CreateItemStreamAsync(Stream streamPayload, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override TransactionalBatch CreateTransactionalBatch(PartitionKey partitionKey)
        {
            throw new NotImplementedException();
        }

        public override Task<ContainerResponse> DeleteContainerAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ResponseMessage> DeleteContainerStreamAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ItemResponse<T>> DeleteItemAsync<T>(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ResponseMessage> DeleteItemStreamAsync(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override ChangeFeedEstimator GetChangeFeedEstimator(string processorName, Container leaseContainer)
        {
            throw new NotImplementedException();
        }

        public override ChangeFeedProcessorBuilder GetChangeFeedEstimatorBuilder(string processorName, ChangesEstimationHandler estimationDelegate, TimeSpan? estimationPeriod = null)
        {
            throw new NotImplementedException();
        }

        public override FeedIterator<T> GetChangeFeedIterator<T>(ChangeFeedStartFrom changeFeedStartFrom, ChangeFeedMode changeFeedMode, ChangeFeedRequestOptions changeFeedRequestOptions = null)
        {
            throw new NotImplementedException();
        }

        public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilder<T>(string processorName, ChangesHandler<T> onChangesDelegate)
        {
            throw new NotImplementedException();
        }

        public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilder<T>(string processorName, ChangeFeedHandler<T> onChangesDelegate)
        {
            throw new NotImplementedException();
        }

        public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilder(string processorName, ChangeFeedStreamHandler onChangesDelegate)
        {
            throw new NotImplementedException();
        }

        public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilderWithManualCheckpoint<T>(string processorName, ChangeFeedHandlerWithManualCheckpoint<T> onChangesDelegate)
        {
            throw new NotImplementedException();
        }

        public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilderWithManualCheckpoint(string processorName, ChangeFeedStreamHandlerWithManualCheckpoint onChangesDelegate)
        {
            throw new NotImplementedException();
        }

        public override FeedIterator GetChangeFeedStreamIterator(ChangeFeedStartFrom changeFeedStartFrom, ChangeFeedMode changeFeedMode, ChangeFeedRequestOptions changeFeedRequestOptions = null)
        {
            throw new NotImplementedException();
        }

        public override Task<IReadOnlyList<FeedRange>> GetFeedRangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override IOrderedQueryable<T> GetItemLinqQueryable<T>(bool allowSynchronousQueryExecution = false, string continuationToken = null, QueryRequestOptions requestOptions = null, CosmosLinqSerializerOptions linqSerializerOptions = null)
        {
            throw new NotImplementedException();
        }

        public override FeedIterator<T> GetItemQueryIterator<T>(QueryDefinition queryDefinition, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            var items = Items.ApplyFilter(queryDefinition.QueryText, queryDefinition.GetQueryParameters());
            var convertedItems = items.Select(x => x.ToObject<T>()).ToList();
            // Use the parameters as desired to decide the behavior.
            return new ListBasedFeedIterator<T>(convertedItems);
        }

        public override FeedIterator<T> GetItemQueryIterator<T>(string queryText = null, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            throw new NotImplementedException();
        }

        public override FeedIterator<T> GetItemQueryIterator<T>(FeedRange feedRange, QueryDefinition queryDefinition, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            throw new NotImplementedException();
        }

        public override FeedIterator GetItemQueryStreamIterator(QueryDefinition queryDefinition, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            throw new NotImplementedException();
        }

        public override FeedIterator GetItemQueryStreamIterator(string queryText = null, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            throw new NotImplementedException();
        }

        public override FeedIterator GetItemQueryStreamIterator(FeedRange feedRange, QueryDefinition queryDefinition, string continuationToken, QueryRequestOptions requestOptions = null)
        {
            throw new NotImplementedException();
        }

        public override async Task<ItemResponse<T>> PatchItemAsync<T>(string id, PartitionKey partitionKey, IReadOnlyList<PatchOperation> patchOperations, PatchItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            var partitionKeyValue = JArray.Parse(partitionKey.ToString())[0].ToString();
            // Update or replace the original item in your InMemory storage as needed
            JObject? item = Items.Find(x =>
                x["id"].ToString() == id &&
                x[_partitionKey].ToString() == partitionKeyValue);

            if (item == null)
            {
                throw new CosmosException("Item not exist", System.Net.HttpStatusCode.NotFound, 0, String.Empty, 0);
            }

            // Ensure item is JObject to allow patching
            var itemAsJObject = item as JObject;
            if (itemAsJObject == null)
            {
                throw new InvalidOperationException("Item must be JObject to be patchable.");
            }

            // Apply each patch operation
            foreach (var operation in patchOperations)
            {
                ApplyPatchOperation(itemAsJObject, operation);
            }

            return new InMemoryItemResponse<T>(itemAsJObject.ToObject<T>());
        }

        private void ApplyPatchOperation(JObject item, PatchOperation operation)
        {
            switch (operation.OperationType)
            {
                case PatchOperationType.Set:
                    item[operation.Path.Replace("/", "")] = GetValueFromOperation(operation);
                    break;

                case PatchOperationType.Remove:
                    item.Remove(operation.Path);
                    break;

                // Implement other operation types as needed...
                default:
                    throw new NotSupportedException($"Operation type {operation.OperationType} is not supported.");
            }
        }

        private JToken GetValueFromOperation(PatchOperation operation)
        {
            var type = operation.GetType();
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PatchOperation<>))
                {
                    var valueProperty = type.GetProperty("Value");
                    if (valueProperty != null)
                    {
                        var value = valueProperty.GetValue(operation);
                        if (value == null)
                        {
                            return JValue.CreateNull();
                        }
                        return JToken.FromObject(value);
                    }
                }
                type = type.BaseType;
            }
            throw new InvalidOperationException("Operation does not have a Value property.");
        }

        public override Task<ResponseMessage> PatchItemStreamAsync(string id, PartitionKey partitionKey, IReadOnlyList<PatchOperation> patchOperations, PatchItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ContainerResponse> ReadContainerAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ResponseMessage> ReadContainerStreamAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override async Task<ItemResponse<T>> ReadItemAsync<T>(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            var partitionKeyValue = JArray.Parse(partitionKey.ToString())[0].ToString();
            JObject? item = Items.Find(x =>
                x["id"].ToString() == id &&
                x[_partitionKey].ToString() == partitionKeyValue);

            if (item == null)
            {
                throw new CosmosException("Item not exist", System.Net.HttpStatusCode.NotFound, 0, String.Empty, 0);
            }

            return new InMemoryItemResponse<T>(item.ToObject<T>());
        }

        public override Task<ResponseMessage> ReadItemStreamAsync(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<FeedResponse<T>> ReadManyItemsAsync<T>(IReadOnlyList<(string id, PartitionKey partitionKey)> items, ReadManyRequestOptions readManyRequestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ResponseMessage> ReadManyItemsStreamAsync(IReadOnlyList<(string id, PartitionKey partitionKey)> items, ReadManyRequestOptions readManyRequestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<int?> ReadThroughputAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ThroughputResponse> ReadThroughputAsync(RequestOptions requestOptions, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ContainerResponse> ReplaceContainerAsync(ContainerProperties containerProperties, ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ResponseMessage> ReplaceContainerStreamAsync(ContainerProperties containerProperties, ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ItemResponse<T>> ReplaceItemAsync<T>(T item, string id, PartitionKey? partitionKey = null, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ResponseMessage> ReplaceItemStreamAsync(Stream streamPayload, string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ThroughputResponse> ReplaceThroughputAsync(int throughput, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ThroughputResponse> ReplaceThroughputAsync(ThroughputProperties throughputProperties, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ItemResponse<T>> UpsertItemAsync<T>(T item, PartitionKey? partitionKey = null, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ResponseMessage> UpsertItemStreamAsync(Stream streamPayload, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
