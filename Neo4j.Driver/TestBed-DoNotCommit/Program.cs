using Neo4j.Driver;
using Neo4j.Driver.Preview.Mapping;

var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "password"));

RecordMappers.RegisterProvider<MyMappingProvider>();

var moviesAndPeople = await driver
    .ExecutableQuery("MATCH (m:Movie)<-[]-(p:Person) RETURN m AS movie, p AS person")
    .ExecuteAsync()
    .AsObjectsAsync<MovieAndPerson>();

foreach (var record in moviesAndPeople)
{
    Console.WriteLine($"{record.Movie.Title} ({record.Movie.ReleaseYear}): {record.Person.Name}");
}

Console.ReadLine();

// ==========

public class Movie
{
    public string Title { get; set; } = "";

    [MappingPath("released")]
    public int ReleaseYear { get; set; }
    public string Tagline { get; set; } = "";
}

public class Person
{
    public string Name { get; set; } = "";
    public int Born { get; set; }
}

public class MovieAndPerson
{
    public Movie Movie { get; set; }
    public Person Person { get; set; }
}

public class MyMappingProvider : IMappingProvider
{
    public void CreateMappers(IMappingRegistry registry)
    {
        registry
            .RegisterMapping<Movie>(
                builder =>
                {
                    builder
                        .Map(m => m.Title, "title")
                        .Map(m => m.ReleaseYear, "released")
                        .Map(m => m.Tagline, "tagline");
                })
            .RegisterMapping<Person>(
                builder =>
                {
                    builder
                        .Map(m => m.Name, "name")
                        .Map(m => m.Born, "born", x => x.As<int>());
                })
            .RegisterMapping<MovieAndPerson>(
                builder =>
                {
                    builder
                        .Map(m => m.Movie, "movie")
                        .Map(m => m.Person, "person");
                });
    }
}
