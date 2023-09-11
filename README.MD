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
    // Initializing a fake Cosmos client with a list of pets.
    var fakeClient = new FakedCosmosClient(createMissingContainers: true);
    fakeClient.InitContainer(databaseId: "PetDB", containerId: "Pets", partitionKey: "type", new[]
    {
        new { id = "1", type = "Dog", name = "Buddy" },
        new { id = "2", type = "Cat", name = "Whiskers" }
    });

    // Crafting a query to find a specific pet.
    var query = new QueryDefinition("SELECT * FROM p WHERE p.id = @id").WithParameter("@id", "1");
    var container = fakeClient.GetContainer(databaseId: "PetDB", containerId: "Pets");

    // Fetching the pet details using the mock container.
    var iterator = container.GetItemQueryIterator<JObject>(query);
    var result = new List<JObject>();
    while (iterator.HasMoreResults)
    {
        foreach (var item in await iterator.ReadNextAsync())
        {
            result.Add(item);
        }
    }

    // 'result' now contains data about "Buddy", the dog.
}

```

## When to consider `FakeCosmosEasy`
While Cosmos DB emulators are excellent for achieving high fidelity to the real service, they might be overkill for simpler unit tests or when rapid testing iterations are crucial. In such scenarios, consider `FakeCosmosEasy` as a lightweight, swift alternative that trims the fat without compromising on essential functionalities.

## Future Enhancements for FakeCosmosEasy:
- **Utilizing DynamicLinq:** Our current query parsing approach might benefit from the adaptability of DynamicLinq. This would streamline the process, but handling JObjects remains a challenge as direct property names aren't recognized.

- **Incorporating ANTLR:** ANTLR, a potent parsing tool, can significantly enhance query parsing. However, it presents a steep learning curve.

- **Exploring CosmosSQLANTLR:** The [CosmosSQLANTLR](https://github.com/bchong95/CosmosSQLANTLR) project offers potential insights and components to boost FakeCosmosEasy's parsing abilities.