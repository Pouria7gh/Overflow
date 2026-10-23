using Typesense;

namespace SearchService.Data;

public static class SearchInitializer
{
    public static async Task EnsureIndexExists(ITypesenseClient client)
    {
        const string schemaName = "questions";
        try
        {
            // if collection exists return
            await client.RetrieveCollection(schemaName);
            Console.WriteLine($"Collection {schemaName} has been created already");
            return;
        }
        catch (TypesenseApiNotFoundException e)
        {
            Console.WriteLine($"Collection {schemaName} has not been created yet");
        }

        // create question schema
        var schema = new Schema(schemaName, new List<Field>()
        {
            new("id", FieldType.String),
            new("title", FieldType.String),
            new("content", FieldType.String),
            new("tags", FieldType.StringArray),
            new("createdAt", FieldType.Int64),
            new("hasAcceptedAnswer", FieldType.Bool),
            new("answerCount", FieldType.Int32),
        })
        {
            DefaultSortingField = "createdAt"
        };

        // create collection
        await client.CreateCollection(schema);
    }
}