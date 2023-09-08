using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RR.FakeCosmosEasy.SQLParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RR.FakeCosmosEasy.Helpers
{
    public static class SimpleQueryParser
    {
        public static IEnumerable<JObject> ApplyFilter(this IEnumerable<JObject> items, string query, IReadOnlyList<(string Name, object Value)> parameters)
        {
            // Extract the SELECT and WHERE parts from the query
            var selectFields = query.Split(new[] { "SELECT", "FROM" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim().Split(',').Select(x => x.Trim()).ToList();

            var whereConditions = query.Split(new[] { "WHERE" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();

            var predicate = PredicateParser.ConvertToPredicate(whereConditions, parameters);
            var filteredItems = items.Where(predicate).ToList();

            if (selectFields.Contains("*"))
            {
                return filteredItems.Select(x => x.DeepClone().ToObject<JObject>()).ToList();
            }

            // If specific fields are selected, create new instances with only those fields populated
            return filteredItems.Select(item =>
            {
                // Create a new JObject and populate it with the properties specified in selectFields
                var selectedJObject = new JObject();
                foreach (var field in selectFields)
                {
                    var fieldWithoutC = field.Replace("c.", "");
                    JToken token;
                    if (item.TryGetValue(fieldWithoutC, StringComparison.InvariantCultureIgnoreCase, out token))
                    {
                        selectedJObject[fieldWithoutC] = token;
                    }
                }

                // Deserialize the new JObject back to type T
                return selectedJObject.ToObject<JObject>();
            }).ToList();
        }
    }
}
