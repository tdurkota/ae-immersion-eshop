# Fleet Mode Best Practices

## Worktree Pattern (Essential for Parallel Subagents)

When dispatching **5+ parallel subagents** that write to files or perform git operations:

### Pattern
1. **Create worktree per subagent**: Each agent gets its own isolated git worktree
   ```bash
   git worktree add ../agent-<task-id> <base-branch>
   cd ../agent-<task-id>
   ```

2. **Agent works independently**: No file locks, no git state conflicts
   - Writes to `<task-id>.md` or subdirectories
   - Can git add/commit without impacting other agents

3. **Merge after completion**: Agent pushes branch or creates PR
   - Worktree branch merged to main
   - No concurrent write conflicts

4. **Cleanup**: Remove worktree after merge
   ```bash
   git worktree remove ../agent-<task-id>
   ```

### Benefits
- ✅ Eliminates file lock issues
- ✅ Each agent has isolated git state
- ✅ Enables true parallel git operations
- ✅ Scales safely to 20+ agents

### When NOT needed
- Single subagent or sequential work
- Agents write to completely different directories (low collision risk)
- Quick in-memory work (no file I/O)

## Coordination Orchestrator

- Always create an **Orchestrator session** for fleet work
- Orchestrator maintains:
  - Todo tracking (status: pending, in_progress, done, blocked)
  - Dependency graph (todo_deps)
  - Summary of all agent work
- User can query orchestrator for real-time status

## Monitoring
- Poll todo status every 2-3 minutes to catch blockers early
- If agent stalls >5 min on simple task, investigate (read_agent to see logs)
- Flag file lock errors immediately—likely need worktree setup

---
*Pattern established: 2026-09-15 (ae-immersion-eshop parallel-subagents fleet)*
