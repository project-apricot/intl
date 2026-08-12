# ApricotFramework.Intl

[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.Intl.svg?label=ApricotFramework.Intl)](https://www.nuget.org/packages/ApricotFramework.Intl/)
[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.Intl.AspNetCore.svg?label=ApricotFramework.Intl.AspNetCore)](https://www.nuget.org/packages/ApricotFramework.Intl.AspNetCore/)
[![CI](https://github.com/project-apricot/intl/actions/workflows/ci.yml/badge.svg)](https://github.com/project-apricot/intl/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/project-apricot/intl/blob/main/LICENSE)

Message localization for .NET. Look a message up by id for a locale and substitute named
placeholders into it, over translation sources you supply.

`ApricotFramework.Intl` is the **zero-dependency** core.
`ApricotFramework.Intl.AspNetCore` adds dependency-injection wiring, request-based locale
resolution, JSON translation sources, and an `IStringLocalizer` bridge so Razor and DataAnnotations
localize from the same files.

## Install

```bash
dotnet add package ApricotFramework.Intl
```

For ASP.NET Core applications, install the integration package instead — it brings the core with it:

```bash
dotnet add package ApricotFramework.Intl.AspNetCore
```

## Usage

```csharp
// Program.cs
using ApricotFramework.Intl.AspNetCore;

builder.Services.AddIntlCore(builder.Configuration);
builder.Services.AddJsonTranslationSource("en-US", () => File.OpenRead("Translations/all.en.json"));
builder.Services.AddJsonTranslationSource("hy-AM", () => File.OpenRead("Translations/all.hy.json"));
```

```csharp
// anywhere IIntlService is injected
var hello = intl.Format("messages.hello");
var welcome = intl.Format("messages.welcome", new Dictionary<string, object?> { ["name"] = "Apricot" });
var explicitLocale = intl.Format("messages.hello", "hy-AM");
```

Registration order does not matter, as long as everything is registered before the container is
built.

Locale names are matched case-insensitively, and a locale with no entry is retried without its last
subtag before the fallback locale is tried — so `hy-AM` resolves against `hy` if that is what you
registered, and a matching language always beats the fallback language. See the
[documentation](https://projectapricot.dev) for the full resolution order.
