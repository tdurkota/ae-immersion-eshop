# Event Versioning Strategy

## Purpose
Define how integration events evolve without breaking downstream consumers as the marketplace and existing eShop domains continue to ship independently.

## Versioning Rules
1. **Append-only contracts**: published event fields are never removed or repurposed in-place.
2. **Additive change first**: optional fields may be added to the current version when consumers can safely ignore them.
3. **New major version for breaking changes**: rename/removal, semantic meaning changes, required field additions, or structural changes create a new version (`v2`, `v3`, and so on).
4. **Parallel publication is required during migration**: when a breaking change is introduced, producers publish both prior and new versions until all approved consumers complete cutover.
5. **Version is explicit in the contract**: every payload includes `eventType` and `eventVersion`, and the registry stores producer, status, owner, and sunset metadata.
6. **Schema registration before release**: no producer may emit a new event or version until its schema and sample payload are added to the event contract registry.
7. **Consumer isolation**: subscribers must bind to the event version they support and treat unknown fields as non-breaking.

## Registry Requirements
Each registry entry must capture:
- Event name
- Version
- Owning team
- Producing service
- Known consuming services or partner integrations
- Compatibility notes
- Example payload
- Lifecycle state (`draft`, `active`, `deprecated`, `retired`)
- Migration or sunset target date when applicable

## Migration Strategy
### Non-Breaking Changes
- Add new optional fields only
- Update schema documentation and example payloads
- Notify consumers through release notes, but do not require immediate action

### Breaking Changes
1. Create a new versioned contract entry, such as `OrderCreatedIntegrationEvent v2`.
2. Preserve the previous version unchanged.
3. Publish `v1` and `v2` side-by-side from the producer.
4. Update consumers incrementally, prioritizing internal subscribers before partner integrations.
5. Monitor adoption through subscription inventories, contract tests, and message telemetry.
6. Mark the older version as `deprecated` only after all required consumers validate the new version.
7. Retire the older version after the announced sunset window and remove publication code in a scheduled cleanup release.

## Recommended Migration Window
- Internal consumers: 1-2 sprints
- External partners: minimum 90-day notice unless a contractual SLA requires longer
- High-risk financial or payout events: require dual-publish validation and rollback readiness before deprecation

## Example: `OrderCreatedIntegrationEvent`
- **v1**: baseline order creation payload for existing downstream subscribers
- **v2**: adds seller attribution, commission, and payout routing fields for marketplace workflows
- **Migration approach**: dual-publish `v1` and `v2`, keep `v1` stable, validate all subscribers against `v2`, then deprecate `v1` with a published sunset date
