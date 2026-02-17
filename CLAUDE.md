# DnD Damage Calculator

A web application that receives D&D character information and calculates damage statistics (average, p25, p50, p75, p90, p95).

## Commands

```bash
dotnet build                     # Build the solution
dotnet test                      # Run all 53 tests
dotnet run --project src/DnDDamageCalc.Web  # Run the app (http://localhost:5082)
dotnet publish -c Release        # AOT-compiled release build
```

## Tech Stack

- **.NET 10** Minimal API with Native AOT (`PublishAot`, `CreateSlimBuilder`)
- **HTMX 2.0.4** for dynamic HTML — no Blazor, no JS frameworks
- **PicoCSS v2** (dark theme) for styling
- **Supabase PostgreSQL** via REST API (AOT-compatible, no SDK)
- **Supabase Auth** for Google OAuth authentication
- **System.Text.Json** with source generation for JSONB serialization
- **Scriban 5.10.0** for reusable HTML templates (with AOT trimming support)
- **SQLite** (for tests only) via `Microsoft.Data.Sqlite` (WAL mode, AOT-compatible)
- **xUnit** + `WebApplicationFactory<Program>` for testing

## Project Structure

```
src/DnDDamageCalc.Web/
├── Program.cs                          # Entry point: DB init, static files, endpoint mapping
├── DnDDamageCalc.Web.csproj            # .NET 10, AOT, Sqlite + protobuf-net + Scriban packages
├── Models/
│   └── Character.cs                    # Domain models with protobuf annotations
├── Data/
│   ├── ICharacterRepository.cs         # Repository abstraction for DI
│   ├── SupabaseCharacterRepository.cs  # HTTP-based Supabase implementation
│   ├── SqliteCharacterRepository.cs    # SQLite implementation (tests only)
│   ├── Database.cs                     # SQLite connection factory (tests only)
│   └── FormParser.cs                   # IFormCollection -> Character parsing
├── Html/
│   └── HtmlFragments.cs               # HTML fragment rendering (C# + Scriban templates)
├── Services/
│   ├── ITemplateService.cs            # Template service interface
│   └── TemplateService.cs             # Scriban template loader + cacher
├── Endpoints/
│   └── CharacterEndpoints.cs          # All character-related routes
├── Simulation/
│   └── DamageSimulator.cs             # Monte Carlo damage simulation engine
└── wwwroot/
    ├── index.html                      # Shell page: sidebar + form container
    └── templates/                      # Scriban template files (.scriban)

tests/DnDDamageCalc.Tests/
├── CharacterEndpointTests.cs           # Integration tests (WebApplicationFactory)
├── CharacterRepositoryTests.cs         # DB CRUD tests (temp SQLite file per test)
├── DamageSimulatorTests.cs             # Simulation logic unit tests
└── FormParserTests.cs                  # Form parsing edge cases
```

## Module Details

### Models (`Models/Character.cs`)

Domain model hierarchy:

```
Character        { Id, Name, List<CharacterLevel> }
CharacterLevel   { LevelNumber (1-20), List<Attack> }
Attack           { Name, HitPercent, CritPercent, MasteryVex, MasteryTopple,
                   TopplePercent, FlatModifier, List<DiceGroup> }
DiceGroup        { Quantity, DieSize (4/6/8/10/12/20) }
```

- `Character.Id` and `Character.Name` are columns in PostgreSQL/SQLite
- `Character.Levels` is serialized to JSONB (Supabase) or protobuf blob (SQLite tests)
- `CharacterLevel`, `Attack`, and `DiceGroup` are plain C# classes (no attributes for Supabase)
- Masteries are bool flags (Vex, Topple) — expandable for future weapon masteries
- `TopplePercent` is the chance the target fails the save when Topple mastery is active
- `FlatModifier` is per-attack (the "+3" in "2d6+1d4+3")

### Database & Repository (`Data/`)

**Production (Supabase)**:
- `ICharacterRepository` — Abstraction for DI and testing
- `SupabaseCharacterRepository` — HTTP client calling Supabase REST API
- PostgreSQL table: `characters (id, user_id, name, data JSONB, created_at, updated_at)`
- JSONB column stores `List<CharacterLevel>` serialized with System.Text.Json
- Row Level Security (RLS) policies enforce user isolation (`user_id = auth.uid()`)
- HttpClient with Authorization header using `SUPABASE_SERVICE_KEY`

**Tests (SQLite)**:
- `SqliteCharacterRepository` — Implements `ICharacterRepository` for tests
- `Database.cs` — Static class with `Configure()`, `CreateConnection()`, `Initialize()`
- Single denormalized table: `Characters (Id INTEGER PK, SupabaseUserId TEXT, Name TEXT, Data BLOB)`
- The `Data` column stores `List<CharacterLevel>` as protobuf-serialized binary blob
- WAL mode enabled on every connection for concurrency
- `Configure()` allows tests to redirect to temp databases

**Repository Methods**:
- `SaveAsync(Character, userId)`: INSERT or UPDATE (upsert) — serializes to JSONB/protobuf
- `GetByIdAsync(int, userId)`: SELECT + deserialize back to `List<CharacterLevel>`
- `ListAllAsync(userId)`: `SELECT Id, Name` only (no blob/JSONB deserialization)
- `DeleteAsync(int, userId)`: DELETE by Id and userId
- All methods are async and enforce user isolation

### Form Parser (`Data/FormParser.cs`)

- Parses `IFormCollection` into `Character` by scanning form keys with regex
- Form field naming convention: `level[{i}].attacks[{j}].name`, `level[{i}].attacks[{j}].dice[{k}].quantity`
- Handles non-contiguous indices (gaps from dynamic removals) via `SortedSet<int>`
- Checkbox fields: `"on"` = true, absent = false
- Uses `[GeneratedRegex]` for the level pattern (AOT-compatible)
- Trims whitespace from string values

### HTML Fragments & Templates (`Html/HtmlFragments.cs` + `Services/TemplateService.cs`)

HTML is rendered using a **hybrid approach**:
- **Complex fragments** (loops/conditionals): C# string interpolation in `HtmlFragments.cs`
- **Reusable simple templates**: Scriban `.scriban` files in `wwwroot/templates/`

#### Scriban Templates

Templates are loaded by `ITemplateService` (Scriban engine) at runtime and cached. This enables:
- Template composition and reusability
- Easy HTML editing without code recompilation (in development)
- AOT compatibility via `<TrimmerRootAssembly Include="Scriban" />` in .csproj

**Current Scriban templates** (in `wwwroot/templates/`):
- `clone-level-button.scriban` — "Clone Last Level" button HTML
- `clone-attack-button.scriban` — "Clone Last Attack" button (parameterized by level index)
- `validation-error.scriban` — Error message span (HTML-encoded message)
- `save-confirmation.scriban` — Success confirmation message (HTML-encoded character name)
- `login-page.scriban` — Full login page HTML
- `character-list.scriban` — Sidebar character list (iterates items, HTML-escaped names)
- `dice-group-fragment.scriban` — Dice quantity/die selector row (parameterized indices & values)

**Hybrid fragments** (remain in C#):
- `CharacterForm(Character?, ITemplateService?)` — full form with embedded CloneLevelButton template call
- `LevelFragment(levelIndex, CharacterLevel?, ITemplateService?)` — level card with embedded CloneAttackButton template call
- `AttackFragment(levelIndex, attackIndex, Attack?)` — attack fieldset (loops over DiceGroups in C#)
- `DamageResultsGraph(List<LevelStats>)` — SVG graph with mathematical rendering (complex calculations)

**Template Usage Pattern:**
```csharp
// In endpoints (with DI):
app.MapGet("/login", (ITemplateService templates) =>
    Results.Text(HtmlFragments.LoginPage(templates), "text/html"));

// In HtmlFragments (optional parameter for fallback):
public static string ValidationError(string message, ITemplateService templates) =>
    templates.Render("validation-error", new { message });

// Fallback for tests (without service):
if (templates is null) { /* return hardcoded HTML */ }
```

**Template Variable Naming:**
- Use `snake_case` for template variables: `{{ message }}`, `{{ level_index }}`
- Use `{{ variable | html.escape }}` to prevent XSS
- Use Scriban conditionals: `{{ if condition }}...{{ end }}`
- Use Scriban loops: `{{ for item in items }}...{{ end }}`

Key methods:
- `CharacterForm(Character?)` — full form with hidden monotonic counters; level buttons in flex row with conditional clone button
- `LevelFragment(levelIndex, CharacterLevel?)` — single level card with attacks container
- `AttackFragment(levelIndex, attackIndex, Attack?)` — attack fieldset with hit/crit/masteries/topple%/damage
- `DiceGroupFragment(levelIndex, attackIndex, diceIndex, DiceGroup?)` — inline dice row (qty + die size + remove)
- `CloneLevelButton()` → **Scriban template**
- `CharacterList(List<(int,string)>)` → **Scriban template**
- `DamageResultsGraph(List<LevelStats>)` — damage statistics table with percentiles
- `ValidationError(string)` → **Scriban template**
- `SaveConfirmation(int, string)` → **Scriban template**

Counter management: monotonic hidden fields (`levelCounter`, `attackCounter`, `diceCounter`) that only increment, updated via `hx-swap-oob="true"` on add responses. This avoids index collisions when elements are removed.

### Endpoints (`Endpoints/CharacterEndpoints.cs`)

Extension method `MapCharacterEndpoints()` registers all routes:

| Method | Route | Purpose | HTMX swap |
|--------|-------|---------|-----------|
| GET | `/character/form` | Empty form | innerHTML into container |
| GET | `/character/{id}` | Load saved character | innerHTML into container |
| GET | `/character/list` | List saved characters | innerHTML into sidebar |
| POST | `/character/level/add` | Add level fragment | beforeend + OOB counter + OOB clone btn |
| POST | `/character/level/clone` | Clone last level (incremented number) | beforeend + OOB counters + OOB clone btn |
| POST | `/character/attack/add` | Add attack fragment | beforeend + OOB counter |
| POST | `/character/save` | Save to SQLite | innerHTML (form + message) |
| DELETE | `/character/{id}` | Delete character | updated sidebar list |
| POST | `/character/validate-percentages` | Validate hit%+crit% <= 100 | inline error |
| POST | `/character/calculate` | Run damage simulation | innerHTML into results div |

**Note**: Level, attack, and dice operations (add/remove) are handled entirely client-side via JavaScript functions and do not require server endpoints.

Server-side validation on save: name required, levels 1-20, attack name required, percentages 0-100, hit%+crit% <= 100.

### Simulation (`Simulation/DamageSimulator.cs`)

Monte Carlo damage simulation engine (10,000 iterations per level by default).

- `LevelStats` — result DTO with LevelNumber, Average, P25, P50, P75, P90, P95
- `DamageSimulator.Simulate(Character, iterations)` — iterates each level, simulates turns, returns sorted percentile stats
- `SimulateTurn(attacks)` — processes attacks sequentially within a turn, tracking Vex/Topple state
- **Vex mastery**: on a miss, grants advantage (roll twice) to the next attack; consumed after one use
- **Topple mastery**: on a hit/crit, target makes a save (TopplePercent chance to fail); prone grants advantage to all subsequent attacks for the turn
- **Advantage**: recalculates effective hit/crit rates using `1 - (1-p)^2` formula, preserving crit/hit ratio
- **Crits**: double dice quantity only, flat modifier applied once
- Percentile calculation uses linear interpolation on sorted damage arrays

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
- **No EF Core** — Direct HTTP calls to Supabase REST API for AOT compatibility
- **No JS frameworks** — vanilla JS only where absolutely necessary (sidebar toggle)
- **Native AOT** must compile (`dotnet publish -c Release`)
- HTML rendering via C# string interpolation, not Razor/template engines
- **Dependency Injection** — Use `ICharacterRepository` for testability

### Validation Strategy (3 layers)

1. **HTML5 attributes**: `min`, `max`, `required` on inputs
2. **HTMX on-change**: hit%/crit% inputs trigger `/character/validate-percentages`, inline error display
3. **Server on save**: full validation before persisting; return form with errors if invalid

### Data Persistence

**Production (Supabase)**:
- Single `characters` table with user_id, name, and JSONB data column
- JSONB stores `List<CharacterLevel>` using System.Text.Json
- Row Level Security (RLS) enforces user isolation at database level
- `ListAllAsync()` never reads JSONB column — lightweight listing
- HttpClient-based REST API calls (no SDK for AOT compatibility)

**Tests (SQLite)**:
- Single `Characters` table with Id, SupabaseUserId, Name, and protobuf blob
- Protobuf-net serializes `List<CharacterLevel>` for backward compatibility with existing tests
- Tests use `SqliteCharacterRepository` via `CustomWebApplicationFactory`
- Fast, isolated, no network calls

### Testing Conventions

- Integration tests use `CustomWebApplicationFactory` with `IClassFixture`
- Factory injects `SqliteCharacterRepository` instead of Supabase
- Repository tests use temp SQLite files with unique GUIDs, cleaned up in `Dispose()`
- Both test classes use `[Collection("Database")]` to prevent parallel execution
- Call `SqliteConnection.ClearAllPools()` before deleting temp DB files (WAL mode keeps handles)
- `public partial class Program { }` in Program.cs exposes entry point to test infrastructure
- **No Supabase calls in tests** — fast, isolated, repeatable
- Mock authentication via `TestUserId` configuration setting

### HTMX Patterns

- Fragment add endpoints return HTML + an `hx-swap-oob="true"` hidden input to update the monotonic counter, **except for client-side operations**
- Save returns confirmation message + full re-rendered form
- Percentage validation returns inline error or empty string
- **OOB visibility pattern**: clone button wrapped in `<span id="clone-level-btn">`, shown/hidden via `hx-swap-oob="innerHTML"` from add-level and clone endpoints
- **Clone endpoint** receives full form via `hx-include="#character-form"`, parses with `FormParser`, updates all three counters (level, attack, dice) via OOB to prevent index collisions with cloned content
- **Client-side operations**: All add/remove operations for levels, attacks, and dice are now pure JavaScript for instant feedback:
  - **Level removal**: `removeLevel(levelId)` - removes DOM element and renumbers remaining levels
  - **Attack removal**: `removeAttack(attackId)` - removes DOM element instantly
  - **Dice add**: `addDice(levelIndex, attackIndex)` - creates new dice group with monotonic counter
  - **Dice removal**: `removeDice(diceId)` - removes DOM element instantly

### PicoCSS Conventions

- Do NOT use `role="group"` on `<fieldset>` — PicoCSS treats it as an inline input group, making child inputs non-interactive
- Use `<article>` for card-like containers (levels, character info)
- Use `.grid` class for side-by-side inputs
- Use CSS custom properties (`--pico-*`) for theming consistency
