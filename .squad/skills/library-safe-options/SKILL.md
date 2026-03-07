---
name: "library-safe-options"
description: "Pattern for adding configuration options to a .NET class library without requiring a DI container"
domain: "dotnet-library-design"
confidence: "high"
source: "Sage configuration modernization assessment (ripley, 2026-07-15)"
---

## Context

When modernizing configuration in a **.NET class library** (not an application), you cannot assume the consumer uses a DI container, `IOptions<T>`, or `Microsoft.Extensions.Configuration`. The library must work with plain `new` while also being DI-friendly.

This pattern was developed for the Sage simulation library but applies to any .NET class library removing `System.Configuration.ConfigurationManager` dependencies.

## The Pattern

### 1. Define POCO Options Classes

Plain C# classes with public get/set properties and sensible defaults. No dependencies on any framework.

```csharp
public sealed class MyLibraryOptions
{
    public int MaxRetries { get; set; } = 3;
    public bool EnableLogging { get; set; } = false;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

**Rules:**
- `sealed` class (no inheritance complexity)
- All properties have defaults matching current/safe behavior
- No constructor parameters — must be `new`-able with zero args
- No DI attributes, no `IOptions<T>`, no framework types
- Place in the same namespace as the consuming class

### 2. Accept Options via Optional Constructor Parameters

```csharp
// Library consumer can use: new MyService() — gets defaults
// Or: new MyService(new MyServiceOptions { MaxRetries = 5 })
public class MyService
{
    private readonly MyServiceOptions _options;

    public MyService(MyServiceOptions options = null)
    {
        _options = options ?? new MyServiceOptions();
    }
}
```

**Rules:**
- Parameter type is the concrete POCO, not `IOptions<T>`
- Default value is `null`, coalesced to `new Options()` in the body
- This is **not a breaking change** — existing callers using `new MyService()` still compile

### 3. For Singletons / Static Classes: Use Static `Configure()` Method

```csharp
public static class DiagnosticAids
{
    private static DiagnosticsOptions _options = new();

    public static void Configure(DiagnosticsOptions options)
    {
        _options = options ?? new DiagnosticsOptions();
    }

    public static bool IsEnabled(string key) =>
        _options.Flags.TryGetValue(key, out bool v) && v;
}
```

**Rules:**
- Static `Configure()` is optional-call — library works without it
- Document "call before first use" semantics
- Thread safety: simple field assignment is atomic for reference types; if more complex initialization is needed, use `Interlocked` or lock

### 4. DI Extension Methods (Optional, Separate Concern)

Only if the library wants to provide first-class DI support. Can be in the same assembly or a separate one.

```csharp
// Optional — consumers who use DI can call this
public static class MyLibServiceCollectionExtensions
{
    public static IServiceCollection AddMyLib(
        this IServiceCollection services,
        Action<MyLibraryOptions> configure = null)
    {
        var options = new MyLibraryOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddTransient<MyService>();
        return services;
    }
}
```

**Rules:**
- Do NOT make `Microsoft.Extensions.DependencyInjection` a hard dependency of the core library
- Either put this in a separate package (e.g., `MyLib.Extensions.DependencyInjection`) or guard with `#if` / conditional compilation
- The extension method creates the POCO and registers it — the library classes receive the plain POCO, not `IOptions<T>`

## Anti-Patterns

- **Don't use `IOptions<T>` in library constructors** — forces a dependency on `Microsoft.Extensions.Options` for all consumers, even those not using DI
- **Don't read from ambient configuration** (`ConfigurationManager`, environment variables, `IConfiguration`) inside the library — the library doesn't own its host's config
- **Don't use static mutable state as the only configuration path** — prefer instance-level options passed via constructors. Use static `Configure()` only for genuinely global/static services
- **Don't require `Configure()` to be called** — the library must work with zero configuration

## Migration Checklist (from ConfigurationManager)

1. Inventory all `ConfigurationManager.GetSection()` / `AppSettings` calls
2. For each call site, document: what keys are read, what defaults exist, where they're consumed
3. Group related keys into POCO options classes (one per subsystem/concern)
4. Set POCO defaults to match current no-config-file behavior
5. Add optional parameter to constructors (or static `Configure()` for singletons)
6. Replace config reads with options property reads
7. Remove `System.Configuration.ConfigurationManager` package reference
8. Run all tests — behavior with no config should be identical

## Examples

### Before (library reads from host's app.config)
```csharp
public class Executive
{
    public Executive(Guid id)
    {
        var nvc = (NameValueCollection)ConfigurationManager.GetSection("Sage");
        _maxThreads = nvc != null ? int.Parse(nvc["WorkerThreads"]) : 900;
    }
}
```

### After (library accepts options POCO)
```csharp
public class Executive
{
    public Executive(Guid id, ExecutiveOptions options = null)
    {
        options ??= new ExecutiveOptions();
        _maxThreads = options.MaxWorkerThreads; // default: 900
    }
}
```

Consumer (no DI):
```csharp
var exec = new Executive(Guid.NewGuid()); // uses defaults
var exec2 = new Executive(Guid.NewGuid(), new ExecutiveOptions { MaxWorkerThreads = 50 });
```

Consumer (with DI):
```csharp
services.AddSingleton(new ExecutiveOptions { MaxWorkerThreads = 50 });
services.AddTransient<Executive>();
```

## Confirmation (2026-07-15)

- Applied successfully during the ConfigurationManager → POCO options migration in Sage; defaults preserved and tests passing.
