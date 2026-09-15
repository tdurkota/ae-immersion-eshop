---
name: adversarial-review-skill
description: 'Generate adversarial (red-team) code reviews identifying design flaws, edge cases, and risk. Export to /docs as timestamped markdown. Use when: pre-merge risk assessment, security/performance red-teaming, design critique, edge-case documentation, post-feature knowledge capture, iteration retrospective.'
argument-hint: 'Optional: feature name, iteration number, specific components to review'
---

# Adversarial Code Review

## Purpose

Document design risks, edge cases, and opportunities in delivered code from a **red-team perspective**. Output is a timestamped markdown artifact captured in `/docs` for audit, knowledge, and iteration tracking.

An adversarial review assumes code is functionally correct and focuses on: why it might fail under load, change, concurrency, or scale; architectural debt; coupling; gaps in observability; and alternative designs.

## When to Use

- **Pre-merge**: Capture critical observations before integration
- **Red-team**: Identify security, performance, or reliability blind spots  
- **Design critique**: Document trade-offs and assumptions
- **Edge-case documentation**: Surface boundary conditions and error paths
- **Iteration retrospective**: Compare and improve reviews across cycles

## Inputs (Optional)

- **Feature name**: Extracted from branch if omitted (e.g., `feature/auth-redesign` → `auth-redesign`). For non-standard branches (e.g., `user/jane/exp`, `hotfix/prod`), fallback to full branch name or prompt user.
- **Iteration number**: Auto-detected by finding max iteration in `/docs` for this feature; defaults to 1 if none exist
- **Scope**: Specific files or components to focus on (defaults to all changed files)

## Steps

### 1. Gather Context
Extract feature name, branch, commit, and list of changed files. Create `/docs` directory if missing. Scan `/docs` for existing reviews of this feature to determine next iteration number.

**Completion criterion**: `/docs` directory exists and is writable, branch name parsed, feature name identified (with fallback for non-standard branches), changed files listed, next iteration number determined by scanning existing reviews, scope confirmed (single feature or multiple distinct features to split).

### 2. Perform Adversarial Analysis
Review code across [key dimensions](./references/adversarial-dimensions.md): architecture, risk, security, performance, testing. For each finding, document location, impact, assumption, and recommendation.

**Completion criterion**: At least one finding per dimension recorded; all critical/high findings include code reference and actionable recommendation.

### 3. Structure Report
Organize findings by severity (critical, high, medium, low), each with [required fields](./references/completion-criteria.md). Include executive summary, trade-offs, and metrics.

**Completion criterion**: Markdown written with [metadata header](./templates/metadata-header.md), findings grouped by severity, code snippets included, recommendations stated.

### 4. Export to /docs
Write file as: `docs/adversarial-review_[feature]_[branch]_[iteration]_[timestamp].md`

Timestamp format: Use [generate-timestamp.py](./scripts/generate-timestamp.py) for cross-platform consistency (macOS/Linux/Windows). Format: ISO 8601 with milliseconds (e.g., `20260915T143022.123Z`).

**Collision detection (atomic write)**: Before writing, check if target filename exists. If it does, attempt to read it; if successful, increment collision counter and retry (e.g., `..._20260915T143022.123Z_001.md`). Continue until a unique filename is found or max retries (10) exceeded. This prevents silent overwrites under extreme concurrency.

Example: `adversarial-review_payment-gateway_feature-payment-gateway-integration_1_20260915T143022.123Z.md`

**Completion criterion**: File exists at correct path with correct naming, valid YAML frontmatter, all required sections populated, collision check performed, no overwrites of existing reviews.

## Output Summary

- Display review file path and line count
- Report critical findings count
- Flag assumptions requiring validation

## Tips

- Focus on **intent over syntax**: let linting catch style
- Be **constructive**: frame as "resilience opportunity" not "this is broken"
- Document **unknowns**: explicitly note assumptions needing validation
- Compare **iterations**: each cycle builds on the last; track improvements
