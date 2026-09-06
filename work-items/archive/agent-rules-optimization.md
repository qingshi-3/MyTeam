# Agent Rules Optimization

Status: Completed

## Goal and authorization

The user approved optimizing the audited global/project instructions, execution profiles, and skills, explicitly retaining main-only development. Reduce repeated approval, fixed model routing, unnecessary documentation, and validation blind spots.

## Scope and boundaries

- Edit global AGENTS.md, project AGENTS.md, three personal agent profiles and their config descriptions, and the audited personal skills.
- Keep main-only development, read-only research/donor boundaries, state ownership, authored Godot resources, and low-concurrency builds.
- Use persistent user-level instructions for explicit Sites and OpenAI Docs workflow exceptions; do not patch versioned bundled plugin caches or duplicate their skills.
- Preserve the configured main model/provider/reasoning, unrelated configuration, game code, Web code, and research assets.
- Main agent owns integration. Any independent review is read-only; no parallel overlapping edits.

## Confirmed approach

Explicit scoped implementation requests authorize ordinary implementation and verification. Discussion-only requests remain read-only. Ask only for material unresolved decisions or new authority; prior authorization persists. Default to end-to-end ownership; delegation is optional and bounded. Full task documents are for sustained/cross-module/delegated work, not every minor edit. Interactive UI implementation includes scoped browser/input QA; report actual validation coverage. Save compatibility is an explicit support contract. Skills provide technical guidance without establishing competing workflows or document trees.

## Acceptance

Main-only remains explicit in both rule files. No forced old-model execution or universal reconfirmation remains in edited profiles. Personal skill metadata and agent TOML parse. References remain valid. Original files are backed up outside skill discovery before changes. Review realistic small-fix, UI, delegation, save, and local-config scenarios; preserve user authority and external-publication boundaries.

## Progress and recovery

- Audited originals and confirmed current branch is main. Existing research/Web changes are outside this task.
- Backed up all 12 original target files with matching SHA256 before edits to `C:/Users/qs/.codex/backups/agent-rules-20260905-130701`. Backup paths preserve their original layout relative to `C:/Users/qs/` and are outside skill discovery.
- Updated global/project rules, three optional execution profiles, and only the two relevant profile descriptions in root config. Removed model pins from profiles; retained high/xhigh effort choices and all unrelated main configuration.
- Updated six personal skills: using-godot-prompter, playwright, web-design-guidelines, component-system, save-load, and godot-testing. No skills installed/deleted; bundled/system/plugin-cache skills untouched.
- Independent read-only scenario review found two residual example/checklist issues. Corrected missing-required-component failure handling and conditional save serialization/error requirements; follow-up review passed.
- Completed validation and moved this record to the existing archive. No game/Web implementation or protected research changes were made by this task.
- Recovery: restore only exact backed-up files if requested, checking for later user changes before replacement.

## Verification handoff

- Python tomllib parsed config and all three profiles. Full parsed-config comparison with backup confirmed only the intended executor/executor-xhigh descriptions changed. Profiles have no model pin and retain their intended reasoning effort.
- The bundled skill quick validator could not start because PyYAML is absent in both available Python runtimes. Without installing dependencies, used the Web project's existing js-yaml parser to validate all six YAML frontmatters, required/allowed fields, names, description bounds, unfinished placeholders, and every local Markdown resource link; all passed. This is an alternative static check, not a successful run of quick_validate.py.
- Project AGENTS.md passed git diff --check; current branch remains main. Existing unrelated dirty files were preserved.
- Independent read-only scenario review covered scoped UI fixes, diagnosis-only saves, nonoverlapping main-workspace delegation with shared files, unsupported save versions, local settings inspection, and local-only previews. Follow-up review of both corrections found no remaining blocker within the reviewed scope.
- Validation covers instruction coherence and file formats, not actual future agent behavior or Godot execution of illustrative snippets. Linked references were checked for existence, not comprehensively re-audited.
- Active agents may retain previously loaded profile instructions; file changes do not prove that an already-running agent changed its configuration. New sessions/agents should read the updated files; no live model switch is claimed.
