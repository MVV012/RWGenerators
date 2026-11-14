# RWGenerators

Includes some source generators to help with creating Rain World mods. All code is generated at compile time and included to your mod's dll file, so there is no need to add dependency to other mod or to include extra dll in plugins folder

### Installation

Install latest dll file from releases, put it in any folder and reference it from .csproj like this:
```xml
<ItemGroup>
  <Analyzer Include="{path to file}/RWGenerators.dll" />
</ItemGroup>
```
Also set LangVersion in .csproj to at least `latest`/`14.0`, and install [PolySharp](https://www.nuget.org/packages/PolySharp/)

### Usage

Use these attributes from `RWGenerators` namespace to generate code:
```cs
AttachField
AttachOptionalField
GenerateConfigAccessors
```
