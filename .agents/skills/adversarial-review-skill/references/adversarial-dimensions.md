# Adversarial Review Dimensions

Each review covers these dimensions. For each, identify findings that expose design flaws, assumptions, or risks.

## Architecture & Design

- **Coupling**: Does the code tightly couple to frameworks, third-party libraries, or internal systems? Would swapping implementations require surgery?
- **Cohesion**: Are responsibilities grouped logically, or scattered across files/classes?
- **Abstraction leaks**: Do internal details (database structure, cache keys, serialization) surface to callers?
- **Dependency direction**: Do dependencies flow toward stable abstractions, or are there circular paths?
- **Scalability assumptions**: What throughput, concurrency level, or state size does the design assume? When does it break?
- **Pattern consistency**: Does it align with the codebase's established patterns, or introduce new conventions?

## Risk & Edge Cases

- **Boundary conditions**: Empty collections, null/undefined, zero, negative, max int, max string length?
- **Concurrency**: Race conditions on shared state, deadlock potential, lock granularity, ordering assumptions?
- **Transaction isolation**: Do partial failures leave inconsistent state? Is atomicity guaranteed?
- **Error paths**: Are exceptions caught, logged, propagated correctly? Do error handlers hide root causes?
- **Resource cleanup**: Connections, memory, file handles, locks—are they released in all paths (success, error, cancel)?
- **Replay/idempotency**: What happens if a request is sent twice? If a process crashes mid-operation?

## Security & Data

- **Input validation**: Are all external inputs validated? Format, range, encoding? Injection risks (SQL, command, template)?
- **Authorization**: Does every action check permission? Are privilege boundaries enforced consistently?
- **Data exposure**: Are sensitive values (PII, credentials, secrets) logged, cached, or returned unnecessarily?
- **Encryption**: Are data in transit and at rest encrypted where required? Key management sound?
- **Audit trails**: Can you reconstruct who did what, when, and from where?
- **Secrets handling**: Are secrets injected, never hardcoded? Rotated? Accessed only by authorized code?

## Performance & Operations

- **Query complexity**: N+1 queries, full-table scans, inefficient joins? Caching strategy correct?
- **Throughput bottlenecks**: Synchronous I/O, serial processing, lock contention on hot paths?
- **Observability**: Are logs, metrics, and traces emitted at the right granularity and frequency?
- **Operability**: Can you debug, configure, and operate this without source access? Are knobs exposed?
- **Monitoring blind spots**: Which failure modes have no alert? Which SLO is not measured?
- **Operational debt**: Does this require manual toil, or can it scale without operational cost?

## Testing & Maintainability

- **Happy-path coverage**: All code branches exercised? Edge cases and errors tested?
- **Test brittleness**: Do tests couple to implementation details (mocking internals)? Will refactoring break them?
- **Code readability**: Would a new team member understand the intent without asking? Are names precise?
- **Documentation gaps**: Are non-obvious design choices documented? Why this algorithm, not that one?
- **Duplication**: Is logic repeated across files? Candidates for shared utilities or base classes?
- **Refactoring opportunities**: Can this be simplified? Does it introduce technical debt that future changes will compound?

## Scoring Guidelines

- **Critical**: Affects production users, causes data loss, enables unauthorized access, or violates SLA
- **High**: Tech debt that slows future work, observability gap that hides failure, or risky assumption undocumented
- **Medium**: Nice-to-have improvement, marginal performance gain, or consistency issue
- **Low**: Style preference, optional polish, or future consideration

## Important: "No Findings" Is Valid

Not every code change has findings in every dimension. If a dimension genuinely has no issues (e.g., UI-only change has zero security concerns), explicitly mark it as **"No findings in [dimension]"** rather than forcing a low-quality finding. Quality over completeness.
