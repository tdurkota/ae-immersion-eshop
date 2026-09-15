# Report Structure

Template for the markdown body of an adversarial review. Follow this structure for consistency and tooling.

## Overall Structure

```markdown
---
[metadata header here]
---

# Adversarial Review: [Feature] (Iteration [N])

## Executive Summary

[1–3 sentences on overall risk profile, key themes, production readiness]

## Critical Findings (N)

[Each finding as a subsection, see format below]

## High Findings (N)

[Each finding as a subsection]

## Medium Findings (N)

[Each finding as a subsection]

## Low Findings (N)

[Each finding as a subsection]

## No Issues Identified

[List dimensions where no findings emerged, or omit if findings cover all]

## Design Trade-offs & Assumptions

[Explicit design choices and why, assumptions that must hold for safety]

## Metrics

[Performance and coverage data]

## Appendix: Iteration Comparison

[If applicable: comparison to previous review]
```

## Finding Format

For each finding, use this structure:

```markdown
### N. [Finding Title]

**Category**: [Architecture | Risk | Security | Performance | Testing]

**Severity**: [Critical | High | Medium | Low]

**Location**: `[File Path](file.ts#L10-L25)`

**Code**:

\`\`\`[language]
[relevant code snippet, 2–5 lines]
\`\`\`

**Risk**: [Explanation of why this matters and what could go wrong]

**Assumption**: [What must be true for this to be safe?]

**Recommendation**: [Concrete, actionable next step]

---
```

## Example Finding

```markdown
### 1. Race Condition in CartService.UpdateItem

**Category**: Risk & Edge Cases

**Severity**: Critical

**Location**: [`src/Basket.API/Services/CartService.cs`](src/Basket.API/Services/CartService.cs#L84-L92)

**Code**:

\`\`\`csharp
var cachedCart = _cache.Get(cartId);
cachedCart.Items[itemIndex].Quantity += delta;
_cache.Set(cartId, cachedCart);  // Vulnerable if another thread modifies before write
\`\`\`

**Risk**: Under concurrent updates, two threads may load the same cart, modify it independently, and the second write overwrites the first. Customer loses modifications.

**Assumption**: Cache is never read while being written (false under load).

**Recommendation**: Use atomic compare-and-swap or database row versioning with optimistic locking.

---
```

## Executive Summary Examples

- **Low risk**: "Cart optimization reduces API calls 40% with minimal concurrency risk. Recommend pre-production load testing for write-heavy scenarios."

- **High risk**: "Payment flow introduces distributed transaction anti-pattern. Critical: saga pattern or compensating transactions required before production."

- **Medium risk**: "Auth redesign improves UX but lacks observability for token expiry edge cases. Recommend adding metric for failed-auth-retry rate."

## Metrics Section Example

```markdown
## Metrics

- **Files changed**: 8
- **Lines added/removed**: +450 / -120
- **Cyclomatic complexity**: avg 3.2 (within range, max 7)
- **Test coverage (new code)**: 72%
- **Test coverage (modified code)**: 81%
- **Dependency changes**: Aspire.Hosting 8.0 → 8.1 (minor)
```

## Iteration Comparison Example

```markdown
## Appendix: Iteration Comparison

| Dimension | Iteration 1 | Iteration 2 | Change |
|-----------|------------|------------|--------|
| Critical findings | 2 | 0 | ✅ Resolved |
| High findings | 3 | 1 | ✅ Improved |
| Architecture coupling | Tight (3 circular deps) | Loose (0 circular deps) | ✅ Improved |
| Test coverage | 72% | 89% | ✅ Improved |
| Observable gaps | Log levels inconsistent | Standardized, added trace span | ✅ Improved |

**Progress**: Race condition fixed with optimistic locking. Auth error observability added. One high finding remains: cache invalidation timing edge case (deferred to iteration 3).
```
