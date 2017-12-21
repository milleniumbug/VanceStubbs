``` ini

BenchmarkDotNet=v0.10.11, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.125)
Processor=Intel Core i7-7700K CPU 4.20GHz (Kaby Lake), ProcessorCount=8
  [Host]     : .NET Framework 4.6.2 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.2600.0
  DefaultJob : .NET Framework 4.6.2 (CLR 4.0.30319.42000), 32bit LegacyJIT-v4.7.2600.0


```
|      Method |     Mean |     Error |    StdDev | Scaled | ScaledSD |
|------------ |---------:|----------:|----------:|-------:|---------:|
|       Proxy | 2.120 us | 0.0369 us | 0.0327 us |   1.78 |     0.03 |
| Boilerplate | 1.189 us | 0.0103 us | 0.0092 us |   1.00 |     0.00 |
