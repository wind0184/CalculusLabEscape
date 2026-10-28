# Phase 38 — Calibration instructions and module navigation

The saved Power Core introduction now explains the real expanded-curriculum sequence: test the repair, use Next calibration when shown, complete all checks, then Install regulator. The number-input introduction follows the same sequence. Saved instructions and generation defaults match; the conditional wording also makes sense for older curricula without independent checks.

Integration's Finish module button now returns directly to the containment-case overview. Next calibration stays in the current module and opens the next unfinished question. The action is guarded against unfinished answers and support overlays. It does not add an assessed attempt, change accepted work, consume a heart, open the master cover or deactivate the device. Completed modules can still be opened from the overview for review.

Implementation: `MissionOnboarding.cs`, `MissionIntroductions.asset` and `CaseCalibrationCanvas.cs`. Existing playtests now assert the actual navigation destination and no longer simulate the redundant back click. These changes resolve the remaining instruction and navigation findings from the 2026-10-18 AI playtest; Phase 37 resolved the shared stale-feedback finding.

## Verification

Evidence: `../../qa/calibration-navigation-2026-10-18`.

- Both development and release Windows builds succeeded.
- Full Differentiation → Integration player test passed at 1280×720, with 101 actual scene/Canvas screenshots and no errors.
- Both revised introductions rendered without overflow and matched their saved/default text.
- Seven navigation checks passed: three Next calibration transitions stay in the current module, and all four Finish module transitions go directly to the overview. The first dial check also retains its existing partial save/resume test.
- The same run completed both 18-step missions, rescue, all four integration modules, guarded Master Deactivation, retries, help, save/resume, local attempt records and focused practice. It also retained the Phase 37 feedback-clearing checks.
- Of 16 audited authored scenes, UI prefabs and production data assets, only `MissionIntroductions.asset` changed. Within that asset, only the Power Core and number-input instructions changed; the other eight cards are unchanged. Question banks, grades, curriculum versions and authored geometry are unchanged.

Key images: `missions-720/introduction-bridge-power.png`, `missions-720/introduction-bridge-numbers.png` and `missions-720/finish-module-0-overview.png` through `finish-module-3-overview.png`.
