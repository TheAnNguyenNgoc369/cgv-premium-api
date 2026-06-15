# Read First

Always read this file before doing anything.

This file contains workflow and execution rules.

For repository structure, module locations, search keywords, and implementation patterns, read:

AGENT.md

Only read the relevant sections of AGENT.md needed for the current task.

# Additional Context

Use:

- SKILL.md → workflow and rules
- AGENT.md → repository map and implementation patterns
- PROMPT.md → reusable task templates

Do not read entire documents if only a small section is needed.

---


# Core Workflow

1. Do not start coding immediately.
2. Search before reading files.
3. Read only files relevant to the task.
4. Analyze first.
5. Create a plan.
6. Implement with minimal changes.
7. Validate.
8. Report.
9. Stop.

---

# Scope Rules

Only modify files directly related to the task.

Do NOT:

* Scan the entire repository.
* Refactor unrelated code.
* Rename files/classes without reason.
* Reorganize folders.
* Introduce new architecture patterns.
* Add packages unless necessary.

Keep diffs small.

---

# File Discovery Rules

Before reading files:

* Search first.
* Identify the minimum set of files.
* Follow existing implementation patterns.

Use:

* grep
* rg
* code search

before opening files.

Large files should be read partially when possible.

---

# Planning Rules

Before implementation, report:

## Findings

* Existing implementation
* Relevant files
* Existing patterns
* Risks

## Plan

File-by-file changes.

---

# Architecture Rules

Follow existing project architecture.

Prefer:

1. Reuse
2. Extend
3. Create new

Never create parallel implementations when an existing pattern already exists.

---

# Validation Rules

Always perform the minimum validation required.

Examples:

Feature:

* build
* verify behavior

Bug Fix:

* reproduce
* fix
* verify

Database:

* verify migration
* verify startup

Do not run broad commands when targeted validation is sufficient.

---

# Large Change Confirmation

Require confirmation before:

* > 10 modified files
* Authentication changes
* Database schema changes
* New packages
* Cross-layer refactors

---

# Token Efficiency

* One session = one task.
* Grep first, read later.
* Avoid reading unrelated files.
* Avoid long explanations.
* Stop after task completion.
* Do not search for extra improvements.

---

# Final Report

## Findings

## Changes Made

## Validation

## Risks

## Next Steps
