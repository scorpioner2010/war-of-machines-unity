# Battle countdown

## Ownership

- `BattleCountdownController` owns the client countdown lifecycle and starts it from the scene-loading-hidden event.
- `BattleCountdownView` owns only the animated number presentation.
- `BattleCountdownGate` owns movement/firing lock state. It keeps a local client state and per-room server states.
- `GameplaySpawner` opens the server room lock before robot spawn; `BattleCountdownAuthority` owns the network-side completion acknowledgement and releases it after every connected real player reports the completed countdown.
- Robot input, movement, and weapon classes only consume the gate; turret aim remains in `VehicleInputController` and is not gated by the countdown.

## Timing

The loading screen is hidden first, then the client gate waits `0.5` seconds before displaying `10`. Each number is displayed for one second and plays `UI_Battle_Countdown` once. The gate releases after the `1` tick finishes.
