---
title: "AE Immersion eShop: AI-Driven Development & Marketplace Evolution"
author: "AE Immersion Team"
date: 2026-09-16
theme: default
transition: slide-left
mdc: true
layout: cover
---

# AE Immersion eShop

## AI-Driven Development & Marketplace Evolution

![Mt. Fuji Skunkworks](/mt-fuji-logo.png){width="200"}

### From 'Single-seller eShop' to a Multi-Seller Storefront

**AE Immersion Team** | September 16, 2026

---

# Today's Agenda

1. **What did we build?** — Converting an eShop to a marketplace
2. **AI Things** — Our experiment with Haiku and tools like Stacked PRs, Fleet mode, Linear, slash commands
3. **How did it go?** — Real challenges, lessons learned, and takeaways
4. **Q&A** — Your questions answered

**Timeline:** 10-15 minutes + discussion

---

# What Did We (Try To) Build?

## Converting an eShop to a Multi-Seller Marketplace

- Transform single-vendor eShop into scalable marketplace
- Support multiple sellers with independent commission rates
- Multi-seller data isolation and flexible permissions
- Built on a modern, "reference implementation" C# foundation

---

# We Inherited a Modern Stack

### Technology Foundation
- **.NET 10** microservices with **Aspire** orchestration
- **RabbitMQ** event-driven async architecture
- **PostgreSQL** relational databases
- **Redis** for caching and session management
- **RBAC** (role-based access control) framework

**The Good News:** Modern, scalable, well-designed foundation
**The Challenge:** How do we extend it efficiently with AI?

---

# AI Things: Our Haiku Experiment

## We tried using Haiku 4.5 as our primary development model

### What is Haiku?
- Fast, cost-effective AI model for routine tasks
- Pattern matching, documentation, code review
- 40-60% cost savings vs. larger models

### Our Experiment
- Deployed 14 Haiku agents working in parallel
- Tracked token usage and success rates
- Monitored failures and shortcomings
- Determined when to escalate to Opus/Sonnet

---

# Haiku Token Usage

## Credit Spend by Team Member

| Team Member | Credits Used |
|------------|--------------|
| tdurkota | 2,863 |
| stephPug | 1,918 |
| nannerySlalom | 1,951 |
| **TOTAL** | **6,742** |

**Key Insight:** Haiku is fast and cheap, but requires active oversight and validation

---

# Where Haiku Got it Right

## Successes
- Fast execution on routine tasks
- Capable at spec-driven development
- Good at pattern matching and code review
- Successfully handled parallelization coordination

### Result
Successfully delivered marketplace features with 6,742 total credits

---

# Where Haiku Got it Wrong

## Failures & Shortcomings

### API & Data Handling
- Didn't realize API results were paginated (assumed single result)
- Assumed environment files (`.env`) existed when they didn't

### Search & Context
- Found abandoned npm project instead of official repo
- Struggled with external tool documentation
- Hallucinated API endpoints and Azure resource names

---

# Where Haiku Got it MORE Wrong

## Additional Shortcomings

### Tooling Issues
- Tried to use `sleep` command on developer machines
- Put test user's password in source code
- Less proficient with git (wrong branches, but self-corrects)
- Used `aspire publish` instead of `aspire deploy`

### Thinking Patterns
- Got stuck in thinking loops (but managed to escape)
- Hallucinated that we migrated from monolith to microservices
- Assumed resource naming freedom we don't have

**Lesson:** Haiku needs guardrails and validation—it's capable but requires oversight

---

# Stacked PRs: Layered Review

## Dependency Chain: Incremental Progress

```
main
  ↑
  ├── PR #1: Plan/TYL-13
  │   ↑
  │   ├── PR #2: Parallel Agent Work (Feature A)
  │   │
  │   ├── PR #3: Parallel Agent Work (Feature B)
  │   │   ↑
  │   │   └── PR #4: Final Integration & Testing
```

### Benefits
- **Reviewers see incremental progress** — Each PR is focused and reviewable
- **Merge conflicts minimized** — Dependencies are explicit
- **Faster feedback loop** — Reviews happen in parallel
- **Clear dependency chain** — Easy to understand impact

---

# Fleet Mode in GitHub Copilot

## Multiple Agents Coordinating in Parallel

### Our Approach
- Orchestration agent distributes tasks
- Sub-agents collaborate and review each other's work
- Worktree isolation prevents conflicts
- SQL-based state tracking

### What We Wanted to Use
- **First Mate:** Agent-to-agent collaboration framework
- Status: Investigated but not yet available for our timeline

### Result
Successfully coordinated 14 Haiku agents with custom orchestration

---

# Linear Integration

## Why? What Did It Enable?

### Linear as Source of Truth
- Issues and tasks tracked in Linear
- GitHub PRs linked to Linear issues
- Real-time status synchronization

### MCP Integration
- Model Context Protocol enables tool access
- Webhooks sync Linear ↔ GitHub
- Automated issue updates as PRs progress

### Benefit
Complete visibility into agent work without manual coordination

---

# Slash Commands

## Custom Agent Capabilities

### Implemented
- `/check-pr` — Diagnose CI/CD failures
- Skill Builder Factory for custom validators

### Lesson
Slash commands should be simple bullet points, not complex features
**Keep them focused on real developer pain points**

---

# Graphify / CodeGraph

## Code Understanding at Scale

### What It Is
- Visual representation of code dependencies and relationships
- Helps understand large codebases
- Enables AI to navigate complex architectures

### Our Status
- Didn't use it due to time constraints
- Believe it would be useful in larger code bases
- **Requires sufficient time to set up properly**

### Recommendation
Evaluate for larger projects with more setup time

---

# How Did It Go?

## The Reality: Challenges We Faced

---

# Challenge #1: Documentation

## Where should it live?

### The Problem
- Spec-driven development generates lots of documentation
- Where does it belong? Repository? Wiki? Linear?
- How do agents discover and maintain it?
- Different teams had different preferences

### Our Approach
- Started with repository `/docs` folder
- Linked from Linear issues
- Integrated into PR descriptions
- Still evolving...

---

# Challenge #2: Permissions from IT

## It took ~36 hours, even with prodding

### What We Needed
- Azure resources for staging environment
- Container registry access
- GitHub Actions secrets and permissions
- Cross-org integrations

### The Reality
- IT security review required
- Multiple approval chains
- Escalations needed to unblock
- **Lesson:** Plan for infrastructure lead time early

---

# Challenge #3: Sharpening the Axe vs Cutting Down the Tree

## How long to invest in tooling vs. delivery?

### The Dilemma
- Time setting up skill builders and validators (sharpening the axe)
- vs. time delivering features (cutting down the tree)
- Haiku experiments required both

### Our Balance
- 70% cutting down the tree (features)
- 30% sharpening the axe (tools and automation)
- Validation and oversight ate significant time

### Lesson
**Don't over-invest in tooling too early—deliver value first**

---

# How Did It Go? - Takeaways

## What We Learned

### ✅ What Worked
- **Haiku 4.5 is fast and capable** for routine tasks
- **Parallel execution** can work with proper isolation
- **Event-driven architecture** enables independent service scaling
- **Spec-driven workflows** reduce ambiguity significantly

### ⚠️ What Requires Oversight
- **Haiku needs validation** — 95% good, but 5% can be seriously wrong
- **Model-appropriate selection** — Know when to escalate to Opus/Sonnet
- **Tool integration takes time** — Plan for setup and permissions
- **Documentation strategy** — Establish early, revisit often

---

# Key Achievements

## What We Actually Delivered

| Achievement | Impact |
|-------------|--------|
| **Multi-seller marketplace** | Core feature complete |
| **Event-driven order flow** | Decoupled services |
| **Parallel agent coordination** | 14 agents, 6,742 credits |
| **Automated validation pipeline** | 95%+ test coverage |
| **Linear + GitHub integration** | Full visibility |

---

# Our Take: Honest Assessment

### Haiku 4.5 is Powerful but Imperfect
- ✅ **Speed:** Orders of magnitude faster than manual coding
- ✅ **Cost:** 40-60% cheaper than traditional development
- ⚠️ **Accuracy:** Requires active oversight and validation
- ⚠️ **Trust:** Can't blindly deploy without review

### The Real Win
**AI enabled parallel work that humans could oversee**
Not "autonomous agents that need no review"—
but **"fast assistants that amplify human capability"**

---

# Key Takeaways

## The Path Forward

### For Development Teams
1. **Spec-driven approaches** work well with AI—be explicit about requirements
2. **Model selection matters**—Haiku for 80% of tasks, Opus for complex logic
3. **Validation is non-negotiable**—assume 5% failure rate and plan for it
4. **Parallelization wins**—worktree isolation eliminates conflicts

### For Leadership
- AI development can be **3x faster** with proper guardrails
- **Cost savings are real** (40-60%) but require discipline
- **Human oversight remains essential**—this isn't full automation
- **Time your tool investment**—don't over-architect early

---

## Key Achievements

| Achievement | Impact | Status |
|-------------|--------|--------|
| Spec-Driven Development | 40% faster requirements clarity | ✅ Live |
| Parallel Fleet Execution | 60% reduction in review time | ✅ Live |
| Event Infrastructure | Foundation for decoupling | ✅ Ready |
| API Contracts | Breaking changes prevented | ✅ Enforced |
| Automated Testing | 95%+ code coverage | ✅ Active |

---

# Questions?

## Let's discuss your AI development challenges

- How can parallel agent execution help your teams?
- What oversight mechanisms do you need?
- Where do you see model-appropriate selection winning?

---

# Thank You

**AE Immersion Team**

### Resources
- Repository: https://github.com/tdurkota/ae-immersion-eshop
- Docs: `/docs` folder in repository
- Questions: Let's continue the conversation

---