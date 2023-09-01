using Neo4j.Driver;
using Neo4j.Driver.Preview.Mapping;

var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "password"));

//RecordObjectMapping.RegisterProvider<MyMappingProvider>();

var moviesAndPeople = await driver
    .ExecutableQuery("MATCH (m:Movie)<-[r:%]-(p:Person) RETURN m AS movie, p AS person, type(r) as relationship")
    .ExecuteAsync()
    .AsObjectsAsync<MovieAndPerson>();

foreach (var record in moviesAndPeople)
{
    Console.WriteLine($"{record.Person.Name} {record.Relationship} {record.Movie.Title}");
    //Console.WriteLine($"{record.PersonName} {record.Relationship} {record.Title} ({record.ReleaseYear}) aged {record.AgeInMovie}");
}

Console.ReadLine();

// ==========

public class SimpleMoviePersonRecord
{
    public string Title { get; set; } = "";

    [MappingPath("movie.released")]
    public int ReleaseYear { get; set; }

    [MappingPath("person.name")]
    public string PersonName { get; set; } = "";

    public string Relationship { get; set; } = "";

    public int AgeInMovie { get; set; }
}

public class MovieAndPerson
{
    public Person Person { get; set; }
    public Movie Movie { get; set; }
    public string Relationship { get; set; }
}

public class Movie
{
    public string Title { get; set; } = "";
    public int Released { get; set; }
    public string Tagline { get; set; } = "";
}

public class Person
{
    public string Name { get; set; } = "";
    public int Born { get; set; }
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
                        .Map(m => m.Released, "released")
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
                        .Map(m => m.Relationship, "relationship")
                        .Map(m => m.Person, "person");
                });
    }

//     public void CreateMappers(IMappingRegistry registry)
//     {
//         registry.RegisterMapping<SimpleMoviePersonRecord>(
//             builder =>
//             {
//                 builder
//                     .Map(x => x.Title, r => r.GetNode("movie").GetValue<string>("title"))
//                     .Map(x => x.ReleaseYear, r => r.GetNode("movie").GetValue<int>("released"))
//                     .Map(x => x.PersonName, r => r.GetNode("person").GetValue<string>("name"))
//                     .Map(x => x.Relationship, r => r.GetValue<string>("relationship"))
//                     .Map(x => x.AgeInMovie, r =>
//                     {
//                         var released = r.GetNode("movie").GetValue<int>("released");
//                         var person = r.GetNode("person");
//                         if (person.Properties.TryGetValue("born", out var born))
//                         {
//                             return released - born.As<int>();
//                         }
//
//                         return -1;
//                     });
//             });
//     }
}
