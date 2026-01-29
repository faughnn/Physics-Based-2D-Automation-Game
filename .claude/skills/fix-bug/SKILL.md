---
name: fix-bug
description: Pick an open bug report, investigate the codebase, and create an implementation plan to fix it. Use when user asks to fix a bug, work on a bug, tackle a bug, or pick up a bug.
allowed-tools: Read, Write, Glob, Grep, Bash, Task, AskUserQuestion, EnterPlanMode
user-invocable: true
---

# Fix Bug Skill

Pick an open bug from `DevPlans/Bugs/`, investigate the codebase, and enter plan mode with a concrete fix plan.

## Workflow

### Step 1: Find Open Bug Reports

Use Glob to find all `OPEN-*.md` files in `G:\Sandy\DevPlans\Bugs\` (top-level only, **not** the `Backlog/` subfolder). Backlog bugs are excluded — they are parked for later.

### Step 2: Let the User Pick

If arguments were provided (e.g. `/fix-bug PlayerFalls`), try to fuzzy-match to one of the open bugs. If there is exactly one match, use it. Otherwise, or if no arguments were given, use **AskUserQuestion** to present the list of open bugs and let the user choose which one to work on. Show the bug filename (without `OPEN-` prefix and `.md` suffix) as the option labels.

### Step 3: Read the Bug Report

Read the full contents of the selected bug report file. Extract:
- Summary and symptoms
- Root cause (if documented)
- Affected code files and line numbers
- Potential solutions already listed

### Step 4: Investigate the Codebase

Launch an **Explore** subagent using the Task tool to deeply investigate the bug:

```
Task tool with subagent_type: "Explore"
model: "opus"
prompt: "Investigate how to fix the following bug.

BUG REPORT:
{full contents of the bug report}

Find:
- All code paths involved in the bug (read the affected files listed)
- The exact root cause — confirm or update what the bug report says
- Related systems, data flow, and call sites
- Similar patterns in the codebase that work correctly (for reference)
- Any existing workarounds, TODOs, or comments about this issue
- Which files need to change and what each change looks like
- Edge cases or risks the fix must handle

Return a detailed technical analysis with specific file paths, line numbers, and code snippets."
```

### Step 5: Ask Clarifying Questions (if needed)

If the investigation reveals ambiguity — multiple valid approaches, unclear requirements, or tradeoffs that need user input — use **AskUserQuestion** to clarify before planning.

Common things to clarify:
- Which solution approach to take (if the bug report lists multiple)
- Whether related issues should be fixed in the same pass
- Scope boundaries (minimal fix vs. broader refactor)

Skip this step if the fix is straightforward and unambiguous.

### Step 6: Enter Plan Mode

Use **EnterPlanMode** to design the implementation plan. The plan should include:

1. **Summary** — One-line description of the fix
2. **Root Cause** — Confirmed technical explanation
3. **Behavior** — What the fix changes from the user's perspective
4. **Files to Modify** — For each file:
   - File path
   - What changes (specific: field additions, method changes, new logic)
   - Key code details (field names, method signatures, algorithms)
5. **New Files** (if any) — Full description of new classes/components
6. **Implementation Order** — Numbered steps with dependencies noted
7. **Verification** — How to confirm the fix works (manual test steps)

The plan should be concrete enough that someone could implement it without re-reading the bug report. Include field names, method signatures, and algorithmic details — not just "update the controller."

### Step 7: Rename Bug Report After Implementation

After the plan is approved and the fix has been implemented, rename the bug report file from `OPEN-{BugName}.md` to `FIXED-{BugName}.md` using the Bash tool:

```
git mv "DevPlans/Bugs/OPEN-BugName.md" "DevPlans/Bugs/FIXED-BugName.md"
```

If the file is not tracked by git, use a regular `mv` instead. This keeps the bug tracker up to date per the project conventions in CLAUDE.md.

## Important Rules

- **Always use Opus model** for subagents per project rules
- **Exclude Backlog bugs** — only show bugs from the top-level `DevPlans/Bugs/` folder
- **Investigate before planning** — never skip the Explore step; plans must be grounded in actual code analysis
- **Confirm root cause** — the bug report's root cause may be wrong or incomplete; the investigation should verify it
- **Follow architecture philosophy** — prefer systems over patches, single source of truth, no special cases (see CLAUDE.md)
- **Respect separation of concerns** — simulation vs game vs sandbox layers (see CLAUDE.md)
- **One bug at a time** — this skill handles a single bug per invocation
