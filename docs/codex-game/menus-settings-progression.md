# Menus Settings And Progression UI

Read this file before changing main menu, settings screens, development tree, vehicle selection UI, profile display, or menu state transitions.

Current owner scripts:
- `Assets/Game/Scripts/MenuController/MenuManager.cs`
  - Global menu state and menu open/close events.
- `Assets/Game/Scripts/MenuController/MenuType.cs`
  - Menu enum used by menu routing and input blocking.
- `Assets/Game/Scripts/UI/MainMenu/MainMenu.cs`
  - Main menu root, settings/development tree buttons, profile display values.
  - Calls `ProfileServer.UpdateProfile()` when main menu opens.
- `Assets/Game/Scripts/UI/MainMenu/VehicleItemView.cs`
  - Vehicle list item view.
- `Assets/Game/Scripts/UI/MainMenu/VehicleSlotView.cs`
  - Vehicle slot display.
- `Assets/Game/Scripts/UI/MainMenu/RobotView.cs`
  - Main menu robot preview display.
  - Initializes preview vehicles through `VehicleRoot.Init(true)`. Menu context keeps the robot model visible but suppresses its gameplay-only world nickname and HP bar.
- `Assets/Game/Scripts/UI/MainMenu/CameraOrbit.cs`
  - Menu camera orbit.
- `Assets/Game/Scripts/UI/Tree/DevelopmentTree.cs`
  - Development tree root/init.
- `Assets/Game/Scripts/UI/Tree/TreeGrid.cs`, `TreeItem.cs`, `ArrowDrawer.cs`
  - Development tree visual layout.
- `Assets/Game/Scripts/UI/Tree/VehicleResearchProgressResolver.cs`
  - Vehicle research/progression data support.
- `Assets/Game/Scripts/UI/Settings/*.cs`
  - Settings tabs and settings model/view/controller.
- `Assets/Game/Scripts/Client/ClientSettings.cs`
  - Local client runtime settings for frame pacing, projectile visuals, HUD, map, camera, reticle, auto-aim, and hover outline behavior.
- `Assets/Editor/ClientSettingsEditor.cs`
  - Custom Unity inspector for `ClientSettings`.
  - Keeps technical serialized field names visible and shows a Ukrainian explanation with a practical example as a tooltip when hovering every root and nested setting.
- `Assets/Editor/DocumentedSettingsInspector.cs`
  - Shared editor-only renderer used by settings inspectors.
  - Draws nested serialized settings recursively and provides tooltip documentation without adding description rows below fields.
- `Assets/Game/Scripts/UI/Loading/*.cs`
  - Loading screen and shared spinner.
- `Assets/Game/Scripts/UI/Screens/*.cs`
  - Popup/screen base infrastructure.

Important behavior:
- `MainMenu.SetActive(true)` opens `MenuType.MainMenu`; false opens `MenuType.GameplayHUD`.
- Settings and development tree buttons use `MenuManager.OpenMenu`.
- Gameplay input blocking is tied to menu state in `VehicleInputController.IsGameplayInputBlockedByUi`.

Rules when editing menus:
- Keep menu state changes consistent with gameplay input blocking.
- Do not let menu-only code affect server-authoritative gameplay.
- If profile fields or vehicle selection changes, update `api-resources.md` too.
- If gameplay HUD behavior changes, update `ui-hud.md`.
- Add a Ukrainian explanation with a practical example to `Assets/Editor/ClientSettingsEditor.cs` whenever `ClientSettings` gains a serialized field, including nested settings.
