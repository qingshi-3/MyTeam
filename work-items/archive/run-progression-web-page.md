# Run Progression Web Page

Status: Completed

## Goal

Replace the Markdown-based delivery of the ten confirmed run-progression mechanism concepts with an image-first Web page inside the existing mechanism-atlas Sites project. The Web page becomes the complete user-facing carrier for the concepts and their twenty generated mechanism diagrams.

## Confirmed Solution

- Reuse the existing `web/game-mechanics-atlas/` application and published Sites project; add an isolated `/run-progression` route rather than creating another site or replacing the atlas homepage.
- Preserve the twenty generated raster diagrams as the primary explanation. Web code presents the images and navigation; it must not redraw the mechanism diagrams with HTML, CSS, Canvas, or SVG.
- Store all ten concepts as structured Web data. The running page must not parse or depend on Markdown.
- Give the page an image-first browsing flow:
  - a compact ten-concept selector and quick comparison entry;
  - one selected concept at a time;
  - its complete-run overview and key decision-loop image shown prominently;
  - only six concise fields: randomized problem, player action, build payoff, progression pressure, terminal condition, and principal risk;
  - previous/next navigation and an accessible enlarged-image view;
  - responsive mouse, keyboard, touch, and reduced-motion behavior.
- Move, rather than duplicate, the twenty source PNG files into the site's public asset surface. Render only the selected concept's two images and lazy-load noncritical imagery so the roughly 47.5 MiB asset set is not fetched at once.
- Remove the old user-facing `README.md` and `PROMPTS.md` delivery surface after its content and images have been migrated. Internal work-item Markdown remains project process memory, not user-facing content.
- Add a clear entry from the existing atlas to the new route without changing the atlas catalog, persistence schema, mechanic semantics, or existing user data.
- Build, test, and publish the validated source to the existing public Sites project. Preserve its current public access and `noindex` policy.

## Authority Impact

- The ten concepts remain exploratory design material. This migration changes only their presentation medium.
- Nothing in this task promotes a concept into `gameplay-design/` authority or authorizes Godot implementation.
- The existing atlas remains an exploratory design tool; the new route is an adjacent exploration surface.
- The archived image-generation task remains historical execution evidence. This work item is the authority for the Web migration and its final delivery location.

## Scope

- One new route and its route metadata.
- Structured data for exactly ten concepts and twenty unique local image paths.
- A focused browser component and scoped responsive styling.
- An atlas navigation entry to the route.
- Move the twenty generated images into the existing site's public assets.
- Remove the superseded Markdown delivery package once migration is verified.
- Focused automated coverage for route rendering, data cardinality, unique image paths, and asset existence.
- Existing site tests, production build, code check, publication, and post-deployment reachability check.

## Non-Goals

- Redesigning the atlas homepage or its six existing work views.
- Adding accounts, server persistence, D1, R2, analytics, or third-party dependencies.
- Editing, regenerating, compositing, or otherwise changing the twenty accepted images.
- Adding more mechanism concepts, lore, balance numbers, or implementation estimates.
- Changing Godot runtime, content, gameplay authority, system architecture, or any other active work item.
- Turning this exploration into an accepted roadmap decision.

## Constraints

- Work on `main`; do not create or switch branches.
- Preserve the dirty worktree and all unrelated user changes.
- Only one write-capable execution Agent may modify the project for this task.
- Keep all Web changes inside `web/game-mechanics-atlas/`, plus removal of the superseded isolated exploration package and maintenance of this work item.
- Preserve `web/.gdignore`, the existing package manager and lockfile, Sites project id, public access mode, and local-storage contracts.
- Do not add a Markdown renderer or make deployment depend on repository Markdown.
- Keep player-facing text Chinese and stable ids/filenames ASCII/English.
- Use the existing image files without overwriting or regenerating them.

## Acceptance Criteria

- `/run-progression` is a complete standalone user-facing presentation of all ten concepts and does not require opening a Markdown file.
- The page exposes exactly ten materially different run frameworks and exactly twenty unique local generated images, two per concept.
- Switching concepts updates both images and all six concise mechanism fields; previous/next and direct selection remain operable by keyboard and touch.
- Enlarged image viewing has a visible close path, focus-safe keyboard behavior, descriptive alternative text, and no color-only critical state.
- Initial rendering does not mount or eagerly fetch all twenty full-size images.
- The atlas offers a visible route entry, while its catalog, persistence schema, saved workspace behavior, and existing views remain unchanged.
- No old user-facing `README.md` or `PROMPTS.md` is required or retained as the concept carrier; no duplicate concept image package remains under `docs/design-explorations/run-progression-mechanisms/`.
- Automated checks prove ten records, twenty unique image paths, all referenced assets present, and the route's server-rendered product shell.
- Existing tests, production build, and lint pass without changing dependencies.
- The existing Sites project deploys successfully, and the public `/run-progression` URL returns the expected page without authentication.
- No Godot/runtime, gameplay-authority, system-design, or unrelated active-task file is changed.

## Progress

- 2026-08-30: User rejected Markdown as the delivery carrier and requested a Web page.
- 2026-08-30: Read-only inspection found an existing vinext/Next Sites project at `web/game-mechanics-atlas/`; a new site is unnecessary.
- 2026-08-30: User confirmed the isolated-route, image-first, structured-data, old-package-removal, and existing-site publication plan.
- 2026-08-30: Activity document created. Implementation has not yet started.
- 2026-08-30: Executor started implementation after reading the task, project routing rules, atlas README, archived concept record, Sites building instructions, and scoped Git status. Confirmed `main`, isolated untracked task surfaces, no dependency changes, and no authority/runtime changes.
- 2026-08-30: Added the isolated `/run-progression` route, structured ten-record data module, selected-concept browser, quick comparison, previous/next controls, accessible enlarged-image dialog, responsive/reduced-motion styling, and an atlas navigation entry. Moved all twenty accepted PNGs into `public/run-progression/`; no images were regenerated or duplicated.
- 2026-08-30: Verified exactly ten structured records, twenty unique referenced PNG paths, twenty present non-empty assets (47.54 MiB), and no remaining old delivery package. Removed the superseded exploration `README.md`, `PROMPTS.md`, and Godot `.import` sidecars with their now-empty package directory after asset resolution passed.
- 2026-08-30: Validation passed: production build exposes `/` and `/run-progression`; all 20 automated tests pass; ESLint passes with no warnings. No dependency, hosting configuration, catalog, persistence, Godot, gameplay-authority, or system-design change was made.
- 2026-08-30: Independent verification returned PASS, and the parent Agent independently reran the production build, 20/20 tests, and lint successfully. Publication resumed under the confirmed public-deployment authorization.
- 2026-08-30: Sites preflight confirmed the existing project is active, currently public, owned by the current user, and live at `https://tower-mechanics-atlas.lingqi0131.chatgpt.site`; access settings were left unchanged.
- 2026-08-30: Copied the verified site source into an isolated OS-temporary Git checkout, confirmed a 49-file SHA-256 manifest match, committed and pushed `main` at `255200c73e66f0d867efeed30cd8331d2846abb1`. The root repository index and unrelated dirty worktree were never staged or committed.
- 2026-08-30: Packaged the already validated project build with the official Sites `package-site.sh`; the archive contained the required vinext entrypoint and hosting metadata. Sites saved exactly one new version, version 7, from the pushed commit.
- 2026-08-30: Published version 7 to the existing public project. Deployment reached `succeeded` at `https://tower-mechanics-atlas.lingqi0131.chatgpt.site` without changing access settings. A final Sites read confirmed `active`, `public`, and latest version 7.
- 2026-08-30: Anonymous production HTTP checks passed: `/run-progression` returned 200 with both the route title and “十种完整局内进程” marker; `/` returned 200 with the atlas title and workspace-restoration marker. The isolated temporary source checkout and archive were removed after verification.
- 2026-08-30: Parent final production verification passed. Sites remained `active`, `public`, latest version 7, and on the expected live URL. Anonymous checks confirmed `/run-progression` returned 200 with the page title, ten-framework content, territory concept, and first-image reference; `/` returned 200 and its production AtlasApp bundle contained the `/run-progression` “局内进程” entry. HEAD requests for the first and last PNG returned 200 `image/png`, and the root Git staging area remained empty. All acceptance criteria are satisfied.

## Resume Condition

The task is complete, independently reviewed, published, and parent-verified. There is no normal resume action. Any revision to the ten concepts, page behavior, imagery, or gameplay authority begins as a new discussion and task.

## Verification Handoff

Final verification passed with no remaining work.

Changed Web surfaces:

1. `app/data/runProgression.ts`: exactly ten structured concepts and twenty unique local image references.
2. `app/run-progression/`: standalone route metadata, image-first browser, quick comparison, responsive presentation, keyboard/touch navigation, and focus-safe image enlargement.
3. `app/components/AtlasApp.tsx` plus scoped shared navigation styling: one visible link to `/run-progression`; catalog and persistence models are unchanged.
4. `public/run-progression/`: the twenty moved accepted PNG assets; only the selected concept's two images are mounted.
5. `tests/run-progression.test.mjs`: record/image cardinality, asset existence, server-rendered route shell, selected-image mounting, navigation, dialog, and atlas-entry checks.

Execution evidence:

- `npm run build`: passed; vinext reports both `/` and `/run-progression`.
- `npm test`: passed, 20/20 tests with 0 failures.
- `npm run lint`: passed with no errors or warnings.
- Final asset audit: 20 PNGs, 20 unique filenames, 0 empty files, 20 unique data references, 0 missing references, total 47.54 MiB.
- Superseded package audit: `docs/design-explorations/run-progression-mechanisms/` no longer exists; no duplicate image package remains.

Publication evidence:

- Live URL: `https://tower-mechanics-atlas.lingqi0131.chatgpt.site/run-progression`
- Existing project remained active and public; no access update was issued.
- Source branch: `main`; pushed commit: `255200c73e66f0d867efeed30cd8331d2846abb1`.
- Saved Sites version: 7; production deployment status: `succeeded`.
- Anonymous reachability: `/run-progression` 200 with expected page markers; `/` 200 with expected atlas markers.
- Parent production verification: title, all ten frameworks, territory concept, route entry in the production AtlasApp bundle, and first/last PNG responses all passed; both image endpoints returned 200 `image/png`.
- Temporary publication repository and build archive: removed after successful verification.
- Root repository staging area: empty after publication and final verification.

Remaining work: none.
