---
name: wrap-up
description: Close a task - run the gate, update CHANGELOG, requirements, decisions and plans, then commit on the task branch and propose the merge. Use at the end of every session or when the user says /wrap-up.
---

# Wrap up

Run through every step; report what each one found. Do not skip a step because it "probably"
does not apply.

1. Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1`. Paste the last
   ten lines: they say whether the process tests ran. If it is red, fix it or stop and report;
   never continue with a red gate.
2. There is no numeric code, no validation case and no golden file (ADR 0039): say so. If the
   session added a case, run `/validate` and paste its drift table; a figure that moved is a
   finding, not something to re-measure into place (ADR 0033).
3. `git diff` the whole change. Read it back. Write a three-sentence plain-language summary:
   what changed for the user, what changed inside, what did not change that someone might expect.
   Every type and method the diff adds or edits in the code has its documentation comment, with
   a tag for each parameter and for the return; add what is missing (the rule
   "Documentation comments").
4. `CHANGELOG.md`: a change a reader of the version history cares about (behaviour added,
   changed or removed, a fix, a figure that moved, a refactor worth knowing about) has a line
   under `[Unreleased]`, in the right subsection (Added, Changed, Fixed, Removed; Internal). A
   review follow-up, a test-only change, a comment or doc fix gets none; nothing checks this at
   commit, so it is decided here (ADR 0023). Say which way you decided.
5. `docs/requirements.md`: statuses match reality. A requirement touched by this task that is
   still `draft` is a question for the user, not something to leave. A new `R###_` test moves
   its requirement to `implemented`.
6. `docs/decisions/`: any choice made in this session that someone could later ask "why?" about
   has an ADR, including a choice settled in chat that happened to match convention. If unsure,
   list the candidates and ask. An ADR still `proposed` while code built on it is about to be
   committed is a question for the owner: accept it, or change the code. An ADR already merged
   into `main` changes three ways only (ADR 0024): a typo, an unclear phrase or a
   renamed path is fixed in place; a wrong figure, or a reason that no longer holds while the
   decision stands, gets a dated `(Corrected|Restated YYYY-MM-DD: ...)` note next to it, or
   right after `## Decision` when the text is inside it; anything that changes what is done is
   a new ADR that supersedes it, in part or whole, and the old one changes only its Status.
   The Status and Supersedes lines may always be completed so both sides agree. Look at
   `git diff main...HEAD -- docs/decisions/` and say which of these each edit to an ADR
   already on `main` is. A new ADR takes the next number of the one sequence (ADR 0075).
7. `docs/plans/`: if a plan was followed, note at its end what deviated and why.
8. `docs/validation.md`: if figures were re-measured, the date and the output are recorded.
9. `README.md` is the user documentation: if the change is user-visible (a mode, an option, a
   window, a switch), the text there describes it. If the change touched `CLAUDE.md`,
   `.claude/rules/`, `process.json`, `.claude/settings.json`, `scripts/`, `.claude/skills/` or
   `.claude/agents/`, quote what `README.md` says about the touched piece and fix what no
   longer holds, in this task (ADR 0011). A change to the reminder window is looked at with
   `--preview`, one to the activity monitor with `--detect`, before the commit. Any other change
   under `src/MindfulnessReminder.App/` is listed in the wrap-up report for the owner to look at
   in their own copy after the merge: a full tray start of the worktree's build would repair
   the auto-start entry to it (R-006).
10. `docs/design/`: in the project folder of the task's worktree, not the main checkout, run
    `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check-design.ps1 -Since main`.
    It lists the notes whose `Covers:` sentences name a code or test file this branch changed
    (the folders and files `process.json` lists). Read each one against the change and update or
    delete what no longer holds, in this task (the rule "Design notes"). Code whose
    workings a reader needs and that no note covers gets a note with a `Covers:` sentence that
    names its files as paths, and a row in `docs/design/README.md`. The gate checks the index and
    the paths, not the text.
11. If the commit's folder is the main checkout on a `claude/` branch, move the work to the
    branch's own worktree first: the git hook refuses a session's commit there (ADR
    0026). The main checkout is where the two lines of
    `git rev-parse --absolute-git-dir --path-format=absolute --git-common-dir` are equal. On a
    `claude/` branch in its own worktree, commit without asking (ADR 0021): one line,
    English, at
    most 100 characters, saying why. No body, no trailers, no `Co-Authored-By` (the summary
    from step 3 stays in the chat; the "what" is in the diff and, when it warrants one, the
    CHANGELOG).
    Report the message and the short hash. On `main` or any branch not named `claude/`, do not
    commit: propose the message and leave the commit to the owner (the commit hook refuses it
    anyway). If the branch is ready to merge into `main`,
    say so, suggest `/code-review main...<branch>` first if it has not run on this work, then
    ask: does the owner want you to run the merge, or will they do it by hand? Do nothing until
    they answer. The merge is `git merge --no-ff <branch>` in the main checkout, so that the git
    hooks see it: a fast-forward runs none (ADR 0022, 0026). If a merge you run stops on conflicts, resolve them, `git add` the files and
    conclude with `git merge --continue`, never `git commit`, which the hook refuses on `main`;
    a conflict where the two sides meant different things is a question for the owner. If the
    gate fails only on the merged result (two ADRs with the same number), `git merge --abort`,
    fix it on the task branch with a commit there, and merge again. Once the merge is done, delete the branch with `git branch -d`; if a worktree
    holds it, release it first (`git switch --detach` from the session inside that worktree,
    `git worktree remove <path>` from outside). `-D` only when the owner asks. Never
    `--no-verify`.
12. If anything was left unfinished, say so in one list, with what is needed to finish it.
