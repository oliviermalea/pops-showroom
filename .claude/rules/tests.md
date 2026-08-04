# Test Rules — ShowRoom

- Every module must have its own test project under `tests/`.
- Endpoint tests and Assembler tests are the highest testing priority and are mandatory for each HTTP feature.
- For create features, endpoint tests must assert `201 Created`, Location header consistency, and a response payload containing only the created `PublicId` string when the contract is identifier-only.
- Use xUnit with AAA structure and name the SUT variable `sut` when relevant.
- Prefer `AwesomeAssertions` for assertions in module tests.
- Add unit tests for domain behavior, configuration, and edge cases.
- Add integration tests for endpoints and persistence when the module exposes HTTP features.
- Keep test fixtures aligned with the module being tested, inspired by the Acquisition module pattern.
- For each module exposing HTTP endpoints, include a dedicated `<Module>BusinessWebFactory` for full endpoint isolation.
- For each module exposing HTTP endpoints, include a dedicated `<Module>DatabaseConfiguration` used by endpoint tests to externalize module connection settings.
