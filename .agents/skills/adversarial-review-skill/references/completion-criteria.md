# Completion Criteria

Sharp, checkable conditions that define "done" for each review step.

## Step 1: Gather Context — Done When

- [ ] `/docs` directory exists (created if needed via `mkdir -p docs/`), and is writable (verified via test write)
- [ ] Current branch name extracted (e.g., `feature/auth-redesign` or `user/jane/exp`)
- [ ] Feature name identified (from branch, or fallback to full branch name slugified if non-standard)
- [ ] Current commit hash recorded
- [ ] List of changed files obtained (lines added/removed per file)
- [ ] **Scope decision made**:
  - [ ] If branch touches **one cohesive feature**: proceed with single review, scope = all changed files or user-specified subset
  - [ ] If branch touches **multiple distinct features** (e.g., auth + cart, or service A + service B + service C): **produce separate reviews per feature**, each with own iteration counter (e.g., `_..._auth_1_...md` and `_..._cart_1_...md`)
  - [ ] If branch has **>20 changed files or touches >3 distinct services**: flag for splitting; warn user that shallow review risk is high with single review
- [ ] All existing reviews in `/docs` scanned for this feature (or features if multi-feature split)
- [ ] Next iteration number(s) determined (max existing + 1, or 1 if none exist, per feature)

## Step 2: Perform Adversarial Analysis — Done When

- [ ] All six dimensions reviewed: Architecture, Risk, Security, Performance, Testing, Operations
- [ ] For **each dimension**:
  - [ ] If findings exist, each documented with: location, severity, code snippet (if applicable), risk, assumption, recommendation
  - [ ] If **no findings** (dimension genuinely not applicable), explicitly note: "No findings in [dimension]" (e.g., "No findings in Security — UI-only change")
- [ ] All critical findings verified against actual code (not hallucinated)
- [ ] All findings reference specific file paths, line numbers, or method names
- [ ] Severity levels used consistently per scoring guidelines in [adversarial-dimensions.md](adversarial-dimensions.md)
- [ ] No forced findings: quality > quantity. Better to skip a dimension than invent low-quality findings.

## Step 3: Structure Report — Done When

- [ ] Markdown file written with valid YAML frontmatter (see [metadata header](../templates/metadata-header.md))
- [ ] Executive summary (1–3 sentences) states overall risk profile
- [ ] Findings grouped by severity: critical, high, medium, low
- [ ] Within each severity, findings grouped by dimension (architecture, risk, security, etc.)
- [ ] Each finding section formatted per [report structure](../templates/report-structure.md)
- [ ] Trade-offs documented (explicit design choices and why)
- [ ] Assumptions requiring validation listed
- [ ] Metrics included: files changed, lines added/removed, test coverage %
- [ ] Appendix: comparison to previous iteration (if applicable)

## Step 4: Export to /docs — Done When

- [ ] `/docs` directory exists (created in Step 1) and is writable (verified via test write)
- [ ] Timestamp generated using [generate-timestamp.py](../scripts/generate-timestamp.py) (portable, millisecond precision)
- [ ] Target filename calculated: `adversarial-review_[feature]_[branch]_[iteration]_[timestamp].md`
  - `[feature]`: slugified feature name (lowercase, hyphens, no spaces)
  - `[branch]`: branch name (lowercase, hyphens)
  - `[iteration]`: integer from Step 1 (guaranteed unique per feature)
  - `[timestamp]`: ISO 8601 with milliseconds from script (e.g., `20260915T143022.123Z`)
- [ ] **Collision detection (atomic write)**:
  - [ ] Check if target filename already exists
  - [ ] If it does, increment collision counter (`_001`, `_002`, etc.) and retry with new filename
  - [ ] Continue until a unique filename is found or max retries (10) exceeded
  - [ ] Example collision sequence: `..._20260915T143022.123Z.md` → `..._20260915T143022.123Z_001.md` → `..._20260915T143022.123Z_002.md`
  - [ ] If max retries exceeded, raise clear error (do not overwrite)
- [ ] File written with valid YAML frontmatter (parseable by tools)
- [ ] All sections of report present
- [ ] No existing review overwritten
- [ ] Summary printed: file path, line count, critical findings count, collision counter (if any)
