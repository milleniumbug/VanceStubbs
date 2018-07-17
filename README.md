VanceStubbs [![Build status](https://ci.appveyor.com/api/projects/status/w2k237to1bh6hqon?svg=true)](https://ci.appveyor.com/project/milleniumbug/vancestubbs)
======

Reflection.Emit-based library that provides stubs, proxies, AOP and magic.

[NuGet](https://www.nuget.org/packages/VanceStubbs/1.0.0)

API
-----

Given a `var factory = new VanceStubbs.Factory();`:

- `factory.OfStubs.WhiteHole<T>()` - creates an instance of `T` that implements every abstract method and property by throwing a `NotImplementedException`
- `factory.OfStubs.BlackHole<T>()` - creates an instance of `T` that implements every abstract method and property by returning a `default` and otherwise doing nothing.
- `factory.OfProxies.NotifyChangedProxy<T>(args)` - creates an instance of `T` (with the passed constructor arguments) that implements every abstract property as a notifying property with a backing field.
- `factory.OfProxies.For<T>()` - creates an AOP proxy type builder.
- `new TypeDictionary<Value>(enumerableOfTypeToValueMappings)` - creates a `IReadOnlyDictionary<Type, Value>` that uses overload resolution rules to match a most specific type. (warning: passing a `typeof(AbstractNonDefaultConstructibleClass)` as a key doesn't currently work. This will change in the future)


Memory usage
------

All instances can be created either through a static factory or non-static factories. Each dynamically created type is never freed if you use a static factory, therefore it's highly recommended to use a non-static factory if you want to control over memory usage.

*Implementation detail*: A `VanceStubbs.Factory` composes over an `AssemblyBuilder` created with `RunAndCollect`. These assemblies will be collected if there's no reference to a factory or dynamically created object instances.



Name...?
--------

[Vance Stubbs from WH40K: Dawn of War: Soulstorm](https://1d4chan.org/wiki/Vance_Motherfucking_Stubbs)
