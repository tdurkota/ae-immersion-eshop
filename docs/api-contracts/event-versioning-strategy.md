# Event Versioning Strategy

## Purpose
Define how integration events evolve as the marketplace and existing eShop domains continue to ship independently.

## Context
This application currently serves no traffic. We prioritize rapid development and iteration over backward compatibility or clean migration paths. We will not roll back during the development phase, and all services can be redeployed as needed. As the platform matures and traffic increases, these strategies may evolve to require formal versioning and migration windows.

## Versioning Rules
1. **Append-only contracts (preferred)**: published event fields are never removed or repurposed in-place to maintain resilience across versions.
2. **Additive changes first**: optional fields may be added to the current version when consumers can safely ignore them.
3. **Breaking changes allowed**: rename/removal, semantic meaning changes, required field additions, or structural changes can be introduced when all consumers are updated in the same deployment cycle.
4. **Version is explicit in the contract**: every payload includes `eventType` and `eventVersion` for clarity and forward compatibility planning.
5. **Schema registration before release**: event schemas should be documented before emission to serve as contracts, but are not blocking deployment during development.
6. **Consumer isolation**: subscribers must handle the event versions they support and treat unknown fields as non-breaking.

## Registry Requirements
Each registry entry should capture:
- Event name
- Version
- Owning team
- Producing service
- Known consuming services
- Example payload
- Lifecycle state (`draft`, `active`, `deprecated`)

## Migration Strategy
### Making Changes
Since this application serves no traffic and will not be rolled back during development, all changes can be deployed as needed:

- **Non-breaking changes** (adding optional fields): Update schema and example payloads, deploy normally
- **Breaking changes** (field removal, rename, semantic changes): Update all consumers in coordinated deployments where services are redeployed together
- **No migration windows required**: Changes take effect immediately upon deployment
- **No parallel publication needed**: Services can be updated without running multiple versions side-by-side

## Example: `OrderCreatedIntegrationEvent`
- **Current version**: baseline order creation payload
- **Changes approach**: Update schema directly in all services through coordinated deployment
- **No versioning needed**: All services are updated together during development
