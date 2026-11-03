# TEST Transport Audio Demo

Open `Assets/TransportAudioDemo/TEST.unity` and press Play.

- `1-8` selects HL-120, HL-62, Leopard 1, T-55, Tiger, T1, Scout Mix, or Assault Mix.
- `W/S` or the arrow keys control throttle and reverse.
- `A/D` or the arrow keys steer the selected primitive vehicle.
- Each vehicle uses a separate `TransportAudioProfile` from `Assets/TransportAudioDemo/Audio/EngineProfiles`.
- The generated arena footprint is four times larger than the original demo arena, and the seventh/eighth display vehicles use mixed profiles built from existing engine recordings.
- This copy uses Unity's built-in legacy input API so it works with the destination project's current input settings; no Input System package is required.

The engine mixer keeps idle, run, and high RPM loops alive and crossfades their volume from the current engine demand. Pitch follows demand with smoothing; start, acceleration, and available stop recordings are one-shot events with cooldowns, so clips are not restarted every frame or duplicated during braking. Very short recordings are kept as one-shot events and are never used as continuous loops; Tiger's short high recording is therefore used only for acceleration character while the long run loop carries the sustained sound.
