# Scripts: Timestamp Generation & Testing

## Files in This Directory

- **generate-timestamp.py** — Generate ISO 8601 timestamps with millisecond precision (cross-platform)
- **test-adversarial-review.py** — Automated test suite for timestamp format, precision, collision detection
- **README.md** — This file

---

## Quick Start: Timestamp Generation

### Python

```bash
python3 ./generate-timestamp.py
# Output: 20260915T193842.123Z
```

### Inline (Python one-liner)

```bash
python3 -c "from datetime import datetime, timezone; now = datetime.now(timezone.utc); print(now.strftime('%Y%m%dT%H%M%S') + f'.{now.microsecond // 1000:03d}Z')"
```

### Shell Integration

For use in shell scripts:

```bash
TIMESTAMP=$(python3 ./generate-timestamp.py)
FILENAME="adversarial-review_myfeature_feature-myfeature_1_${TIMESTAMP}.md"
echo "$FILENAME"
# Output: adversarial-review_myfeature_feature-myfeature_1_20260915T193842.123Z.md
```

## Format

- **ISO 8601**: `YYYYMMDDTHHHMMSS.SSSZ`
- **Example**: `20260915T193842.123Z` (2026-09-15 at 19:38:42.123 UTC)
- **Precision**: Milliseconds (3 digits)
- **Timezone**: UTC (Z suffix)

---

## Quick Start: Testing

### Run Full Test Suite

```bash
python3 ./test-adversarial-review.py
```

**Expected output**: `5/5 tests passed` (all green ✓)

### What Gets Tested

1. ✓ Timestamp format (ISO 8601)
2. ✓ Millisecond precision (no duplicates in rapid succession)
3. ✓ UTC timezone (Z suffix, current time)
4. ✓ Collision detection (atomic write retry with counter)
5. ✓ No silent overwrites (counter files instead of overwriting)

### Cross-Platform Testing

Test on all target platforms:

```bash
# macOS
python3 ./test-adversarial-review.py

# Linux (same)
python3 ./test-adversarial-review.py

# Windows (PowerShell)
python .\test-adversarial-review.py
```

All should output: `Total: 5/5 tests passed`

---

## For Beta Validation

See [../TESTING.md](../TESTING.md) for:
- Detailed test procedures
- Stress testing for concurrency
- CI/CD integration (GitHub Actions)
- Pre-commit hook setup
- Known limitations and edge cases
- Checklist before beta rollout

---

## Why Millisecond Precision?

Timestamps with second-level precision risk collisions when multiple reviews are generated within the same second (e.g., parallel CI jobs, rapid automation). Millisecond precision reduces collision probability to <1% and collision detection handles the rest.
