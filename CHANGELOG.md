# Neo4j dotnet Driver Change Log

## Version 5.0
- Change Transaction config timeout to accept null, Timeout.Zero explicitly.
    - Zero removes timout.
    - null uses server default timeout.
- Change Bookmark to Bookmarks.
    - replace all uses with pluralized Bookmarks.