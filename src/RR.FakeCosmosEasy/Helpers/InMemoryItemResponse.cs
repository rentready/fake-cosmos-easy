using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RR.FakeCosmosEasy.Helpers
{
    public class InMemoryItemResponse<T> : ItemResponse<T>
    {
        private T _item;
        private HttpStatusCode _statusCode = HttpStatusCode.OK; // Default
        private Headers _headers = new Headers();

        public InMemoryItemResponse(T item)
        {
            _item = item;
        }

        public override T Resource => _item;
        public override HttpStatusCode StatusCode => _statusCode;
        public override Headers Headers => _headers;

        // Implement other members if necessary...
    }
}
