# FakeCosmosEasy: TDD for Cosmos DB
## Introduction
`FakeCosmosEasy` is a TDD (Test Driven Development) library designed for NoSQL Cosmos DB. While Cosmos DB emulators provide an environment that closely mirrors the real Cosmos DB service and allow offline development, they might sometimes present a performance overhead. For certain tests where complete emulation is unnecessary, `FakeCosmosEasy` steps in to optimize the process.

## Features & Current Limitations
1. **Initial Version:** As of now, this is the inaugural version of `FakeCosmosEasy`.
1. **SQL Queries:** It supports basic SQL queries, with plans for enhanced support in upcoming iterations.
1. **Operations:** The current version has a limited set of operations. More operations will be added based on user feedback and requirements.

## Advantages of using `FakeCosmosEasy`
1. **Speed:** It delivers faster test execution, significantly reducing the time taken for unit tests.
1. **Simplicity:** Set up is straightforward, minimizing configuration hassles.
1. **Flexibility:** It supports the dynamic creation of missing containers, streamlining the testing process.
1. **Customizable Data:** Easily mock data tailored to your unit tests, ensuring more focused and relevant tests.

## Quick Start
Let's dive into a basic example showcasing how `FakeCosmosEasy` can be put to use:

```csharp
[Fact]
public async Task SimpleTest()
{
    var fakeClient = new FakedCosmosClient(createMissingContainers: true);
    fakeClient.InitContainer(databaseId: "dynamics", containerId: "rr_jobprofile", partitionKey: "_entity", new[]
    {
        new { id = "A1", _entity = "Partition1", property = "Value1" },
        new { id = "B2", _entity = "Partition2", property = "Value2" },
        new { id = "C3", _entity = "Partition3", property = "Value3" },
        // ... Add more sample data as needed
    });

    var container = fakeClient.GetContainer("SampleDatabase", "SampleContainer");
    var queryText = @"SELECT c.id, c._entity FROM c WHERE c.property = @value";
    
    var query = new QueryDefinition(queryText).WithParameter("@value", "Value1");
    var iterator = container.GetItemQueryIterator<dynamic>(query);

    while (iterator.HasMoreResults)
    {
        var results = await iterator.ReadNextAsync();
        foreach (var item in results)
        {
            // Process items here...
        }
    }
}

```

## When to consider `FakeCosmosEasy`
While Cosmos DB emulators are excellent for achieving high fidelity to the real service, they might be overkill for simpler unit tests or when rapid testing iterations are crucial. In such scenarios, consider `FakeCosmosEasy` as a lightweight, swift alternative that trims the fat without compromising on essential functionalities.