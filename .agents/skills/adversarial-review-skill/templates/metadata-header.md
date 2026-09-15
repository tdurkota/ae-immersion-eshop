# Metadata Header

Paste at the top of every adversarial review. Tools can parse this to filter, diff, and track reviews over time.

```yaml
---
feature: payment-gateway-integration
branch: feature/payment-gateway-integration
iteration: 1
reviewed_at: 2026-09-15T14:30:22Z
reviewed_commit: abc1234def5678
scope: Basket.API, Ordering.API, Webhooks.API
previous_review_url: ./adversarial-review_payment-gateway_feature-payment-gateway-integration_0_20260908T100000Z.md
---
```

## Fields

- **feature**: Human-readable feature name (e.g., `payment-gateway-integration`)
- **branch**: Git branch name (e.g., `feature/payment-gateway-integration`)
- **iteration**: Review iteration number (1 for first review, 2 for second, etc.)
- **reviewed_at**: ISO 8601 timestamp with Z suffix (UTC) (e.g., `2026-09-15T14:30:22Z`)
- **reviewed_commit**: Full or short commit hash of the code reviewed
- **scope**: Comma-separated list of affected services/components
- **previous_review_url**: Path to prior iteration (optional; omit if first review)

## Extraction

Extract from branch name, with fallback:
- `feature/auth-redesign` → `feature: auth-redesign`, `branch: feature/auth-redesign`
- `fix/cart-race-condition` → `feature: cart-race-condition`, `branch: fix/cart-race-condition`
- `user/jane/experiment` → `feature: user-jane-experiment` (fallback: slugify full branch), `branch: user/jane/experiment`
- Non-standard → prompt user or use full branch name slugified

**Timestamp** (cross-platform): Generate with [generate-timestamp.py](../scripts/generate-timestamp.py), e.g., `20260915T193842.123Z` (ISO 8601 with millisecond precision, UTC).

**Iteration**: Query `/docs/` for all existing reviews matching this feature. Find max iteration number. Assign next one (e.g., if `_1_` and `_2_` exist, next is `_3_`). This is determined in Step 1.

**Collision handling**: When writing the file, check if target filename exists. If yes, append collision counter before `.md`: `..._20260915T193842.123Z_001.md`. Retry with incremented counter until unique name found or max retries (10) exceeded. **Never silently overwrite.**
