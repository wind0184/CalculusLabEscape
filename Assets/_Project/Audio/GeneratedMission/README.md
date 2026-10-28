# Generated mission sound effects

These two sounds were generated through ElevenLabs and supplied by the project owner for this game. WAV derivatives are imported into the saved mission scenes; the original Downloads files are unchanged.

- **WoodBridgeInstall.wav**: wooden bridge version 2, original filename `A_wooden_bridge_sect_#2-1793104669632.mp3`. Excerpt 3.3–4.5 seconds, short edge fades, gain -2.14 dB, stereo PCM at 48 kHz / 16 bit. About -18 LUFS. Begins 0.36 seconds into an actual bridge installation so its first main impact aligns with the raised deck settling into place. No sound is triggered by a save restore.
- **LaboratoryShutdown.wav**: continuous shutdown version 1, original filename `A_single_continuous__#1-1793106438591.mp3`. Full 2 seconds, short edge fades, gain -5 dB, stereo PCM at 48 kHz / 16 bit. About -18 LUFS. Begins at 24% of the existing shutdown animation, after the lever moves and before the power bars turn off.

`MissionAudio` exposes both clip references and their playback gain in the Inspector. The saved AudioSource does not play on awake, does not loop and obeys the master listener volume. Playback pauses with mission pause or support panels. Other existing cues retain their original sounds.
