# Read First

Always read this file before doing anything.

This file contains workflow and execution rules.

For repository structure, module locations, search keywords, and
implementation patterns, read: AGENT.md

Read only the sections of AGENT.md relevant to the current task.

# Supporting Files

- SKILL.md → workflow and rules
- AGENT.md → repository map and implementation patterns
- PROMPT.md → reusable task templates

---

# Core Workflow

1. Do not start coding immediately.
2. Search before reading files.
3. Read only files relevant to the task.
4. Analyze the existing implementation.
5. Create a plan and report it.
6. Implement with minimal changes.
7. Validate the change.
8. Report findings.
9. Stop.

---

# Scope Rules

Only modify files directly related to the task. Keep diffs small.

Do NOT:

- Scan the entire repository.
- Refactor unrelated code.
- Rename files or classes without a clear reason.
- Reorganize folders.
- Introduce new architecture patterns unprompted.
- Add packages unless strictly necessary.

---

# File Discovery Rules

Before reading any file:

1. Search with `grep` or `rg` to locate candidates.
2. Identify the minimum set of files needed.
3. Read large files partially when only a section is relevant.
4. Follow existing implementation patterns found during search.

Never open a file speculatively.

---

# Planning Rules

Before writing any code, output:

## Findings

- Existing implementation relevant to the task
- Relevant files and their roles
- Patterns already in use
- Risks or side effects

## Plan

List each file to be changed and describe the change.

Proceed only after the plan is stated.

---

# Architecture Rules

Follow the existing project architecture strictly.

Order of preference:

1. Reuse an existing mechanism.
2. Extend an existing mechanism.
3. Create something new only when no existing pattern applies.

Never create a parallel implementation when an existing one already exists.

---

# Validation Rules

Perform the minimum validation that confirms correctness.

| Scenario   | Required steps                        |
|------------|---------------------------------------|
| Feature    | Build → verify behavior               |
| Bug fix    | Reproduce → fix → verify resolved     |
| Database   | Verify migration runs → verify startup|

Do not run broad commands when a targeted check is sufficient.

---

# Large Change Confirmation

Require explicit confirmation before proceeding when the task involves:

- More than 10 modified files
- Authentication or authorization changes
- Database schema changes
- New third-party packages
- Cross-layer refactors

---

# Token Efficiency

- One session = one task.
- Grep first, read later.
- Do not read files unrelated to the task.
- Avoid long explanations unless asked.
- Stop immediately after task completion.
- Do not search for additional improvements.

---

# Final Report

## Findings

## Changes Made

## Validation

## Risks

## Next Steps