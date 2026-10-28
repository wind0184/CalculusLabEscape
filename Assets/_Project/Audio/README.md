# Game audio

Open **GameAudio.mixer** in Unity's Audio Mixer window. Select the **All Sounds** view to see every fader. Adjust the volume in dB on a group, or mute that group while comparing sounds. Edit outside Play mode to keep your changes. No runtime script resets the mixer faders.

There is one shared mixer for Main Menu and all three missions. Every saved AudioSource, including briefing-video audio, outputs into this mixer. The existing in-game Master Volume slider still controls the final AudioListener volume.

```
Master
  UI
  Feedback
    Correct
    Retry
  Hint
  Robot
  Ambience
    River
  Mechanisms
    Portal
    Bridge
    Switch
    Pressure
    Shutdown
  Completion
  Briefing
```

Each fader is also exposed as `<GroupName>Volume`, e.g. `BridgeVolume`, `RetryVolume`, `MasterVolume`, for future settings controls. Values are in dB: 0 = authored level, -6 = approximately half amplitude, -80 = effectively silent. `Feedback` changes Correct and Retry together; `Mechanisms` changes all five physical effects together.

**MissionSounds.asset** holds the 12 clip references, their mixer groups and trim gains for library cues. The two supplied clips retain their existing `MissionAudio.importedClipVolume` trim. Use mixer faders to adjust any category. Scene **Game Audio** objects contain saved, separate playback sources for buttons/hints, answer feedback, robot greeting, river, mission effects and completion. The bridge/case has its existing saved physical AudioSource. UI objects and button events are authored before Play.

Retry feedback uses a quieter beep than correct feedback. Invalid-format entries and incomplete submissions do not play mathematical retry feedback. The robot greeting plays once when a conversation opens. River ambience loops during the bridge mission, reduces during questions/dialogue and pauses with gameplay. Completion plays once on the transition from the finishing animation to results. Restoring bridge progress does not replay installation sounds.

## Sources and processing

Library assets are edited excerpts of public HQ Freesound previews licensed CC0, plus the existing CC0 Kenney Interface Sounds. The three inaccessible Pixabay candidates (button, correct, hint) use the Kenney replacements below. No account or login was used for downloads.

| Asset | Source | License |
| --- | --- | --- |
| Click.wav | Kenney Interface Sounds / click_001.ogg; dry 0.04-second excerpt | CC0; original license in Assets/ThirdParty/Kenney/InterfaceSounds/License.txt |
| Correct.wav | Kenney Interface Sounds / confirmation_001.ogg | CC0 |
| Hint.wav | Kenney Interface Sounds / open_001.ogg | CC0 |
| Retry.wav | [Beep_Error_1.wav — JonnyRuss01](https://freesound.org/people/JonnyRuss01/sounds/478191/) | CC0 |
| Robot.wav | [Beeping Robot or Machine — Jofae](https://freesound.org/people/Jofae/sounds/363251/) | CC0 |
| River.wav | [Ambiance_River_Flow_Medium_Loop_Stereo.wav — Nox_Sound](https://freesound.org/people/Nox_Sound/sounds/479322/) | CC0 |
| Portal.wav | [portal gate opening — berkup](https://freesound.org/people/berkup/sounds/768664/) | CC0 |
| Switch.wav | [Toggle Switch — pillwave](https://freesound.org/people/pillwave/sounds/423957/) | CC0 |
| Pressure.wav | [Air release — davidlay1](https://freesound.org/people/davidlay1/sounds/416080/) | CC0 |
| Completion.wav | [Success Fanfare Trumpets.mp3 — FunWithSound](https://freesound.org/people/FunWithSound/sounds/456966/) | CC0 |
| GeneratedMission/WoodBridgeInstall.wav | User-supplied ElevenLabs wooden bridge #2, 1793104669632 | Supplied by user; retain existing GeneratedMission provenance |
| GeneratedMission/LaboratoryShutdown.wav | User-supplied ElevenLabs continuous shutdown #1, 1793106438591 | Supplied by user; retain existing GeneratedMission provenance |

Library WAVs are 48 kHz stereo PCM. Short cues have edge fades; levels were balanced with measured peak and short-window RMS limits. Portal uses a three-second active excerpt; pressure uses 0.9 seconds. River uses an overlap at the decoded MP3 seam. Original downloads and supplied files are unchanged. Processing measurements and source hashes are in `qa/all-mission-audio-2026-10-27/audio-processing.json`. The dry button replacement is recorded in `qa/ui-audio-overlap-fix-2026-10-27/click-processing.json` in the FYP workspace; it has an 80 Hz high-pass filter and short edge fades.

## Shared button prefabs

All 284 buttons in the four game scenes inherit `Assets/_Project/Prefabs/UI/Buttons/Button.prefab` or its `HintButton.prefab` variant. These contain the shared Image, Button and ButtonAudio components. Existing labels, icons, sizes, colours and gameplay events remain on their individual controls. Changing the inherited audio behaviour updates all instances. The Hint variant inherits the common button and selects the Hint cue.

Edit `LibraryMission/Click.wav` or its entry in `MissionSounds.asset` to change the sound for every ordinary button. Adjust **UI** in `GameAudio.mixer` to change its volume. Ordinary clicks use one playback voice: rapid clicks restart that short sound instead of stacking copies. Every button action still runs. Hint uses its separate saved AudioSource, PlayOneShot playback and the **Hint** fader. Clicks do not restart robot greetings, feedback or mission effects. Drag start and a matching drop also play a UI cue. Exit waits for its click to finish before closing the player.

The six Integration exponent, divisor and release +/- controls use this shared button sound. Their gameplay handlers do not also request a physical dial tick.
