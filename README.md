[![Build status](https://ci.appveyor.com/api/projects/status/r526dq5gpud5tb6y?svg=true)](https://ci.appveyor.com/project/micdenny/strathweb-cacheoutput-webapi2-appfabric)

# DO NOT USE THIS LIBRARY!

This library is in pre-pre-pre-release, and does not work because of an issue found on the [base library](https://github.com/filipw/AspNetWebApi-OutputCache) that tries to cache a non-serializeble object. I'm working on it.

# AppFabric implementation of ASP.NET Web API CacheOutput

Extension to [Strathweb CacheOutput WebApi2](https://github.com/filipw/AspNetWebApi-OutputCache) for Microsoft AppFabric Distributed Caching

Installation
--------------------
You can build from the source here, or you can install the [Nuget version](https://www.nuget.org/packages/Strathweb.CacheOutput.WebApi2.AppFabric/):

For Web API 2 (.NET 4.5)
    
    PM> Install-Package Strathweb.CacheOutput.WebApi2.AppFabric
    
Usage
--------------------

You can register your implementation using a handy *GlobalConfiguration* extension method:

```csharp
//instance
GlobalConfiguration.Configuration.CacheOutputConfiguration().RegisterCacheOutputProvider(() => new MyCache());

//singleton
var cache = new MyCache();
GlobalConfiguration.Configuration.CacheOutputConfiguration().RegisterCacheOutputProvider(() => cache);	
```

If you prefer **CacheOutput** to use resolve the cache implementation directly from your dependency injection provider, that's also possible. Simply register your *IApiOutputCache* implementation in your Web API DI and that's it. Whenever **CacheOutput** does not find an implementation in the *GlobalConiguration*, it will fall back to the DI resolver. Example (using Autofac for Web API):

```csharp
cache = new MyCache();
var builder = new ContainerBuilder();
builder.RegisterInstance(cache);
GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(builder.Build());
```

If no implementation is available in neither *GlobalConfiguration* or *DependencyResolver*, we will default to *System.Runtime.Caching.MemoryCache*.
