---
name: scan-bugs
description: Scan open bug reports and investigate whether each has been fixed in the codebase. Use when user asks to scan, check, audit, or review bug reports.
allowed-tools: Read, Glob, Grep, Bash, Task
user-invocable: true
---

# Scan Bugs Skill

Scan all OPEN bug reports in `DevPlans/Bugs/` and investigate each one against the current codebase to determine if the bug has been fixed.

## Roles

- **You (the orchestrator)**: Find bugs, read them, launch agents, present summary. Lightweight coordination only.
- **Subagents (investigators)**: Investigate the codebase, determine verdict, and rename the file themselves if FIXED.

## Workflow

### Step 1: Find Open Bug Reports

Use Glob to find all files matching `DevPlans/Bugs/OPEN-*.md`.

### Step 2: Read Each Bug Report

Read each OPEN bug report to extract the full contents for the agent prompt.

### Step 3: Launch Investigation Subagents

Launch exactly **one** Task subagent per bug report — no more, no less. Launch them all in a **single message** with multiple Task tool calls so they run in parallel.

Use `subagent_type: "general-purpose"` and `model: "opus"`.

**Do NOT launch agents in multiple rounds.** All agents must be launched in one batch.

Each subagent prompt must include:
- The full contents of the bug report
- The full file path of the bug report (so the agent can rename it)
- Instructions to investigate, determine a verdict, rename if FIXED, and return a short result

Example subagent prompt:
```
Investigate whether the following bug has been fixed in the codebase.

BUG REPORT:
{full contents of the bug report file}

FILE PATH: G:\Sandy\DevPlans\Bugs\OPEN-{BugName}.md

INSTRUCTIONS:
1. Read the affected code files mentioned in the bug report
2. Search for the specific functions, patterns, or issues described
3. Check if the root cause has been addressed
4. Check if the symptoms described would still occur given the current code

AFTER INVESTIGATION:
- If FIXED: Rename and move the file to the Fixed folder using Bash: mv "G:/Sandy/DevPlans/Bugs/OPEN-{BugName}.md" "G:/Sandy/DevPlans/Bugs/Fixed/FIXED-{BugName}.md"
- If OPEN or UNCLEAR: Do not move or rename.

RETURN FORMAT (keep it short — one line verdict, one line evidence):
VERDICT: [FIXED/OPEN/UNCLEAR]
EVIDENCE: [One sentence summary]
```

### Step 4: Collect Results

Wait for all subagents to complete. Each agent returns a short verdict + one-line evidence summary.

### Step 5: Summary

Present a summary table to the user:

```
| Bug | Verdict | Evidence Summary |
|-----|---------|-----------------|
| ... | FIXED   | ...             |
| ... | OPEN    | ...             |
```

## Important Rules

- **One agent per bug**: Launch exactly one subagent per OPEN bug report. Never duplicate.
- **Single launch**: All agents must be launched in one message. No second round of launches.
- **Agents own the rename**: Each agent renames and moves its own bug file to `DevPlans/Bugs/Fixed/` if the verdict is FIXED. The orchestrator does not rename or move files.
- **Short responses**: Agents return only a one-line verdict and one-line evidence summary. No lengthy analysis. This keeps the orchestrator's context clean.
- **Only check OPEN bugs**: Skip any file not prefixed with `OPEN-`
- **Use Opus model**: All subagents must use `model: "opus"` per project rules
- **Rename only on FIXED verdict**: Only rename files when the evidence clearly shows the bug is resolved
