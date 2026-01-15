# Claude Code Settings

**Always use the Opus model for any subagents that are launched.**

---

# Development Guidelines

## Architecture Philosophy

**Systems, Not Patches**

- Build unified systems that handle all cases, not individual fixes for individual problems
- One source of truth - if logic exists, it lives in ONE place
- No special-case rules for specific scenarios
- If a "fix" only addresses one situation, step back and design a system that handles ALL similar situations
- When something doesn't work, ask: "What system is missing?" not "What patch can I add?"

**Example:**
- BAD: Adding `GetClusterNetForce()` to ClusterManager that loops over cells and calls ForceZoneManager
- GOOD: Adding `GetNetForceInArea()` to ForceZoneManager itself, so ANY system needing area queries uses the same logic

**Questions to ask before implementing:**
1. Does this logic already exist somewhere? (Don't duplicate)
2. Where should this logic live? (Single responsibility)
3. Will other systems need this? (Design for reuse)
4. Am I adding a special case or extending a system? (Prefer the latter)
