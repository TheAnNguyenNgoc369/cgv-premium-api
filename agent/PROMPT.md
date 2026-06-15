# ANALYSIS ONLY PROMPT
Read SKILL.md first.
Do NOT implement.

Task:

<task>

Your job:

1. Find relevant files.
2. Identify existing patterns.
3. Identify risks.
4. Produce a file-by-file plan.

Output:

## Findings

## Relevant Files

## Risks

## Plan

Stop after analysis.

---

# IMPLEMENTATION PROMPT

Read SKILL.md first.

Task:

<task>

Requirements:

* Follow existing patterns.
* Modify the minimum number of files.
* Do not refactor unrelated code.
* Validate changes.

Output:

## Findings

## Changes Made

## Validation

## Risks

Stop after implementation.

---

# TASK BREAKDOWN PROMPT

Read SKILL.md first.

Do NOT implement.

Break the request into the smallest independent tasks.

For each task provide:

* Task Name
* Goal
* Search Starting Points
* Expected Files
* Validation
* Stop Condition

Each task must be executable in a fresh session.

Output only the breakdown.

---

# BUG FIX PROMPT

Read SKILL.md first.

Task:

<bug description>

Requirements:

1. Reproduce issue.
2. Find root cause.
3. Implement minimal fix.
4. Verify fix.
5. Add tests if applicable.

Stop after completion.

---

# ADD NEW FEATURE PROMPT

Read SKILL.md first.

Task:

<feature description>

Requirements:

1. Find existing similar feature.
2. Follow existing architecture.
3. Implement minimal changes.
4. Add validation.
5. Add tests.
6. Validate.

Stop after completion.
