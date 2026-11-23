# Battle countdown

## Ownership

- `BattleCountdownController` owns the client countdown lifecycle and starts it from the scene-loading-hidden event.
- `BattleCountdownView` owns only the animated number presentation.
- `BattleCountdownGate` owns movement/firing lock state. It keeps a local client state and per-room server states.
- `GameplaySpawner` opens the server room lock before robot spawn; `BattleCountdownAuthority` owns the network-side completion acknowledgement and releases it after every connected real player reports the completed countdown.
- Robot input, movement, and weapon classes only consume the gate; turret aim remains in `VehicleInputController` and is not gated by the countdown. While the server gate is active, `VehicleMovementController` keeps a gravity-only `CharacterController.Move` pass so a robot can land; horizontal movement and hull rotation remain suppressed.
- `GunCrosshair` and `SniperScopeOverlay` own their own reticle presentation response to the client countdown state: they hide immediately and reveal over two unscaled seconds after the gate ends. `GunCrosshair` includes the standalone `Back` and `Back2` opacity groups in this transition while preserving the configured reticle opacity. Countdown logic does not manipulate reticle geometry or aiming state.

## Timing

The loading screen is hidden first, then the client gate waits `0.5` seconds before displaying `10`. Each number from `10` through `1` is displayed for one second and plays `UI_Battle_Countdown` once. The gate releases after the `1` tick finishes. The `Client` scene serializes `countdownSeconds` as `10`.

The server initializes the synchronized match timer to its full configured duration, then holds it there while the room countdown gate is active. It begins decrementing only after `BattleCountdownAuthority` receives completion from every connected real player and releases the room gate; time spent in the pre-battle countdown does not consume match time.
