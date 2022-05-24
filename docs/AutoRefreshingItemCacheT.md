# AutoRefreshingItemCache<T>
This type exists to enable caching of read-only and reference data with minimal effort. It uses delegates to 
refresh data to a predetermined schedule in the background, so that data is already sufficiently up to date 
whenever it is requested by the consumer.

This type is based upon the original InMemoryItemCache<T> but exists for simpler code where the consumer needs 
synchronous access to the data, whilst the data source exposes only async data access functionality. Async data 
access is fast becoming the norm, and designing new data access interfaces using synchronous methods feels out 
of step with modern development practices. However, consumers of cached data sometimes need to do so 
synchronously, especially where data is in use in or via an old API. 

We at DotNotStandard have encountered one such example with a Data Annotations attribute that we have developed 
as part of DotNotStandard.Validation. The contract we must fulfil as part of implementing a validation 
attribute - namely inheriting from the ValidationAttribute base class - does not allow support for async data 
access. However, we wanted to support use of async access to data sources as some data sources no longer support 
synchronous access methods - and we expect this to become more prevalent. AutoRefreshingItemCache<T> helps 
overcome this problem because it can perform async data access in the background to fill the in-memory cache with 
data, which can then be accessed via the cache's synchronous GetItem method.

As with InMemoryItemCache<T>, items returned from the cache are run through a cloning operation before being 
returned. It is important to understand this process and why it is performed, as you may need to change the 
cloner in use to have the caching behaviour more closely suit your needs.

# The Purpose of Cloning
An object in the cache is deep cloned as part of returning it to the consumer. This ensures that the instance 
provided is not associated with any other consumer, which ensures there are no threading issues as a result of 
consumers accessing a type that is not thread-safe on multiple threads concurrently.

Highly scalable hosts (such as ASP.NET Core) use multiple threads to concurrently handle multiple requests, but 
this can result in threading problems, such as race conditions and deadlocks that tie up threads indefinitely. 
InMemoryItemCache<T> sidesteps these issues by creating a new, separate instance of the cached item to each 
consumer.

Cloning has several important implications:
1. You must use an alternate cloner if you want to cache instances that can be changed. By default, we use this 
cache for read-only reference/lookup data, so this behaviour is appropriate for our purposes. Consider providing 
the NonCloningClonerFactory<T> for editable objects - but remember that there is then no guaranteed thread-safety.
2. The cache is not absolutely the fastest it could be by default. Again, consider providing the 
NonCloningClonerFactory<T> to the constructor if you want absolutely the best performance - but premember that 
this is at the expense of the thread safety we provide by default.
3. Unless you use a different cloner, the types cached must implement ICloneable. Again, you can overcome this 
limitation by changing the cloner in use from the default to another of the available implementations.

For reference, note that out of process caches apply the same cloning behaviour by default. If a cache is out of 
process then the item being cached must be serialised and sent to the other process; this serialisation is a form 
of cloning. All we do in our cache is replicate the same behaviour as you would experience with an out of process 
cache. However, as we are not sending the item to another process, our in-memory cache is faster. Cross-process 
operations are always slower than in-process operations, usually by several orders of magnitude.

# Changing the Cloner
You can use any cloner - including the original BinaryFormatter implementation, or indeed your own, custom 
implementation. Change the cloner by passing an instance of the factory used to create the cloner into the second 
parameter of the constructor of the AutoRefreshingItemCache<T> type as you new it up.

If you do not pass a factory to the constructor then an instance of CloneableClonerFactory<T> is created and used.

Several built in cloners are available (each with their own factory); alternatively you may provide your own if you 
wish.

# Available Cloner Factories
## CloneableClonerFactory<T>
The new CloneableCloner<T> makes use of the ICloneable interface to request that the cached object clone itself. 
From Version 2.x of the package this is the default cloner. The result of this is that, by default, all cached 
items are *expected* to implement the ICloneable interface, and use it to create a deep clone of themselves (the 
root object and all child objects, throughout the graph.)

If the item you wish to cache does not implement this interface then you must change the cloner in use by 
providing an alternative cloner factory. Do this by passing an alternative cloner factory into the constructor. 
See the section entitled 'Changing the Cloner' for more details.

## NonCloningClonerFactory<T>
As the name suggests, the NonCloningCloner<T> type does no cloning at all. This minimises the time spent 
retrieving an item from the cache, so it is highly performant. However, this cloner does nothing to guarantee 
the safety of the type that is cached - the cache returns the same instance to all callers, on all threads.

If you think that your cached type is thread-safe, or that it will not be retrieved on more than one thread at 
a time, then this cloner may be for you.

## BinaryFormatterClonerFactory<T>
The original cloner that uses BinaryFormatter is still available. Pass an instance of BinaryFormatterClonerFactory<T>
into the constructor of the InMemoryItemCache<T> type to switch back to using this version.

BinaryFormatter is disabled on .NET 5.0 and above, so you would need to reenable it to make use of this cloner. 
Although BinaryFormatter can be re-enabled on these newer versions of the runtime, this is generally not recommended 
as this may introduce security vulnerabilities into your applications. BinaryFormatter was deprecated because of the 
potential for introducing deserialization vulnerabilities into host applications.

> Reneabling and using BinaryFormatter introduces the potential for security vulnerabilities in your application. 
> It is important to fully understand the risk that reenabling BinaryFormatter poses if you intend to use it.

The following docs pages on the Microsoft website provide more details on the security issues from which 
BinaryFormatter suffers.

[https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/5.0/binaryformatter-serialization-obsolete](https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/5.0/binaryformatter-serialization-obsolete)

[https://docs.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide)
