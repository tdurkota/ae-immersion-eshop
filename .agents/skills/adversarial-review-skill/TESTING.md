# Testing Strategy: Adversarial Review Skill

This document outlines how to validate the adversarial-review-skill before beta deployment, focusing on timestamp generation and collision detection across platforms.

## Quick Start

Run the full test suite:

```bash
cd .agents/skills/adversarial-review-skill
python3 scripts/test-adversarial-review.py
```

Expected output: `5/5 tests passed` (green checkmarks).

---

## Test Coverage

### Unit Tests (Automated)

Run `test-adversarial-review.py` to validate:

| Test | What It Checks | Pass Criteria |
|------|----------------|---------------|
| **Timestamp Format** | Output is ISO 8601 format | Matches `YYYYMMDDTHHHMMSS.SSSZ` |
| **Millisecond Precision** | Timestamps have millisecond granularity | 10 rapid calls produce ≥3 unique millisecond values |
| **UTC Timezone** | Timestamp is in UTC, not local time | Ends with `Z`, within 5 seconds of current UTC |
| **Collision Detection** | File retry logic with counter appending | Handles 3 sequential collisions correctly (`_001`, `_002`, `_003`) |
| **No Silent Overwrites** | Original files are never silently replaced | Counter files created instead of overwriting |

### Integration Tests (Manual)

#### macOS
```bash
# Run test suite
python3 .agents/skills/adversarial-review-skill/scripts/test-adversarial-review.py

# Manual: Invoke timestamp script directly
python3 .agents/skills/adversarial-review-skill/scripts/generate-timestamp.py

# Manual: Test in shell script
TIMESTAMP=$(python3 .agents/skills/adversarial-review-skill/scripts/generate-timestamp.py)
echo "Generated: $TIMESTAMP"
```

#### Linux
```bash
# Same as macOS (Python is platform-independent)
python3 .agents/skills/adversarial-review-skill/scripts/test-adversarial-review.py
```

#### Windows (PowerShell)
```powershell
# Run test suite
python .\\.agents\skills\adversarial-review-skill\scripts\test-adversarial-review.py

# Or using Python -c:
python -c "from datetime import datetime, timezone; now = datetime.now(timezone.utc); print(now.strftime('%Y%m%dT%H%M%S') + f'.{now.microsecond // 1000:03d}Z')"
```

---

## Stress Tests (Concurrency)

### Test Scenario: Parallel Review Generation

Simulate concurrent review submissions to detect collision timing issues.

**Expected behavior**: All reviews written without silent overwrites, using collision counters if needed.

#### bash/macOS/Linux

```bash
#!/bin/bash
# stress-test-collision.sh

DOCS_DIR="/tmp/adversarial-review-stress-test"
rm -rf "$DOCS_DIR" && mkdir -p "$DOCS_DIR"

# Function to generate a review file
generate_review() {
    local n=$1
    local timestamp=$(python3 .agents/skills/adversarial-review-skill/scripts/generate-timestamp.py)
    local filename="$DOCS_DIR/adversarial-review_stresstest_feature-stresstest_1_${timestamp}.md"
    
    # Check collision
    if [ -f "$filename" ]; then
        # Collision detected, use counter
        filename="${filename%.md}_$(printf "%03d" $n).md"
    fi
    
    echo "Review $n" > "$filename"
    echo "Created: $(basename "$filename")"
}

# Launch 5 reviews in parallel (within same millisecond)
for i in {1..5}; do
    generate_review $i &
done

wait

# Verify all files exist and no overwrites
count=$(ls "$DOCS_DIR" | wc -l)
echo "Generated $count files (expected 5)"
ls -la "$DOCS_DIR"
```

Run:
```bash
bash stress-test-collision.sh
```

#### Windows (PowerShell)

```powershell
# stress-test-collision.ps1

$DocsDir = "C:\tmp\adversarial-review-stress-test"
Remove-Item -Force -Recurse -ErrorAction SilentlyContinue $DocsDir
New-Item -ItemType Directory -Path $DocsDir | Out-Null

# Function to generate review
function New-Review {
    param([int]$n)
    
    $timestamp = python .agents/skills/adversarial-review-skill/scripts/generate-timestamp.py | % { $_.Trim() }
    $filename = "$DocsDir/adversarial-review_stresstest_feature-stresstest_1_${timestamp}.md"
    
    if (Test-Path $filename) {
        # Collision, use counter
        $filename = $filename -replace '\.md$', "_$($n.ToString('000')).md"
    }
    
    "Review $n" | Out-File -FilePath $filename
    Write-Output "Created: $(Split-Path -Leaf $filename)"
}

# Launch 5 reviews in parallel
1..5 | ForEach-Object { New-Review $_ }

# Verify
$count = (Get-ChildItem $DocsDir).Count
Write-Output "Generated $count files (expected 5)"
Get-ChildItem -Path $DocsDir | Format-Table Name
```

Run:
```powershell
.\stress-test-collision.ps1
```

---

## CI/CD Integration

### GitHub Actions

Add to `.github/workflows/test-adversarial-review.yml`:

```yaml
name: Test Adversarial Review Skill

on: [push, pull_request]

jobs:
  test:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, macos-latest, windows-latest]
        python-version: ['3.9', '3.10', '3.11', '3.12']

    steps:
      - uses: actions/checkout@v4
      
      - name: Set up Python
        uses: actions/setup-python@v4
        with:
          python-version: ${{ matrix.python-version }}
      
      - name: Run tests
        run: python .agents/skills/adversarial-review-skill/scripts/test-adversarial-review.py
      
      - name: Test timestamp generation (manual invocation)
        run: python .agents/skills/adversarial-review-skill/scripts/generate-timestamp.py
```

### Pre-commit Hook (Local)

Add to `.husky/pre-commit`:

```bash
#!/bin/sh

echo "Testing adversarial-review-skill before commit..."
python3 .agents/skills/adversarial-review-skill/scripts/test-adversarial-review.py

if [ $? -ne 0 ]; then
  echo "Tests failed! Blocking commit."
  exit 1
fi
```

Make executable:
```bash
chmod +x .husky/pre-commit
```

---

## Test Results: Passing Criteria

### Alpha → Beta Gate

✅ **Proceed to beta if**:
- All 5 automated tests pass on macOS
- Timestamp test passes on Linux (via GitHub Actions or manual)
- Timestamp test passes on Windows (via GitHub Actions or manual)
- Stress test shows no silent overwrites

⚠️ **Revisit if**:
- Collision counter logic shows edge cases (e.g., max retries hit)
- Timestamps differ between platforms (format or precision)
- Silent overwrites detected in stress test

### Exit Codes

```
0 = All tests passed → Safe to beta
1 = One or more tests failed → Block beta, investigate
```

---

## Known Limitations & Edge Cases

### Limitation 1: Same Millisecond on Ultra-Fast Hardware

On very fast systems (or under heavy load), multiple `generate-timestamp.py` invocations *within the same millisecond* may produce identical timestamps.

**Mitigation**: Collision detection counter handles this. Files get `_001`, `_002` suffixes.

**Testing**: Stress test intentionally spawns 5 concurrent calls to trigger this.

### Limitation 2: Distributed `/docs` Storage

If `/docs` is synced via NFS, S3, or Dropbox, network latency can create race conditions where two processes calculate the same iteration number simultaneously.

**Mitigation**: Add atomic file-system operations (e.g., `O_EXCL` on Unix, `CreateNew` on Windows).

**Testing**: Not covered by unit tests. Recommend integration test in real environment.

### Limitation 3: Windows Path Length

Windows paths have a 260-character limit (without special handling). Adversarial review filenames can be long.

**Example**: `adversarial-review_very-long-feature-name_feature-very-long-feature-name_10_20260915T193842.123Z_001.md`

**Mitigation**: Use short feature names, or use `\\?\` prefix on Windows (uncap limit to 32KB).

**Testing**: Create a review with a 40+ character feature name and verify filename doesn't exceed limit.

---

## Checklist: Before Beta Rollout

- [ ] Run `test-adversarial-review.py` on macOS: 5/5 pass
- [ ] Run `test-adversarial-review.py` on Linux (or GitHub Actions): 5/5 pass
- [ ] Run `test-adversarial-review.py` on Windows (or GitHub Actions): 5/5 pass
- [ ] Manual: Generate a timestamp, verify format is `YYYYMMDDTHHHMMSS.SSSZ`
- [ ] Manual: Generate a review in `/docs`, verify file created with correct name
- [ ] Manual: Generate a second review within 1 second, verify collision counter used (`_001`)
- [ ] Stress test: 5 parallel reviews, 0 silent overwrites
- [ ] GitHub Actions: Workflow runs on all three platforms, all pass
- [ ] Documentation: Test procedure documented in team wiki/README
- [ ] Decision: Team signs off on "ready for beta"

---

## Post-Beta: Observability

Once beta is live, track:

1. **Collision counter usage**: Log when `_001`, `_002` etc. are used. Should be rare (<1% of reviews).
2. **Timestamp consistency**: Sample timestamps across team, verify no platform discrepancies.
3. **Review file creation success rate**: Track failures (permission errors, disk full, etc.).
4. **Feature name extraction**: Log fallback cases (non-standard branches) to identify patterns.

Example monitoring:
```bash
# Count collisions
ls docs/adversarial-review_*_[0-9][0-9][0-9].md | wc -l

# Check for oldest reviews (archive candidates)
find docs -name "adversarial-review_*.md" -type f -mtime +180 | wc -l
```

---

## Support & Debugging

### Test Fails: "Timestamp format invalid"
- Verify Python 3.7+ installed: `python3 --version`
- Check `generate-timestamp.py` exists and is executable
- Run directly: `python3 .agents/skills/adversarial-review-skill/scripts/generate-timestamp.py`

### Test Fails: "Insufficient millisecond variation"
- May indicate system time skew or very slow hardware
- Run test multiple times; variance is non-deterministic
- If consistently fails, check system clock accuracy

### Test Fails: "Collision detection not working"
- Verify temp directory is writable: `python3 -c "import tempfile; print(tempfile.gettempdir())"`
- Check disk space: `df -h /tmp` (macOS/Linux) or `dir C:\` (Windows)

### Manual Test: "Review file not created"
- Verify `/docs` directory exists and is writable: `mkdir -p docs && touch docs/test.md && rm docs/test.md`
- Check file permissions: `ls -la docs/`
- Verify filename doesn't exceed OS path length limits

---

## Questions?

See [SKILL.md](../SKILL.md) for workflow details, or reach out to the team lead.
