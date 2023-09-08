using Microsoft.Azure.Cosmos;
using System.Net;

namespace RR.FakeCosmosEasy.Helpers
{
    public class ListBasedFeedIterator<T> : FeedIterator<T>
    {
        private readonly IList<T> _items;
        private int _currentIndex = 0;

        public ListBasedFeedIterator(IList<T> items)
        {
            _items = items;
        }

        public override bool HasMoreResults => _currentIndex < _items.Count;

        public override async Task<FeedResponse<T>> ReadNextAsync(CancellationToken cancellationToken = default)
        {
            // This is a simplistic example. In a real-world scenario, you might want to handle paging, continuation tokens, etc.
            if (!HasMoreResults)
                throw new InvalidOperationException("No more items to iterate over.");

            var item = _items[_currentIndex];
            _currentIndex++;

            // Constructing a fake FeedResponse<T>. You might want to mock this more thoroughly.
            return new FeedResponseMock<T>(new List<T> { item });
        }

        private class FeedResponseMock<TItem> : FeedResponse<TItem>
        {
            private readonly IReadOnlyList<TItem> _items;

            public FeedResponseMock(IReadOnlyList<TItem> items)
            {
                _items = items;
            }

            public override int Count => _items.Count;
            public override string ContinuationToken => throw new NotImplementedException();
            public override double RequestCharge => throw new NotImplementedException();
            public override string ActivityId => throw new NotImplementedException();
            public override IReadOnlyList<TItem> Resource => _items;
            public override HttpStatusCode StatusCode => throw new NotImplementedException();
            public override Headers Headers => throw new NotImplementedException();

            public override string IndexMetrics => throw new NotImplementedException();

            public override CosmosDiagnostics Diagnostics => throw new NotImplementedException();

            public override IEnumerator<TItem> GetEnumerator() => _items.GetEnumerator();
        }
    }
}