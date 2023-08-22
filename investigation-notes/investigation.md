## Interface

```c#
ExecutableQuery(cypher)
	.ReturnObjects<T>() // uses convention mapper unless specified
	.ExecuteAsync()     // returns IReadOnlyList<T>


ExecutableQuery(cypher)
	.ReturnObjects<T>()
	.UsingMapper<MyMapper>() // MyMapper is custom class that implements IRecordMapper<T> 
	                         // (creating this class would be the long way round)

	.ExecuteAsync()          // returns EagerResult<IReadOnlyList<T>> (or just the list?)

```

keep bothering with EagerResult? Will it ever contain useful info?

## Defining mapping

```c#
public class MyMappingProvider : IMappingProvider
{
	public void CreateMappers(IMappingRegistry registry)
	{
		registry
		    .RegisterMapping<Person>(builder => 
		    {
		    	builder

				    // simple mapping
				    .Map(x => x.Name, "name")        // would be good to have an analyzer on these Map calls
				    .Map(x => x.Height, "height")    // to flag if x.Height doesn't have a setter (to catch at
				                                     // compile time rather than run time)

				    // conversion of a field value
				    .Map(destination: x => x.Age, sourceKey: "age", converter: (long x) => (int)x) 

				    // direct access to the IRecord through a Func<IRecord>, in case properties of the record itself are needed
				    .Map(x => x.Postcode, r => r["address"].As<INode>()["postcode"].As<string>) 

				    // shorthand for the above
				    .Map(x => x.Postcode, "address.postcode")
		    })
		    .RegisterMapping<Movie>(builder => builder.ByConvention()) // maybe no need, this is used by default for an unknown type?
		    .RegisterMapping<Campaign, MyCampaignMapper>();            // MyCampaignMapper implements IRecordMapper<Campaign>
	}
}


// in driver startup

var driver = GraphDatabase.Driver(...);
driver.RegisterMappings<MyMappingProvider>();

// now the driver has all the mappings available (through a static AsyncLocal<Dictionary<Type, IRecordMapping>> variable)

// then do:
ExecutableQuery(cypher)
    .ReturnObjects<T>()
	.ExecuteAsync()          // returns IReadOnlyList<T>

```

In the case of `address.postcode`, we do something like:

* find the `address` field on the record
* does it have a property at the root called `postcode` (ie is `address` an object or dictionary)?
    * if yes, use that as the value
    * otherwise, is it an `INode` or a `Dictionary<string, object>`?
        * if yes, does the node have a `postcode` property or key?
            * if yes, use that as the value
* otherwise, no value can be found, throw an exception

## Convention based mapper

The convention based mapper will be an implementation of IRecordMapper<T> that uses the names of fields 
to intelligently do the mapping, making explicitly-coded mappers unnecessary 80% of the time.

```c#
public class Person
{
    public string Name { get; set; } // will be populated from "name" field, or "name" property on single record or object
    public int Age { get; set; } // conversion from long happens automatically

    [MappingPath("address.postcode")]
    public string Postcode; // overridden convention 
}
```

It will also need to do things like: 

```c#
public class Person
{
    // ...
    public List<string> Hobbies { get; set; } // if not overridden, will look at the 'hobbies' field for a list of strings

    // or
    [MappingPath("person.hobbies")]
    public List<string> Hobbies { get; set; } // will look at the 'person' field for a property called 'hobbies'
                                              // that is a list of strings
}
```

Maybe these lists would be better as arrays, or `IReadOnlyList`s.

## Making mapping simple

Also add stuff like:
```c#
IRecord r;
r.GetValue<string>("name")
r.GetNode("address").GetValue<string>("postcode")
r.GetValue<string>("address.postcode")
```
to make mapping building easier/more readable

## Nested objects

A class might have properties that are classes, which come from nodes/dicts on the record
or properties that are a list of classes

### classes
```c#
// something like:
    .Map(destination: x => x.Address, sourceKey: "CustomerAddress", nodeMapper: typeof(AddressMapper))

// or
    .Map(destination: x => x.Address, sourceKey: "CustomerAddress", nodeMapper: typeof(ConventionBasedNodeMapper<Address>))
```

### lists
```c#
//something like:
    .Map(x => x.Addresses, sourceKey: "Addresses", {some kind of mapper specified here*})
```

* depending on whether the items in the list are primitives, nodes, or dicts, some kind of mapper would need to be specified
it's possible that INodeMapper IS_A IValueMapper maybe 

## misc notes

`IRecordMapper<T>`
has method `T Map(IRecord record)`

`ConventionBasedRecordMapper<T> where T : new() : IRecordMapper<T>`

does fields to properties by name (case insensitive by default?)

## Methods of hydrating objects

### Code Generators 
While the technology does work quite nicely and transparently, the developer experience while working on them is awful. You basically have to have two IDEs open, one to develop the generator and one to use it. The one that uses it needs to be restarted every time you make a change to the generator. Also writing unit tests for the 
generators is really time consuming and involves large inline strings full of code, making the tests really brittle.

#### Analyzers
As part of this, I also wrote some analyzers and these are really promising - we can analyze code patterns and give the user hints or warnings, or even build errors. We could, for example, raise a compile error if the user tries to return the cursor from a transaction function.

### Reflection
The reason code generators were tried in the first place was because it was assumed that generated, compiled code would be faster at run time than populating the objects through reflection. However, in benchmarking there was little to no difference between the methods, and in fact the reflection method seemed to be faster(!). This gives a much easier developer experience, although the demo that I built was somewhat overcomplicated due to it caching lots of reflection information for performance. This would be better achieved through building up compiled delegates using the Expression API and calling those (which has the effect of executing identical code to simply setting the properties in C# code).
