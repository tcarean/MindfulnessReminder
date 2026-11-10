# The kit's decision records

A closed copy, taken on 2026-11-10, of the 36 decision records of `ClaudeCodeStarterKit`, the
starter kit whose process this project adopted on 2026-10-09 (ADR 0001) and whose files it has
carried itself since it moved to a repository of its own (ADR 0038). Every "the kit's ADR
NNNN" in this repository names the file `NNNN-*.md` in this folder.

- The numbers are the kit's and are independent of the project's own, in `docs/decisions/`.
  Inside these files a bare "ADR NNNN" is another file of this folder.
- The files are not edited and none is added. A process decision made from 2026-11-10 on is a
  project ADR in `docs/decisions/`, whose text names the record here that it replaces.
- `scripts/check-decisions.ps1` reads `docs/decisions/*.md` only, not this folder.
- The records were written for a kit shared by several projects in one repository. What they
  say about the monorepo, `scripts/sync.ps1`, `.githooks/` and the `Starter` sample describes
  that setting, not this repository.
