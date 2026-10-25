# Unity Code Samples

Фрагменты кода двух моих WebGL-игр на Unity 6 (2026). Файлы взяты из рабочих проектов без изменений, поэтому сами по себе не компилируются: рядом нет аспектов, настроек и вьюх, на которые они ссылаются.

Стек: Leopotam.EcsProto (ECS), VContainer (DI), UniTask, ScriptableObject-настройки, сервисы платформы за интерфейсами.

## «Зелья Бабы-Яги» — головоломка-сортировка (`YagaPotions/`)

Уровни не хранятся, а генерируются по сиду; каждый уровень гарантированно решаем, а план решения служит обучению и подсказкам.

| Файл | Что показывает |
|---|---|
| [Gameplay/Puzzle/PuzzleSolver.cs](YagaPotions/Gameplay/Puzzle/PuzzleSolver.cs) | Поиск в глубину с отсечением повторов по ключу состояния, лимитом узлов и глубины; списки ходов переиспользуются по уровням глубины, без аллокаций в цикле |
| [Gameplay/Puzzle/LevelGenerator.cs](YagaPotions/Gameplay/Puzzle/LevelGenerator.cs) | Генерация по сиду с проверкой решаемости решателем, повтор с новым сидом, добавление пустой колбы как запасной вариант |
| [Gameplay/Puzzle/PuzzleBoard.cs](YagaPotions/Gameplay/Puzzle/PuzzleBoard.cs), [Flask.cs](YagaPotions/Gameplay/Puzzle/Flask.cs) | Модель доски: переливание, отмена через стек записей, клонирование, хеш состояния |
| [Gameplay/Puzzle/SeededRandom.cs](YagaPotions/Gameplay/Puzzle/SeededRandom.cs) | Детерминированный xorshift для воспроизводимых уровней |
| [Gameplay/Systems/Level/TutorialSystem.cs](YagaPotions/Gameplay/Systems/Level/TutorialSystem.cs) | ECS-система: обучение ведёт игрока по плану решателя, прогресс в сохранении |
| [Gameplay/Systems/Level/PourSystem.cs](YagaPotions/Gameplay/Systems/Level/PourSystem.cs), [UndoSystem.cs](YagaPotions/Gameplay/Systems/Level/UndoSystem.cs) | Обработка запросов хода и отмены, сообщения об изменении уровня |
| [Gameplay/LevelLifetimeScope.cs](YagaPotions/Gameplay/LevelLifetimeScope.cs) | Дочерний скоуп VContainer для уровня |
| [Services/Ads/AdsService.cs](YagaPotions/Services/Ads/AdsService.cs), [IAdsProvider.cs](YagaPotions/Services/Ads/IAdsProvider.cs) | Реклама за интерфейсом провайдера: защита от двойного показа, пауза игры, кулдаун межстраничной, аналитика |
| [Services/Platform/PlatformBridge.cs](YagaPotions/Services/Platform/PlatformBridge.cs) | Приём событий из JavaScript-SDK площадки |
| [Services/Save/](YagaPotions/Services/Save) | Сохранение за `ISaveService` поверх хранилища платформы |

## «Нечисть» — survivors-like с хабом и прогрессией (`UnderworldSurvivors/`)

| Файл | Что показывает |
|---|---|
| [Bootstrap/ProjectLifetimeScope.cs](UnderworldSurvivors/Bootstrap/ProjectLifetimeScope.cs) | Корень композиции: сервисы, стейт-машина игры, выбор реализаций под площадку (Яндекс Игры, VK, GamePush, локальная в редакторе) |
| [Gameplay/Players/PlayerMoveSystem.cs](UnderworldSurvivors/Gameplay/Players/PlayerMoveSystem.cs) | ECS-движение: рывок, отбрасывание, ограничение картой |
| [Gameplay/Enemies/EnemySpawnSystem.cs](UnderworldSurvivors/Gameplay/Enemies/EnemySpawnSystem.cs), [WaveDirector.cs](UnderworldSurvivors/Gameplay/Enemies/WaveDirector.cs) | Волны по таймлайну, бюджет спавна на кадр, элитные враги и босс, плавный рост числа врагов |
| [Gameplay/Enemies/EnemySpaceHash.cs](UnderworldSurvivors/Gameplay/Enemies/EnemySpaceHash.cs) | Пространственный хеш для поиска соседей без аллокаций — сотни врагов на слабом телефоне |
| [Services/Save/](UnderworldSurvivors/Services/Save), [Core/Save/PlayerSave.cs](UnderworldSurvivors/Core/Save/PlayerSave.cs) | Версионируемый DTO сохранения и миграции по версии |
| [Services/Platform/](UnderworldSurvivors/Services/Platform), [Services/Ads/](UnderworldSurvivors/Services/Ads) | Абстракция SDK площадки и рекламы, локальные реализации для редактора |
