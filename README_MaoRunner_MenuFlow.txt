MaoRunner — меню и навигация сцен
=================================

Что добавлено
-------------
1) SceneNames.cs — единый список имён сцен.
2) SceneLoader.cs — загрузка сцен (MainMenu, CharacterMenu, Runner, Player) и выход.
3) MainMenuUI.cs — кнопка «Меню персонажа» и «Выход».
4) CharacterMenuUI.cs — «Играть», «Назад», «Player Scene» (опц.).
5) ProgressPanel.cs — панель прогресса (уровень, XP, монеты).
6) MaoRunnerSceneTools.cs (Editor) — создаёт пустые сцены и добавляет их в Build Settings.

Быстрый старт
-------------
1. Скопируй файлы в проект.
2. В Unity: меню **MaoRunner → Scenes → Ensure Default Scenes & Build Settings** — создаст сцены
   `Assets/Scenes/MainMenu.unity`, `CharacterMenu.unity`, `Runner.unity`, `Player.unity` и добавит их в Build Settings.
3. В **MainMenu**:
   - Создай Canvas с двумя кнопками: «Меню персонажа» и «Выход».
   - Добавь `SceneLoader` на пустой объект (или он сам создастся кодом).
   - На Canvas добавь `MainMenuUI` и привяжи ссылки на кнопки.
4. В **CharacterMenu**:
   - Canvas с кнопками: «Играть», «Назад», «Player Scene» (опционально).
   - Добавь `CharacterMenuUI` и привяжи ссылки на кнопки.
   - При желании добавь панель прогресса: `ProgressPanel` + привяжи Text-поля.
5. В **Runner** — твоя игровая сцена (укажи её имя в Build Settings как `Runner` или отредактируй `SceneNames`).

Горячие заметки
---------------
- Все имена сцен централизованы в `SceneNames.cs` — меняй их там, а не строками в коде.
- В продакшене стоит добавить проверку существования сцен и graceful fallback.
- Для UI можешь использовать TextMeshPro — достаточно заменить `UnityEngine.UI.Text` на TMP_Text.
