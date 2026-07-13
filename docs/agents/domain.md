# Domain Docs

How engineering skills should consume Kinect Bridge's domain documentation when exploring this project.

## Before exploring, read these

- `CONTEXT.md` at this project root.
- `docs/adr/` for ADRs that touch the area being changed.

If these files do not exist, proceed silently. The domain-modeling workflow creates them when terminology or decisions are actually resolved.

## File structure

This is a single-context project:

```
kinect-bridge/
├── CONTEXT.md
├── docs/adr/
└── .scratch/
```

## Use the glossary's vocabulary

When naming a domain concept in an issue, proposal, hypothesis, or test, use the term defined in `CONTEXT.md`. If a needed concept is absent, reconsider the terminology or record the gap for domain modeling.

## Flag ADR conflicts

If work contradicts an existing ADR, state the conflict explicitly rather than silently overriding it.
