# DnD Damage Calculator

A web application that receives D&D character information and calculates damage statistics (average, p25, p50, p75, p90, p95).

## Commands

```bash
dotnet build                     # Build the solution
dotnet test                      # Run all 35 tests
dotnet run --project src/DnDDamageCalc.Web  # Run the app (http://localhost:5082)
dotnet publish -c Release        # AOT-compiled release build
```

## Tech Stack

- **.NET 10** Minimal API with Native AOT (`PublishAot`, `CreateSlimBuilder`)
- **HTMX 2.0.4** for dynamic HTML — no Blazor, no JS frameworks
- **PicoCSS v2** (dark theme) for styling
- **SQLite** via `Microsoft.Data.Sqlite` (WAL mode, AOT-compatible, no EF Core)
- **protobuf-net** for binary serialization of character data blobs
- **xUnit** + `WebApplicationFactory<Program>` for testing

## Project Structure

```
src/DnDDamageCalc.Web/
├── Program.cs                          # Entry point: DB init, static files, endpoint mapping
├── DnDDamageCalc.Web.csproj            # .NET 10, AOT, Sqlite + protobuf-net packages
├── Models/
│   └── Character.cs                    # Domain models with protobuf annotations
├── Data/
│   ├── Database.cs                     # SQLite connection factory + schema init
│   ├── CharacterRepository.cs          # CRUD with protobuf blob serialization
│   └── FormParser.cs                   # IFormCollection -> Character parsing
├── Html/
│   └── HtmlFragments.cs               # All HTML fragment rendering (C# string interpolation)
├── Endpoints/
│   └── CharacterEndpoints.cs          # All character-related routes
└── wwwroot/
    └── index.html                      # Shell page: sidebar + form container

tests/DnDDamageCalc.Tests/
├── CharacterEndpointTests.cs           # Integration tests (WebApplicationFactory)
├── CharacterRepositoryTests.cs         # DB CRUD tests (temp SQLite file per test)
└── FormParserTests.cs                  # Form parsing edge cases
```

## Module Details

### Models (`Models/Character.cs`)

Domain model hierarchy:

```
Character        { Id, Name, List<CharacterLevel> }
CharacterLevel   { LevelNumber (1-20), List<Attack> }
Attack           { Name, HitPercent, CritPercent, MasteryVex, MasteryTopple,
                   FlatModifier, List<DiceGroup> }
DiceGroup        { Quantity, DieSize (4/6/8/10/12/20) }
```

- `Character.Id` and `Character.Name` are SQL columns; everything else is serialized to a protobuf blob
- `CharacterLevel`, `Attack`, and `DiceGroup` are annotated with `[ProtoContract]`/`[ProtoMember(n)]`
- Masteries are bool flags (Vex, Topple) — expandable for future weapon masteries
- `FlatModifier` is per-attack (the "+3" in "2d6+1d4+3")

### Database (`Data/Database.cs`)

- Static class with `Configure(connString)`, `CreateConnection()`, `Initialize()`
- Single denormalized table: `Characters (Id INTEGER PK, Name TEXT, Data BLOB)`
- The `Data` column stores `List<CharacterLevel>` as a protobuf-serialized binary blob
- WAL mode enabled on every connection for concurrency
- `Configure()` allows tests to redirect to temp databases

### Repository (`Data/CharacterRepository.cs`)

- `Save(Character)`: INSERT or UPDATE (upsert) — serializes `character.Levels` to protobuf blob
- `GetById(int)`: SELECT + deserialize blob back to `List<CharacterLevel>`
- `ListAll()`: `SELECT Id, Name` only (no blob deserialization)
- `Delete(int)`: simple DELETE by Id
- All methods create and dispose their own connections

### Form Parser (`Data/FormParser.cs`)

- Parses `IFormCollection` into `Character` by scanning form keys with regex
- Form field naming convention: `level[{i}].attacks[{j}].name`, `level[{i}].attacks[{j}].dice[{k}].quantity`
- Handles non-contiguous indices (gaps from dynamic removals) via `SortedSet<int>`
- Checkbox fields: `"on"` = true, absent = false
- Uses `[GeneratedRegex]` for the level pattern (AOT-compatible)
- Trims whitespace from string values

### HTML Fragments (`Html/HtmlFragments.cs`)

All HTML rendered via C# `$"""..."""` raw string interpolation (AOT-safe, no template engine).

Key methods:
- `CharacterForm(Character?)` — full form with hidden monotonic counters
- `LevelFragment(levelIndex, CharacterLevel?)` — single level card with attacks container
- `AttackFragment(levelIndex, attackIndex, Attack?)` — attack fieldset with hit/crit/masteries/damage
- `DiceGroupFragment(levelIndex, attackIndex, diceIndex, DiceGroup?)` — inline dice row (qty + die size + remove)
- `CharacterList(List<(int,string)>)` — sidebar character items
- `ValidationError(string)` / `SaveConfirmation(int, string)` — feedback messages

Counter management: monotonic hidden fields (`levelCounter`, `attackCounter`, `diceCounter`) that only increment, updated via `hx-swap-oob="true"` on add responses. This avoids index collisions when elements are removed.

### Endpoints (`Endpoints/CharacterEndpoints.cs`)

Extension method `MapCharacterEndpoints()` registers all routes:

| Method | Route | Purpose | HTMX swap |
|--------|-------|---------|-----------|
| GET | `/character/form` | Empty form | innerHTML into container |
| GET | `/character/{id}` | Load saved character | innerHTML into container |
| GET | `/character/list` | List saved characters | innerHTML into sidebar |
| POST | `/character/level/add` | Add level fragment | beforeend + OOB counter |
| POST | `/character/attack/add` | Add attack fragment | beforeend + OOB counter |
| POST | `/character/dice/add` | Add dice group | beforeend + OOB counter |
| DELETE | `/character/level/remove` | Remove level | outerHTML (empty) |
| DELETE | `/character/attack/remove` | Remove attack | outerHTML (empty) |
| DELETE | `/character/dice/remove` | Remove dice group | outerHTML (empty) |
| POST | `/character/save` | Save to SQLite | innerHTML (form + message) |
| DELETE | `/character/{id}` | Delete character | updated sidebar list |
| POST | `/character/validate-percentages` | Validate hit%+crit% <= 100 | inline error |

Server-side validation on save: name required, levels 1-20, attack name required, percentages 0-100, hit%+crit% <= 100.

### Frontend (`wwwroot/index.html`)

- **Layout**: flexbox with collapsible 280px sidebar + main content area
- **Sidebar**: character list loaded on page load, "+ New Character" button, delete (x) per character
- **Toggle**: hamburger button (fixed position) toggles `.collapsed` class with CSS transition
- **Responsive**: sidebar overlays on mobile (<768px) with drop shadow
- **Theme**: PicoCSS dark theme (`data-theme="dark"`)
- **No custom JS** except sidebar toggle — all interactivity via HTMX attributes

## Architecture Rules

### General

- **No Blazor** — HTMX only for interactivity
- **No EF Core** — raw SQL with `Microsoft.Data.Sqlite` for AOT compatibility
- **No JS frameworks** — vanilla JS only where absolutely necessary
- **Native AOT** must compile (`dotnet publish -c Release`)
- HTML rendering via C# string interpolation, not Razor/template engines

### Validation Strategy (3 layers)

1. **HTML5 attributes**: `min`, `max`, `required` on inputs
2. **HTMX on-change**: hit%/crit% inputs trigger `/character/validate-percentages`, inline error display
3. **Server on save**: full validation before persisting; return form with errors if invalid

### Data Persistence

- Single `Characters` table with Id, Name, and a protobuf blob for all nested data
- Only `List<CharacterLevel>` is serialized to the blob; Id and Name are SQL columns
- `ListAll()` never reads the blob — lightweight listing

### Testing Conventions

- Integration tests use `WebApplicationFactory<Program>` with `IClassFixture`
- Repository tests use temp SQLite files with unique GUIDs, cleaned up in `Dispose()`
- Both test classes use `[Collection("Database")]` to prevent parallel execution (shared static `Database` state)
- Call `SqliteConnection.ClearAllPools()` before deleting temp DB files (WAL mode keeps handles)
- `public partial class Program { }` in Program.cs exposes entry point to test infrastructure

### HTMX Patterns

- Fragment add endpoints return HTML + an `hx-swap-oob="true"` hidden input to update the monotonic counter
- Remove endpoints return empty string (outerHTML swap deletes the element)
- Save returns confirmation message + full re-rendered form
- Percentage validation returns inline error or empty string

### PicoCSS Conventions

- Do NOT use `role="group"` on `<fieldset>` — PicoCSS treats it as an inline input group, making child inputs non-interactive
- Use `<article>` for card-like containers (levels, character info)
- Use `.grid` class for side-by-side inputs
- Use CSS custom properties (`--pico-*`) for theming consistency
