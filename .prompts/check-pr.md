# Check PR and Actions Logs

## Description
Analyze GitHub Actions workflow logs and PR checks to detect errors, diagnose root causes, and provide fixes.

## Arguments
- `pr_number` (optional): Specific PR number to check. If not provided, checks the current PR or latest workflow run.

## Instructions

1. **Determine which PR/run to check**:
   - If PR number is provided: use that specific PR
   - Otherwise: check the latest workflow run from current context

2. **Fetch the workflow run**:
   - For specific PR: Use `gh run list --workflow=marketplace-cicd.yml --limit 10` and filter by PR number
   - Otherwise: Use `gh run list --limit 5` to get recent runs
   - Use `gh run view <run-id>` to see the full run details
   - Use `gh run view <run-id> --log` to get detailed logs

3. **Analyze the logs** for:
   - Build failures (dotnet restore, dotnet build errors)
   - Test failures (unit/functional test errors)
   - Deployment failures (aspire publish, Azure login errors)
   - Environment variable or secret issues
   - Network/connectivity errors
   - Permission errors

4. **Read relevant source files** if needed:
   - Check the workflow file structure
   - Review relevant .csproj files if build fails
   - Check appsettings.json if config issues
   - Review Program.cs if Aspire issues

5. **Identify the root cause** by examining:
   - Error messages and stack traces
   - Previous successful workflow runs for comparison
   - Related configuration files

6. **Propose and implement fixes**:
   - Edit workflow file if needed
   - Fix source code issues if needed
   - Update environment variables or secrets if needed
   - Commit changes with clear message

7. **Verify the fix** by:
   - Pushing to feature branch
   - Checking that new workflow run passes
   - Confirming in GitHub Actions UI

## Example Usage
```
/check-pr                    # Check latest workflow run
/check-pr 5                  # Check specific PR #5
/check-pr 12                 # Check specific PR #12
```

## Output
- ✅ Identified error(s)
- 🔍 Root cause analysis
- ✨ Applied fix(es)
- ✔️ Verification status
