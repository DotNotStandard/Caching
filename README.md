# Caching
Caching utilities, including in-memory caching for reference data, such as read-only lookup tables.

# Supported .NET Runtimes
## Version 1.x
Version 1.x of this package will only support running on .NET Core 3.1 or earlier. Internally, this 
version used BinaryFormatter to clone objects from the cache (to avoid the potential for threading issues.) 
BinaryFormatter is disabled by default in .NET 5.0 and above.

Although BinaryFormatter can be re-enabled on newer versions of the runtime, this is generally not recommended 
as this may introduce security vulnerabilities into your applications. BinaryFormatter was deprecated because 
of the potential for introducing deserialization vulnerabilities into host applications.

See the documentation on the InMemoryItemCache<T> type for more details on those vulnerabilities.

## Version 2.x
Version 2.x of the package no longer uses BinaryFormatter for cloning by default. Multiple cloners are now 
included in the package. CloneableCloner is the default cloner in version 2.0 onwards. This places a 
restriction on the types that can be cached by default. However, you can cache any type as long as you 
change the cloner in use to one that suits the type being cached. 

*The change in default cloner is a breaking change in many circumstances.*

For more discussion on cloning and its implications, see the documentation on the InMemoryItemCache<T> type.
