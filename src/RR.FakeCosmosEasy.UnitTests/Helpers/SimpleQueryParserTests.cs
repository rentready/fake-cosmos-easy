using Newtonsoft.Json.Linq;
using RR.FakeCosmosEasy.Helpers;

namespace RR.FakeCosmosEasy.UnitTests.Helpers
{
    public class SimpleQueryParserTests
    {
        [Fact]
        public void TestBasicEqualityFilter()
        {
            // Arrange
            var items = new List<JObject>
            {
                JObject.Parse("{ \"id\": \"1\", \"name\": \"John\" }"),
                JObject.Parse("{ \"id\": \"2\", \"name\": \"Doe\" }")
            };
            var query = "SELECT * FROM c WHERE c.name = @name";
            var parameters = new List<(string Name, object Value)> { ("@name", "John") };

            // Act
            var result = items.ApplyFilter(query, parameters);

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result.First()["id"].ToString());
        }

        [Fact]
        public void TestANDOperator()
        {
            // Arrange
            var items = new List<JObject>
        {
            JObject.Parse("{ \"id\": \"1\", \"name\": \"John\", \"age\": 30 }"),
            JObject.Parse("{ \"id\": \"2\", \"name\": \"John\", \"age\": 25 }"),
            JObject.Parse("{ \"id\": \"3\", \"name\": \"Doe\", \"age\": 25 }")
        };
            var query = "SELECT * FROM c WHERE c.name = @name AND c.age = @age";
            var parameters = new List<(string Name, object Value)> { ("@name", "John"), ("@age", 25) };

            // Act
            var result = items.ApplyFilter(query, parameters);

            // Assert
            Assert.Single(result);
            Assert.Equal("2", result.First()["id"].ToString());
        }

        [Fact]
        public void TestOROperator()
        {
            // Arrange
            var items = new List<JObject>
            {
                JObject.Parse("{ \"id\": \"1\", \"name\": \"John\", \"country\": \"US\" }"),
                JObject.Parse("{ \"id\": \"2\", \"name\": \"Doe\", \"country\": \"CA\" }"),
                JObject.Parse("{ \"id\": \"3\", \"name\": \"Smith\", \"country\": \"US\" }")
            };
            var query = "SELECT * FROM c WHERE c.country = @country OR c.name = @name";
            var parameters = new List<(string Name, object Value)> { ("@country", "CA"), ("@name", "John") };

            // Act
            var result = items.ApplyFilter(query, parameters);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, item => item["id"].ToString() == "1");
            Assert.Contains(result, item => item["id"].ToString() == "2");
        }

        [Fact]
        public void TestComplexCondition()
        {
            // Arrange
            var items = new List<JObject>
            {
                JObject.Parse("{ \"id\": \"1\", \"name\": \"John\", \"country\": \"US\", \"age\": 30 }"),
                JObject.Parse("{ \"id\": \"2\", \"name\": \"Doe\", \"country\": \"CA\", \"age\": 25 }"),
                JObject.Parse("{ \"id\": \"3\", \"name\": \"Smith\", \"country\": \"US\", \"age\": 35 }")
            };
            var query = "SELECT * FROM c WHERE c.country = @country AND c.age = @age OR c.name = @name";
            var parameters = new List<(string Name, object Value)> { ("@country", "US"), ("@age", 35), ("@name", "Doe") };

            // Act
            var result = items.ApplyFilter(query, parameters);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, item => item["id"].ToString() == "2");
            Assert.Contains(result, item => item["id"].ToString() == "3");
        }

        [Fact]
        public void TestSelectSpecificFields()
        {
            // Arrange
            var items = new List<JObject>
            {
                JObject.Parse("{ \"id\": \"1\", \"name\": \"John\", \"country\": \"US\" }"),
                JObject.Parse("{ \"id\": \"2\", \"name\": \"Doe\", \"country\": \"CA\" }")
            };
            var query = "SELECT c.id, c.name FROM c WHERE c.country = @country";
            var parameters = new List<(string Name, object Value)> { ("@country", "US") };

            // Act
            var result = items.ApplyFilter(query, parameters).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0]["id"].ToString());
            Assert.Equal("John", result[0]["name"].ToString());
            Assert.Null(result[0]["country"]);  // country shouldn't be in the result because it wasn't in the SELECT fields
        }

        [Fact]
        public void TestMissingParameter()
        {
            // Arrange
            var items = new List<JObject>
            {
                JObject.Parse("{ \"id\": \"1\", \"name\": \"John\" }")
            };
            var query = "SELECT * FROM c WHERE c.name = @name";
            var parameters = new List<(string Name, object Value)> { };  // Missing parameter

            // Act
            var result = items.ApplyFilter(query, parameters);

            // Assert
            Assert.Empty(result);  // Should return empty due to the missing parameter
        }

        [Fact]
        public void TestGreaterThanOperator()
        {
            // Arrange
            var items = new List<JObject>
            {
                JObject.Parse("{ \"id\": \"1\", \"age\": 20 }"),
                JObject.Parse("{ \"id\": \"2\", \"age\": 25 }")
            };
            var query = "SELECT * FROM c WHERE c.age > @age";
            var parameters = new List<(string Name, object Value)> { ("@age", 20) };

            // Act
            var result = items.ApplyFilter(query, parameters);

            // Assert
            Assert.Single(result);
            Assert.Equal("2", result.First()["id"].ToString());
        }
    }
}
