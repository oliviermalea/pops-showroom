# Test Rules — ShowRoom

- Every module must have its own test project under `tests/`.
- Endpoint tests and Assembler tests are the highest testing priority and are mandatory for each HTTP feature.
- For create features, endpoint tests must assert `201 Created`, Location header consistency, and a response payload containing only the created `PublicId` string when the contract is identifier-only.
- Use xUnit with AAA structure and name the SUT variable `sut` when relevant.
- Prefer `AwesomeAssertions` for assertions in module tests.
- Add unit tests for domain behavior, configuration, and edge cases.
- Add integration tests for endpoints and persistence when the module exposes HTTP features.
- Keep test fixtures aligned with the module being tested, inspired by the Acquisition module pattern.
- Integration isolation uses the shared **`ShowRoom.Testing`** project (referenced by every module test project): `BusinessWebFactory : WebApplicationFactory<Program>` (owns a Testcontainers PostgreSQL container + virtual `ConfigureModuleTestServices`/`InitializeModuleTestServices` hooks), `Database/DatabaseContainer` (xUnit fixture), `Database/IDatabaseConfiguration`, `Configuration/ConfigurationExtensions` (`WithContainerDatabaseConfigured`, `SetFakeSystemClock`), `Http/HttpResponseMessageExtensions` (`GetIdFromLocationHeader`), `BusinessBaseIntegrationTest<TFactory>`.
- For each module exposing HTTP endpoints, include a dedicated `<Module>BusinessWebFactory : BusinessWebFactory` that, in `ConfigureModuleTestServices`, removes the app's `DbContextOptions<<Module>Context>` and re-adds `AddDbContext` pointed at the factory container, and migrates in `InitializeModuleTestServices`. This requires the module to register its DbContext via plain `AddDbContext` (`DatabaseModule.AddDatabase`), NOT Aspire's `AddNpgsqlDbContext` (whose options action cannot be cleanly removed).
- For each module exposing HTTP endpoints, include a dedicated `<Module>DatabaseConfiguration : IDatabaseConfiguration` that externalises the connection string under the key the module's `AddDatabase` reads.
- Endpoint tests are `IClassFixture<<Module>BusinessWebFactory>` + `IClassFixture<DatabaseContainer>` and MUST go through `factory.WithContainerDatabaseConfigured(...)` — never the bare base factory (the app's `AddDatabase` throws without a connection string). Seed via that configured factory's DbContext (or via HTTP).
