# Caching
Caching utilities, including in-memory caching for reference data, such as read-only lookup tables.

# Supported .NET Runtimes
The current version of this software will only support running on .NET Core 3.1 or earlier at this time. 
Internally, this software uses BinaryFormatter to clone objects from the cache (to avoid the potential for 
threading issues.) BinaryFormatter is disabled by default in .NET 5.0 and above.

Although BinaryFormatter can be re-enabled on newer versions of the runtime, this is generally not recommended 
as this may introduce security vulnerabilities into your applications. BinaryFormatter was deprecated because 
of the potential for introducing deserialization vulnerabilities into host applications.

The following docs pages on the Microsoft website provide more details.

[https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/5.0/binaryformatter-serialization-obsolete](https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/5.0/binaryformatter-serialization-obsolete)

[https://docs.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide)

I intend to introduce a replacement for the package in the future for use in CSLA applications. This newer
version will make use of CSLA's own cloning functionality isntead of using BinaryFormatter. This is on the 
backlog awaiting prioritisation.