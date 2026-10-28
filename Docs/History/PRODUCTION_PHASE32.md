# Phase 32 — Limits and Integration main mission questions

Following the Differentiation expansion, this phase adds required independent questions to the other two missions. These questions were authored within the existing Chapter 4 learning scope; no additional Limits or Integration textbook was supplied for this phase.

| Mission | New-run content | Assessment steps | World progress |
| --- | --- | --- | --- |
| Limits | Five guided questions plus five independent checks | 10 | Five portal calibrations |
| Differentiation | Existing expanded curriculum, approximately twelve complete expression groups | 18 | Three installed bridge components |
| Integration | Four guided module problems plus eight independent questions | 18 | Four completed modules, then separate Master Deactivation |

## Limits

Each original calibration now has one independent check. Completing the guided answer opens the independent check; completing and applying both restores 20% portal charge. Added content covers a polynomial limit, an open point with a different assigned value, agreeing piecewise sides, distinguishing a limit from the function value, and unequal sides with no two-sided limit.

Original tables, graphs and piecewise displays remain on their guided questions. Extra questions show their own given expression or stated values instead of reusing an unrelated graph. Three independent items use a single choice without the original three-card reasoning control; the other two use number input. Powers render as superscripts. Hints use each independent question's ID; retries and free basic support preserve earned work.

`LimitsIndependentQuestions.asset` is a second saved `LimitsQuestionBank`. The original five guided questions remain in `LimitsQuestions.asset`. A new `Choice` enum value is appended without renumbering existing answer kinds.

## Integration

Each module requires its original interactive task and two independent questions:

- Exponent Dial: integrate `6x^2` and `-8x^3`, including coefficients, signs and +C.
- Wires: integrate `2x + 3` and `3x^2 - 4x + 5` term by term, using one common +C.
- Sequence: integrate a cube root and `2/sqrt(x)` after rewriting as fractional powers; domains are restricted to `x > 0`.
- Pressure: evaluate integrals of `2x` from 1 to 3 and `3x^2` from 0 to 2, using upper minus lower values and omitting +C.

The original controls remain locked after acceptance, and the saved native calibration panel presents the follow-up questions with specific incorrect-choice feedback. A module becomes STABLE and its lamp turns green only after all its required work is accepted. Completing only the original dial does not unlock the other modules. All four complete modules are required to lift the master cover; deactivating the device remains a separate final action.

`CaseModuleComplete` supplies the same completion rule to UI, module count, physical lamps and master readiness. The physical dial/card/wire/gauge poses still show the accepted original tasks. Case calibration help pauses timers and audio through the existing support system. New records and skill summaries include all eight added answers.

## Save compatibility and authoring

New local runs use `limitsCurriculumVersion = 1` or `integrationCurriculumVersion = 1`. Older JSON records default to version 0 and keep five Limits or ten Integration steps, with historical scores unchanged. Missing independent work prevents a restored Limits session from skipping its calibration. Partially complete Integration calibration restores the remaining question without marking the module complete or replaying success sounds.

New question assets are only created when missing. Editor menus under **Calculus Lab → Questions** open both banks without Play. Integration's `CaseCalibration` panel and Limits' `IndependentGiven` text are saved in their respective UI prefabs. The mathematical text component is attached to the new question/choice fields, so powers and square roots render through the same formatter used in Differentiation. Numeric input remains plain text.

Implementation: `IntegrationQuestionBank.cs`, `CaseCalibrationCanvas.cs`, `OtherTopicBankBuilder.cs`, with targeted changes to the existing game, Limits presenter, module feedback and skill summaries. No scenes are regenerated.

## Verification

Evidence is under `qa/other-topic-banks`.

- `mathematics.txt`: exact rational differentiation of all six indefinite-integral answers, three numerical derivative samples per expression, both definite integrals and the five added limit conclusions checked independently.
- `delivery-build.log`: repeated migration, old curriculum versions, a partially restored Limits calibration, a partially restored Exponent Dial and correct unlocking after both dial questions all PASS. Development and release builds both succeed.
- `missions-final-720` and `missions-final-1080`: both actual Bridge-to-Integration flows PASS, 94 screenshots each. This includes all eight new calibration answers, a specific missing-C retry, blocked premature completion, correct module lamps, saved partial calibration, silent restore, mathematical notation, master shutdown, results and practice records.
- `limits-final-720` and `limits-final-1080`: both ten-question flows PASS, seven screenshots each. Real walking, the five original interactions, independent questions, free correctly attributed support, wrong-answer retries, blocked skipping/duplicate scoring, mid-calibration restore, portal charge and final activation are checked.
- Final serialized records are checked separately: Limits 10/10 accepted with 12 attempts; Differentiation 18/18 with 23 attempts; Integration 18/18 with 23 attempts. The intentional incorrect attempts verify retry and review behaviour, not learner performance claims.
- No runtime exceptions or shader errors in the four final runs. New powers and square roots are visually inspected and checked through actual TMP output. Main-menu Limits wording is updated to ten questions; the final caption-only build retains byte-identical tested development/release gameplay assemblies.
- All three authored scene files remain byte-identical to the pre-change scenes. Changes are saved in question assets, UI prefabs and scripts.

Delivery: `Builds/Chapter4/CalculusLabEscape_Chapter4.exe` and `Builds/LearningLab/CalculusLabEscape.exe`. Source backup: `unity/backups/2026-10-15-other-topic-banks/CalculusLabEscape-source.zip`, excluding generated Library/Builds and the validation copy.
