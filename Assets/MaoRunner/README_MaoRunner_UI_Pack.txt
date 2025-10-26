MaoRunner UI Pack (MainMenuScene + PlayerScene)
===============================================

What you get
------------
- Editor window (Tools/MaoRunner/Build Core Scenes) that generates and wires:
  - MainMenuScene: Title + Start/Options/Exit (TextMeshPro UI).
  - PlayerScene: LVL + XP bar (bottom-left), Coins (top-right), RUN/SHOP/MENU buttons (right).
- Controllers that bind to your existing PlayerProgress (XP, Level, Coins).
- SceneLoaderPro to avoid conflicts with existing SceneLoader.
- Camera+Light auto-setup.
- PlayScene is NOT touched unless you explicitly check the box in the builder.

How to import
-------------
1) Ensure TextMeshPro is installed (Window -> Package Manager -> TextMeshPro).
2) Unzip contents so the top folder is /Assets/MaoRunner/...
3) Open the Editor tool: Tools -> MaoRunner -> Build Core Scenes.
4) Tick which scenes to (re)build (MainMenuScene, PlayerScene). Click Build.
5) Open PlayerScene and assign your Mao model Transform to PlayerSceneController.characterPreview
   to enable rotation preview (optional).

Navigation
----------
- MainMenu.Start -> loads PlayerScene
- PlayerScene.RUN -> loads PlayScene
- PlayerScene.MENU -> loads MainMenuScene

Tweaks
------
- Colors/anchors: Assets/MaoRunner/UI/ProgressUIPro.cs
- Layout values for generated scenes: Assets/MaoRunner/Editor/BuildCoreScenesWindow.cs
- Scene names: Assets/MaoRunner/Infrastructure/SceneLoaderPro.cs
