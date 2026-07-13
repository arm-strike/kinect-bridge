# Issue tracker: Local Markdown

Issues and specifications for Kinect Bridge live as Markdown files in `.scratch/`.

## Conventions

- One feature per directory: `.scratch/<feature-slug>/`
- The specification is `.scratch/<feature-slug>/spec.md`.
- Implementation issues are one file per ticket at `.scratch/<feature-slug>/issues/<NN>-<slug>.md`, numbered from `01` — never a single combined tickets file.
- Triage state is recorded as a `Status:` line near the top of each issue file. See `triage-labels.md` for the role strings.
- Comments and conversation history append to the bottom of the file under a `## Comments` heading.

## Cross-project work

Record Kinect Bridge-specific specifications and issues only in this project's `.scratch/`. For work spanning Arm Strike Game, create a matching cross-project specification or reference file in both projects and add a relative link in each file to its counterpart, for example `../../arm-strike-game/.scratch/<feature-slug>/spec.md`. Keep shared decisions and status aligned in both files.

## When a skill says "publish to the issue tracker"

Create a new file under `.scratch/<feature-slug>/`, creating the directory if needed.

## When a skill says "fetch the relevant ticket"

Read the referenced file. The user will normally pass its path or issue number directly.
