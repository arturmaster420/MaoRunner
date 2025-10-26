MaoRunner UI Core v1 (for Unity 2022.2.7f1, URP)

This package contains an Editor Setup Wizard that BUILDS three scenes for you:
 - MainMenuScene.unity
 - PlayerScene.unity
 - PlayScene.unity (lightweight, leaves your existing gameplay intact)

How to use:
1) Unzip this folder anywhere outside the Unity Assets.
2) Drag the folder 'Assets_add' into your project's Assets.
3) Let Unity compile. Make sure TextMeshPro is imported (Window → TextMeshPro → Import TMP Essential Resources).
4) In Unity top menu: VRSchoolTan → Build Core Scenes.
5) In the popup, assign:
   - Mao Player Prefab (your animated player)  [optional]
   - Floor Segment Prefab (your FloorPrefab)   [optional]
6) Click 'Create/Update Scenes'. The wizard will generate the three scenes under Assets/Scenes/.
7) Set Build Settings → Scenes In Build: add MainMenuScene, PlayerScene, PlayScene in that order.
8) Open MainMenuScene and press Play.

Notes:
- The wizard does NOT overwrite existing scenes unless you check 'Overwrite existing'.
- All UI is created programmatically and remains fully editable in the hierarchy.
- You can tweak colors, fonts, anchors after generation; re-run wizard anytime.

Included scripts (Runtime):
- SceneLoader.cs — centralized scene changes.
- MainMenuController.cs — Start / Quit buttons.
- PlayerSceneController.cs — shows XP bar, coins, rotates player, Start/Back.
- UIHudPlay.cs — small HUD for PlayScene (XP bar, Coins, Speed, Pause).

Included scripts (Editor):
- MaoRunnerSetupWizard.cs — builds all scenes: cameras, lighting, canvases, UI, event system.

Dependency:
- TextMeshPro (TMP) required for UI text.
- URP recommended.

If something goes wrong, delete generated scenes and re-run the wizard.
