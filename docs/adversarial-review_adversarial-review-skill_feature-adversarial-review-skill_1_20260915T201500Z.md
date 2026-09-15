---
feature: adversarial-review-skill
branch: feature/adversarial-review-skill
iteration: 1
reviewed_at: 2026-09-15T20:15:00Z
reviewed_commit: 637762c
scope: .agents/skills/adversarial-review-skill/
previous_review_url: ~
---

# Adversarial Review: Adversarial Review Skill (Iteration 1)

## Executive Summary

The skill correctly applies writing-for-agents principles: lean main file, progressive disclosure via references/templates, sharp trigger words in description. However, the workflow lacks robustness around file system assumptions, edge cases in feature name extraction, and guidance for handling large codebases or ambiguous feature boundaries. Iteration count collision and missing `/docs` directory are blockers for production use. Most findings are medium-high severity and addressable before first production deployment.

## Critical Findings (1)

### 1. File System Collision Risk: Same-Second Reviews Overwrite

**Category**: Risk & Edge Cases

**Severity**: Critical

**Location**: [SKILL.md](SKILL.md#L44-L46), [metadata-header.md](templates/metadata-header.md#L8-L15)

**Code**:

```markdown
### 4. Export to /docs
Write file as: `docs/adversarial-review_[feature]_[branch]_[iteration]_[timestamp].md`
```

**Risk**: If two reviews of the same feature/branch/iteration run within the same second (plausible in automation or parallel CI), the second will silently overwrite the first. No warning, no error. Data loss.

**Assumption**: Reviews are always run sequentially with >1 second gap. False under CI/CD automation or user retry scenarios.

**Recommendation**: Add sub-second precision to timestamp (milliseconds or microseconds), e.g., `20260915T201500Z` → `20260915T201500.123Z`. Alternatively, detect collision and append a counter (`..._001.md`, `..._002.md`).

---

## High Findings (3)

### 2. Missing `/docs` Directory Not Handled

**Category**: Risk & Edge Cases

**Severity**: High

**Location**: [SKILL.md](SKILL.md#L44)

**Code**:

```markdown
### 4. Export to /docs
Write file as: `docs/adversarial-review_[feature]_[branch]_[iteration]_[timestamp].md`
```

**Risk**: Step 4's completion criterion specifies "File exists at correct path" but doesn't specify what happens if `/docs/` directory doesn't exist. Script will fail silently or with unclear error. User must manually create directory.

**Assumption**: `/docs/` directory already exists in all repos where the skill is used.

**Recommendation**: Add explicit step to create `/docs` directory if missing (e.g., `mkdir -p docs/`). Document this as part of Step 4 precondition or as a preamble.

---

### 3. Feature Name Extraction: No Fallback for Non-Standard Branches

**Category**: Risk & Edge Cases

**Severity**: High

**Location**: [SKILL.md](SKILL.md#L21-L22), [metadata-header.md](templates/metadata-header.md#L17-L21)

**Code**:

```markdown
**Feature name**: Extracted from branch if omitted (e.g., `feature/auth-redesign` → `auth-redesign`)
```

**Risk**: Extraction logic assumes branch naming convention: `feature/*`, `fix/*`, `release/*`. What if branch is `user/jake/experiment`, `hotfix/prod-down`, or `main` itself? No fallback documented. Extraction fails, feature name is empty or malformed.

**Assumption**: All branches follow standard prefix convention. Violated for experimentation branches, hotfixes, or direct commits.

**Recommendation**: Document fallback: if prefix not recognized, use full branch name, or prompt user for feature name. Add examples of edge-case branches and what feature names they should produce.

---

### 4. Iteration Number Collision: No Tie-Breaking for Concurrent or Out-of-Order Reviews

**Category**: Risk & Edge Cases

**Severity**: High

**Location**: [metadata-header.md](templates/metadata-header.md#L28-L29)

**Code**:

```markdown
Iteration: Increment if a prior review exists in `/docs` for this feature; otherwise start at 1.
```

**Risk**: If reviews are submitted out of order (e.g., iteration 2 lands before iteration 1 merges), or if a developer manually re-runs iteration 1 after iteration 2 exists, the script won't auto-detect and will reuse iteration numbers. Files get overwritten or numbers become non-sequential. Breaks "iteration retrospective" use case.

**Assumption**: Reviews always complete and land in order (false in distributed/async workflows).

**Recommendation**: Query `/docs` for all existing reviews of the feature, find max iteration, and assign next one. Add logic to detect and warn on out-of-order submissions.

---

## Medium Findings (4)

### 5. Completion Criterion: "At Least One Finding Per Dimension" Is Subjective

**Category**: Testing & Maintainability

**Severity**: Medium

**Location**: [SKILL.md](SKILL.md#L31)

**Code**:

```markdown
**Completion criterion**: At least one finding per dimension recorded; all critical/high findings include code reference and actionable recommendation.
```

**Risk**: What if a dimension genuinely has no findings? E.g., a feature touches only UI and has zero security concerns. Forcing "at least one" per dimension invites hallucinated or low-quality findings ("improve consistency" when there's no inconsistency). Lowers review quality.

**Assumption**: Every code change has findings in every dimension. Often false for small, focused changes.

**Recommendation**: Change criterion to "For each dimension *where findings exist*, document them." Or allow explicit "No issues identified" sections per dimension (already mentioned in template but not enforced in criteria). Track dimensions with zero findings separately.

---

### 6. No Guidance on Code Review Scope for Multi-Feature or Multi-Team Changes

**Category**: Architecture & Design

**Severity**: Medium

**Location**: [SKILL.md](SKILL.md#L23-L24)

**Code**:

```markdown
**Scope**: Specific files or components to focus on (defaults to all changed files)
```

**Risk**: If a branch spans multiple logical features (e.g., cart + checkout refactor together), or touches 50+ files, scope is ambiguous. Skill doesn't guide on how to slice or whether to produce one review or many. Result: either superficial review or overwhelming document.

**Assumption**: One branch = one feature. Violated for larger initiatives or refactors.

**Recommendation**: Add guidance: "If branch spans multiple features, produce separate reviews per feature (one doc per scope)." Document complexity limits (recommend: <20 changed files per review; for larger PRs, split scope).

---

### 7. Missing `/docs` in `.gitignore` or Tracking Decision Unclear

**Category**: Operations

**Severity**: Medium

**Location**: [SKILL.md](SKILL.md#L44)

**Code**:

```markdown
### 4. Export to /docs
Write file as: `docs/adversarial-review_[feature]_[branch]_[iteration]_[timestamp].md`
```

**Risk**: Should `/docs/adversarial-review_*.md` files be version-controlled, ignored, or kept locally? Skill doesn't state. If committed, every review pollutes history and PR diffs. If ignored, reviews are ephemeral and lost on clone. If kept locally, they're not discoverable by team.

**Assumption**: Repos have a convention for `/docs`. Often false; needs to be explicit.

**Recommendation**: Add to SKILL.md: "Document storage choice: commit to repo for audit trail, or keep locally and sync to shared drive/wiki. If committing, add `docs/adversarial-review_*.md` pattern to `.gitignore` if ephemeral, or commit and document retention policy."

---

### 8. Dimension Checklist Is Comprehensive but Overwhelming for First-Time Use

**Category**: Testing & Maintainability

**Severity**: Medium

**Location**: [references/adversarial-dimensions.md](references/adversarial-dimensions.md)

**Code**: Entire file (~100 lines)

**Risk**: New users invoking the skill face a 6-page dimension reference with 30+ questions per dimension. Cognitive load is high. Users may skim, miss depth, or spend 3+ hours on a simple feature review.

**Assumption**: Users will deep-read the dimensions document every time. False; many will scan or skip, leading to inconsistent depth.

**Recommendation**: Add a "quick mode" and "thorough mode" in the skill description and Step 2. Quick mode: review 3–5 key dimensions only; thorough: all 6. Or create a shortened checklist (3 key questions per dimension) as a separate reference.

---

## Medium-Low Findings (2)

### 9. Template Report Structure Assumes Code Snippets Are Always Available

**Category**: Testing & Maintainability

**Severity**: Medium

**Location**: [templates/report-structure.md](templates/report-structure.md#L62-L74)

**Code**:

```markdown
### N. [Finding Title]
...
**Code**:
\`\`\`[language]
[relevant code snippet, 2–5 lines]
\`\`\`
```

**Risk**: For architectural or design findings (e.g., "tight coupling between services"), a code snippet may not apply or may require 20+ lines to show context. Forcing a snippet leads to awkward truncation or vague snippets that don't illustrate the issue.

**Assumption**: Every finding has a localized code snippet. False for system-level findings.

**Recommendation**: Make code snippet optional: "**Code** (if applicable):" or allow architectural findings to reference diagrams/patterns instead.

---

### 10. Metadata Header: Incomplete Frontmatter Causes Silent Parsing Failures

**Category**: Architecture & Design

**Severity**: Medium

**Location**: [templates/metadata-header.md](templates/metadata-header.md#L5-L10)

**Code**:

```yaml
feature: payment-gateway-integration
branch: feature/payment-gateway-integration
iteration: 1
reviewed_at: 2026-09-15T14:30:22Z
reviewed_commit: abc1234def5678
scope: Basket.API, Ordering.API, Webhooks.API
previous_review_url: ./adversarial-review_payment-gateway_feature-payment-gateway-integration_0_20260908T100000Z.md
```

**Risk**: `previous_review_url` field is optional (absent on first review), but YAML parsers expect consistent schema. If a tool tries to auto-analyze reviews, missing or incorrectly formatted fields cause silent failures. No validation documented.

**Assumption**: All tools parsing metadata are forgiving. Often false.

**Recommendation**: Document required vs. optional fields explicitly. Add validation step to completion criteria: "YAML is valid and all required fields present." Consider a JSON Schema or frontmatter validator reference.

---

## Low Findings (2)

### 11. Tips Section Lacks Concrete Examples

**Category**: Testing & Maintainability

**Severity**: Low

**Location**: [SKILL.md](SKILL.md#L66-L71)

**Code**:

```markdown
## Tips

- Focus on **intent over syntax**: let linting catch style
- Be **constructive**: frame as "resilience opportunity" not "this is broken"
- Document **unknowns**: explicitly note assumptions needing validation
- Compare **iterations**: each cycle builds on the last; track improvements
```

**Risk**: Tips are abstract. New users don't know what "constructive" or "document unknowns" looks like in practice. Could benefit from side-by-side "before/after" examples.

**Assumption**: Users understand intent from one-liners.

**Recommendation**: (Optional) Add a `./references/examples.md` with 2–3 before/after finding statements showing good vs. poor phrasing.

---

### 12. No Link to a Completed Review Example

**Category**: Testing & Maintainability

**Severity**: Low

**Location**: [SKILL.md](SKILL.md) (entire file)

**Code**: No reference to an example review output.

**Risk**: Users don't know what a "done" review looks like until they generate one. First review takes 2x longer as user guesses at structure and depth.

**Assumption**: Users can infer output format from templates alone. Partially true; an example would accelerate adoption.

**Recommendation**: Include a 40-line anonymized example review in `./templates/example-review.md` showing findings across all severities and dimensions.

---

## Design Trade-offs & Assumptions

### Documented Trade-offs

1. **Lean SKILL.md vs. Progressive Disclosure**: Chose lean (95 lines) + separate references. Trade: Users may not know full scope of dimensions without reading 6+ pages. Win: Keeps agent context low, skill is discoverable and quick to load.

2. **Model-Invocable vs. User-Invoked**: Chose model-invocable (omitted `disable-model-invocation`). Trade: Context load for skill description always active. Win: Agent can fire skill autonomously when code review context detected.

3. **Dimension-Based Organization**: Chose 6 dimensions (arch, risk, security, perf, testing). Trade: Comprehensiveness vs. cognitive load. Win: Consistent structure across reviews, easier to compare iterations.

### Critical Assumptions

- `/docs` directory exists or can be created
- Feature name can be reliably extracted from branch name
- User will read references on first use (or will ask for help)
- One review per feature; no multi-feature branches
- Timestamps have sufficient granularity to avoid collisions
- YAML frontmatter is correctly formatted before parsing
- All changed files are code (not binary or generated); snippets are textual

---

## Metrics

- **Files in skill**: 5 (SKILL.md, 2 references, 2 templates)
- **Total lines**: 350
- **Breakdown**: SKILL.md ~95 lines, references ~190 lines, templates ~65 lines
- **Test coverage**: 0% (no example reviews or CI tests)
- **Documentation**: Complete for workflow, incomplete for edge cases and examples
- **Code path coverage**: 5 procedures defined; not yet executed

---

## Appendix: Recommendations Prioritized for Next Iteration

| Finding | Priority | Effort | Impact |
|---------|----------|--------|--------|
| File system collision (same-second overwrites) | 🔴 Critical | Low | High—blocks production use |
| Missing `/docs` directory handling | 🟠 High | Low | High—common failure mode |
| Feature name extraction fallback | 🟠 High | Medium | High—enables non-standard branches |
| Iteration number tie-breaking | 🟠 High | Medium | High—enables concurrent reviews |
| Completion criterion subjectivity ("at least one per dimension") | 🟡 Medium | Low | Medium—improves review quality |
| Multi-feature scope guidance | 🟡 Medium | Low | Medium—prevents scope creep |
| Quick vs. thorough mode | 🟡 Medium | High | Medium—improves adoption |
| Example review output | 🟡 Medium | Medium | Medium—accelerates first use |

**Blockers for Production**: #1 (collision), #2 (missing dir), #3 (extraction)

**Recommend**: Address critical and high findings before committing to main branch and publishing skill to team.
