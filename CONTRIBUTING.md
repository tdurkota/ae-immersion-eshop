# Contributing to eShop

Thank you for your interest in contributing to eShop! We're excited to collaborate with you and see how together we can improve and evolve this sample application.

## Getting Started

If this is your first visit, a great way to begin is by tackling issues tagged as `"help wanted"` or `"good first issue"`. These are specially curated to help you get acquainted with the project and make a meaningful impact early on.

## Spot a Typo?

Typos and other small fixes are important to us -— no contribution is too small! If you spot something small, go ahead and open a Pull Request. For these types of contributions, there's no need to create a separate issue.

## Have a Suggestion?

If you have a suggestion on how to enhance eShop, please open an issue with the following details:
- A clear title and description of the suggestion
- Any relevant examples or mockups
- Indicate whether you're interested in implementing the feature yourself

We'll review your suggestion and have a discussion about its potential inclusion in the project.

## Contribution Principles

When considering contributions, we're guided by several principles that align with the project's vision:

- **Best Practices**: We want this sample to be canonical and reflect the best practices in the industry and in .NET.

- **Selectivity in Tools and Libraries**: There is a rich ecosystem of tools, projects, and libraries out there, and we cannot use all of them in this sample. We would like this repo to reflect the use of a realistic set of technologies, not to be a showcase or example of every possible thing it could use.

- **Architectural Integrity**: We welcome refactoring and architectural improvements, provided they're justified. Large-scale changes should come with a clear rationale, such as significant enhancements to the application's design or performance.

- **Enhancing Reliability and Scalability**: We welcome contributions that improve the application's reliability and scalability. These could include updates to error handling, redundancy mechanisms, data access, and any other changes that help eShop operate more robustly under load. We'd love to see relevant test scenarios or metrics for these contributions.

- **Performance Enhancements**: If you're looking to speed up eShop, we're all for it! Please include benchmark comparisons to demonstrate the improvements. Performance improvements that make the code less readable or canonical may have more scrutiny applied.

## Code of Conduct

To ensure a welcoming and positive environment for everyone, please adhere to our Code of Conduct. Respectful collaboration is key to a successful project.

## Local Development Setup

To ensure code quality and catch issues early, we use automated linting and pre-commit hooks.

### Prerequisites
- Node.js and npm installed
- .NET 10.0 SDK

### Setting Up Your Environment

1. Install npm dependencies:
```bash
npm install
```

1. Initialize git hooks:
```bash
npm run prepare
```

This installs pre-commit hooks that will run linting checks before each commit.

### Linting

We use markdownlint to catch markdown formatting issues:

**Check markdown files:**
```bash
npm run lint
```

**Auto-fix markdown issues:**
```bash
npm run lint:fix
```

### Before You Push

Pre-commit hooks run automatically, but you can also run checks manually:

```bash
# Check all markdown files
npm run lint

# Fix auto-fixable issues
npm run lint:fix

# Run E2E tests
npm run test:e2e

# Build all projects
dotnet build eShop.slnx
```

**Never disable or bypass pre-commit hooks.** If a check fails, fix the underlying issue.

### Coding Standards

- **Markdown**: Follow markdownlint rules (see `.markdownlint.json`). Prioritizes clarity over style.
- **.NET Code**: Follow C# conventions from `Directory.Build.props`:
  - Treat warnings as errors (`TreatWarningsAsErrors: true`)
  - Enable nullable reference types (`Nullable: enable`)
  - Use latest C# language features (`LangVersion: latest`)
- **Tests**: Add test coverage for new features. Follow patterns in `tests/API.ContractTests/`
- **Commits**: Write clear, descriptive commit messages. Reference issue numbers when applicable.

### Common Issues

**Markdownlint pre-commit check fails:**
```bash
# Fix automatically
npm run lint:fix
```

**Pre-commit hook doesn't run:**
Ensure the hook is executable:
```bash
chmod +x .husky/pre-commit
```

**Can't commit after fixing issues:**
Stage the fixed files:
```bash
git add .
git commit -m "Fix markdown linting issues"
```
