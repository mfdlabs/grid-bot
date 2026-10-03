# ENHANCEMENT: Move Configuration system to Microsoft.Extensions.Options

## Current Issue

The grid-bot configuration system uses a custom `BaseSettingsProvider` abstraction built on `VaultProvider` that, while functional, has several architectural limitations that affect code quality, testability, and maintainability.

## Current System Architecture

The application currently uses:

- **15 `BaseSettingsProvider` classes** (Discord, Grid, Web, Backtrace, Consul, etc.) that read configuration from Vault and environment variables
- **Singleton registration via reflection** (`AddSettingsProviders()`) that discovers and registers settings at runtime
- **Constructor injection** of concrete settings instances into consumers
- **Live reload via `INotifyPropertyChanged`** for dynamic Vault updates (detected via ~10-minute refresh intervals per provider)
- **Manual re-init logic** in services that need to react to configuration changes (e.g., `LocalConsulClientProvider` regenerates Consul client on address change)

### Key Design Decisions (Context)

- `SettingName` attribute convention exists for backward compatibility with existing environment variable naming (e.g., `CONSUL_ADDR`) and to avoid namespace collisions when multiple providers use env vars
- `BacktraceToken` hardcoded default is intentional—it's a leak-tracing token from when the app was originally closed-source
- `ClientSettings` system is orthogonal—it's a separate mechanism for delivering configuration to internal grid-server/RCC subprocess
- Configuration changes are **rare in production**, but **frequent in staging** when testing new feature gates or system implementations
- Direct Vault updates (not via Settings command) should be detected and picked up on next refresh cycle

## Architectural Flaws

### 1. **Lazy Validation — Failures at Runtime**
- Required settings (e.g., Discord token) throw only when their getter is accessed, not at startup
- Configuration errors discovered during runtime, potentially hours after deployment
- Harder to debug runtime crashes caused by missing configuration

### 2. **Multiple `BuildServiceProvider()` Calls — Fragmented DI Container**
- Setup code calls `BuildServiceProvider()` multiple times during service collection configuration
- Creates multiple singleton instances instead of a single managed container
- Breaks DI semantics; services registered after first build are not accessible to earlier builds
- Memory leaks and unexpected behavior from "singletons"

### 3. **No Centralized Validation Orchestration**
- 15+ settings classes validate independently (or throw on access)
- No single point to verify all configuration at startup
- Inconsistent validation patterns across settings classes

### 4. **Reflection-Based Settings Discovery**
- `AddSettingsProviders()` uses runtime reflection to find all `BaseSettingsProvider` implementations
- Slow startup (reflection overhead)
- Opaque registration—difficult to debug which providers were actually loaded
- No explicit control over registration order

### 5. **Custom Live-Reload Pattern — Non-Standard**
- `INotifyPropertyChanged` pattern differs from ASP.NET Core conventions
- Manual subscription logic scattered across consumers
- Each service implements its own change-detection and re-init logic
- Hard to test configuration change scenarios

### 6. **Type Conversion Inconsistency**
- Some types throw on conversion failure (e.g., `TimeSpan`)
- Others silently fall back to `null` or default value
- No predictable error handling across the system

### 7. **Testing Friction**
- Hard to mock settings in unit tests
- No built-in way to override configuration per test
- Change scenarios difficult to reproduce and verify

### 8. **Vault Refresh Logic Tightly Coupled**
- Refresh polling and Vault synchronization baked into every provider
- Difficult to test Vault interaction independently
- Hard to change refresh behavior without touching provider classes

## Solution: Microsoft.Extensions.Options Pattern

Migrate to the standard `Microsoft.Extensions.Options` pattern to:

- **Move to standard .NET configuration** via `IConfiguration` + `IConfigurationProvider`
- **Centralize validation** with `IValidateOptions<T>` validators per settings class
- **Support per-provider refresh intervals** via custom `VaultConfigurationProvider`
- **Maintain environment variable precedence** (env vars override Vault)
- **Enable testable change handling** with `IOptionsMonitor<T>.OnChange()` callbacks
- **Explicit service registration** replacing reflection-based discovery
- **Single, managed DI container** throughout application startup

## Migration Approach

### Phase 1: Code Refactoring (Low Risk)
1. Create POCO `Options` classes for each settings provider
   - No inheritance, no `INotifyPropertyChanged`
   - Direct properties matching current settings
   - Example: `DiscordOptions`, `GridOptions`, `WebOptions`, etc.

2. Implement `IValidateOptions<T>` validators for each settings class
   - Move validation logic from lazy getters to centralized validators
   - Run all validations at startup, fail fast with clear error messages
   - Example: `DiscordOptionsValidator` checks that token is non-empty

3. Create `VaultConfigurationProvider : IConfigurationProvider`
   - Handles Vault reads with per-provider refresh intervals
   - Maintains environment variable precedence
   - Maps Vault paths to configuration key structure
   - Raises `IChangeToken` on updates for `IOptionsMonitor<T>` support

4. Create extension methods for DI registration
   - `AddGridBotOptions(IServiceCollection, IConfiguration)`
   - Registers all Options classes and validators explicitly
   - Replaces reflection-based `AddSettingsProviders()`

5. Deploy alongside current system with feature flag
   - Old `BaseSettingsProvider` remains functional
   - New Options-based system runs in parallel
   - Can be tested and validated before cutover

### Phase 2: Update Consumers (Low-Medium Risk)
1. Refactor services to use `IOptions<T>` or `IOptionsMonitor<T>`
   - Static settings → `IOptions<T>`
   - Settings that require re-init on change → `IOptionsMonitor<T>`
   - Example: `DiscordClientService` subscribes to Discord token changes

2. Define change-handling pattern
   - Centralized in service constructor or initialization method
   - Pattern: `optionsMonitor.OnChange((options, name) => Reinitialize(options))`
   - Easy to test by mocking `IOptionsMonitor<T>`

3. Update service registration
   - Explicit: services registered by name, not discovered
   - Order-independent: no need for multiple `BuildServiceProvider()` calls

### Phase 3: Vault Data Migration (Critical Path)
1. Create Vault migration tool (CLI or utility class) that:
   - Reads secrets from current Vault structure
   - Transforms to flat key-value structure expected by `IConfiguration`
   - Example transformation:
     ```
     Before:  vault kv get secret/grid-bot-settings/production/discord
              Returns: { "Token": "...", "GuildId": 123 }
     
     After:   Vault stores flat keys under configuration path
              Discord:Token, Discord:GuildId, Grid:ImageName, etc.
     ```
   - Validates schema matches new Options classes
   - Outputs migration script (Terraform/vault CLI/audit trail)
   - Supports rollback if needed

2. Environment-specific migrations (dev → staging → production)
   - Dry-run validation before applying
   - Separate migration for each environment
   - Timing aligned with feature branch testing

### Phase 4: Cutover (Breaking Change)
1. Update startup to use new configuration
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   
   builder.Configuration
       .AddJsonFile("appsettings.json", optional: true)
       .AddJsonFile($"appsettings.{environment}.json", optional: true)
       .AddEnvironmentVariables()
       .AddVaultConfiguration(vaultClient, mountPath, environment);
   
   builder.Services.AddGridBotOptions(builder.Configuration);
   ```

2. Remove old `BaseSettingsProvider` system (optional fallback first)
   - Deprecation period allows rollback
   - Eventually remove `Configuration`, `Configuration.Core`, and `Vault` libraries

3. Gradual environment rollout
   - Deploy to dev first, validate
   - Deploy to staging with feature gate control
   - Deploy to production with hotfix capability

## Benefits

| Aspect | Current | Microsoft.Extensions.Options |
|--------|---------|------------------------------|
| **Validation Timing** | Lazy (at access) | Eager (at startup) ✅ |
| **DI Container** | Multiple instances ❌ | Single, managed ✅ |
| **Error Handling** | Inconsistent | Centralized ✅ |
| **Configuration Sources** | Custom Vault logic | Extensible `IConfigurationProvider` ✅ |
| **Testing** | Hard to mock ❌ | `Mock<IOptions<T>>` trivial ✅ |
| **Change Handling** | Manual `PropertyChanged` | Standard `IOptionsMonitor<T>` ✅ |
| **Documentation** | Implicit | Explicit via config classes ✅ |
| **Performance** | Reflection at startup | Static registration ✅ |
| **Standards Compliance** | Custom pattern | ASP.NET Core conventions ✅ |

## Technical Deliverables

1. **Options Classes** - One POCO per current settings provider
2. **Validators** - `IValidateOptions<T>` implementations with business rules
3. **VaultConfigurationProvider** - Custom `IConfigurationProvider` for Vault integration
4. **DI Extension Methods** - Explicit, single-container registration
5. **Vault Migration Tool** - Transforms current structure to new format
6. **Updated Consumers** - Services using `IOptions<T>` and `IOptionsMonitor<T>`
7. **Tests** - Unit tests for validators and change handling
8. **Migration Runbook** - Step-by-step guide for dev → staging → production

## Acceptance Criteria

- [ ] All configuration validated at startup with clear error messages
- [ ] Single, managed DI container throughout application startup
- [ ] Services can handle configuration changes via `IOptionsMonitor<T>` callbacks
- [ ] Vault refresh intervals (per-provider and global) maintained
- [ ] Environment variable precedence over Vault preserved
- [ ] Three environments (dev, staging, production) successfully migrated
- [ ] Unit tests for validators and configuration change scenarios
- [ ] Vault migration tool tested and verified (dry-run + actual)
- [ ] No breaking changes to external APIs or behaviors
- [ ] Old `Configuration`/`Configuration.Core`/`Vault` libraries removable

## Risk Mitigation

- **Phase 1 (Code Refactoring)**: Low risk—runs parallel with current system
- **Phase 2 (Consumers)**: Medium risk—incremental refactoring allows testing
- **Phase 3 (Vault Migration)**: High risk—data migration; mitigate with dry-run, validation, rollback plan
- **Phase 4 (Cutover)**: Breaking change—gradual rollout dev → staging → production with hotfix capability

## References

- [Microsoft.Extensions.Options](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.options)
- [IConfigurationProvider](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfigurationprovider)
- [IOptionsMonitor<T>](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.ioptionsmonitor-1)
