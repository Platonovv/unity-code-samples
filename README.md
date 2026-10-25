# Unity Code Samples

Избранный код шести моих игр на Unity: мобильные Spin Hero и его рескин (Google Play, App Store) и четыре WebGL-игры 2026 года. В основном алгоритмы (генераторы уровней, решатели, поиск пути) и архитектура (ECS, DI, покупки, миграции сохранений).

Файлы взяты из рабочих проектов. Для публикации из них убраны отладочные части и исправлены ошибки, найденные при отборе. Вне своих проектов файлы не компилируются, потому что ссылаются на соседние аспекты, настройки и вьюхи. Исключение — головоломка «Зелья Бабы-Яги»: она на чистом C# и покрыта тестами.

Стек: Leopotam.EcsProto (ECS), VContainer (DI), UniTask, ScriptableObject-настройки, Newtonsoft.Json.

## С чего начать

1. [YagaPotions/Gameplay/Puzzle/PuzzleSolver.cs](YagaPotions/Gameplay/Puzzle/PuzzleSolver.cs) и [LevelGenerator.cs](YagaPotions/Gameplay/Puzzle/LevelGenerator.cs) — генератор уровней по сиду, который проверяет каждый уровень решателем. Покрыт тестами.
2. [UnderworldSurvivors/Gameplay/Enemies/EnemyFlowField.cs](UnderworldSurvivors/Gameplay/Enemies/EnemyFlowField.cs) — одно поле потока на всех врагов вместо поиска пути для каждого. Работает без аллокаций в кадре.
3. [PixelPop/Gameplay/Flow/LevelGenerator.cs](PixelPop/Gameplay/Flow/LevelGenerator.cs) — уровень строится обратной игрой по картинке, сложность подбирается ботом.

## Тесты

```
cd Tests/YagaPotions.Puzzle.Tests
dotnet test
```

144 теста на xUnit (.NET 8): правила переливания и отмена хода, решатель, а также 40 сгенерированных уровней. Каждый уровень решается, план решателя доводит доску до конца, цвета разложены честно, генерация детерминирована по сиду. Тесты компилируют исходники головоломки напрямую, без Unity, и запускаются в GitHub Actions на каждый пуш.

## «Зелья Бабы-Яги» — головоломка-сортировка, WebGL (`YagaPotions/`)

| Файл | Что показывает |
|---|---|
| [PuzzleSolver.cs](YagaPotions/Gameplay/Puzzle/PuzzleSolver.cs) | Поиск в глубину с отсечением повторных состояний, лимитом узлов и глубины. Ходы в пустую колбу отсекаются по симметрии. Списки ходов переиспользуются по уровням глубины, поэтому цикл не создаёт мусора |
| [LevelGenerator.cs](YagaPotions/Gameplay/Puzzle/LevelGenerator.cs) | Перемешивание по сиду и проверка решаемости решателем. Если не вышло — новый сид, затем запасная пустая колба |
| [PuzzleBoard.cs](YagaPotions/Gameplay/Puzzle/PuzzleBoard.cs), [Flask.cs](YagaPotions/Gameplay/Puzzle/Flask.cs) | Модель доски: переливание, скрытые слои, отмена через стек записей. Хеш состояния не зависит от порядка колб (SplitMix-хеш каждой колбы) |
| [SeededRandom.cs](YagaPotions/Gameplay/Puzzle/SeededRandom.cs) | Xorshift, чтобы уровни воспроизводились на всех платформах одинаково |

## «Нечисть» — survivors-like с хабом и прогрессией, WebGL (`UnderworldSurvivors/`)

| Файл | Что показывает |
|---|---|
| [EnemyFlowField.cs](UnderworldSurvivors/Gameplay/Enemies/EnemyFlowField.cs) | Поле потока алгоритмом Дейкстры на кольцевых корзинах (Dial). Пересчёт только при сдвиге цели на 2 клетки. Сбрасываются только тронутые клетки. Если цель в прямой видимости, враг идёт к ней напрямую |
| [EnemySpaceHash.cs](UnderworldSurvivors/Gameplay/Enemies/EnemySpaceHash.cs) | Пространственный хеш: соседи и ближайший враг без аллокаций — сотни врагов на слабом телефоне |
| [EnemySpawnSystem.cs](UnderworldSurvivors/Gameplay/Enemies/EnemySpawnSystem.cs), [WaveDirector.cs](UnderworldSurvivors/Gameplay/Enemies/WaveDirector.cs) | ECS-спавн: волны по таймлайну, плавный рост числа врагов, бюджет спавна на кадр, элиты и босс, отзыв лишних врагов |
| [HubPathfinder.cs](UnderworldSurvivors/Gameplay/Hub/HubPathfinder.cs) | Поиск в ширину по 8 направлениям без срезания углов и выпрямление пути по видимости. Работает на заранее выделенных массивах |
| [GearGenerator.cs](UnderworldSurvivors/Services/Gear/GearGenerator.cs) | Снаряжение: взвешенная редкость, гарантия редкой вещи (pity), основные и случайные доп. статы. Случайность за [IRandom](UnderworldSurvivors/Core/IRandom.cs) |
| [PurchaseService.cs](UnderworldSurvivors/Services/Purchases/PurchaseService.cs) | Покупки за [IPurchaseProvider](UnderworldSurvivors/Services/Purchases/IPurchaseProvider.cs): защита от двойной покупки, таймаут ответа площадки с поздней выдачей, идемпотентная выдача неподтверждённых покупок после перезапуска |
| [ProjectLifetimeScope.cs](UnderworldSurvivors/Bootstrap/ProjectLifetimeScope.cs) | Корень композиции VContainer: сервисы, стейт-машина игры, выбор реализаций под площадку (Яндекс Игры, VK, GamePush, локальная в редакторе) |

## Pixel Pop — пиксельная головоломка, WebGL (`PixelPop/`)

| Файл | Что показывает |
|---|---|
| [LevelGenerator.cs](PixelPop/Gameplay/Flow/LevelGenerator.cs) | Уровень из картинки обратной игрой: стрелки снимают открытые пиксели, лёд и большие пиксели 2×2 требуют нескольких попаданий. Сложность калибруется: бот проверяет решаемость, «небрежные» боты дают долю случайных побед, и запас зарядов подстраивается под целевой коридор |
| [FlowBot.cs](PixelPop/Gameplay/Flow/FlowBot.cs) | Жадный бот-решатель: оценка цветов по открытым пикселям за вычетом уже летящих зарядов. Промахи с заданной вероятностью по сиду, детектор зависания |
| [ExposureMap.cs](PixelPop/Gameplay/Flow/ExposureMap.cs) | Открытые пиксели лучами с четырёх сторон; большой пиксель учитывается один раз |

## «Полки: собери три» — сортировка тройками, WebGL (`ShelfSort/`)

| Файл | Что показывает |
|---|---|
| [LevelGenerator.cs](ShelfSort/Gameplay/Shelves/LevelGenerator.cs) | Генерация обратной игрой из решённого состояния: «разматчивание» троек и обратные ходы. Поэтому уровень решаем по построению, а записанные ходы в обратном порядке и есть решение для подсказок |

## Spin Hero — мобильная игра, Google Play и App Store (`SpinHero/`)

| Файл | Что показывает |
|---|---|
| [SaveMigrationRunner.cs](SpinHero/Services/SaveLoad/SaveMigrationRunner.cs) | Миграции сохранения v1→v7 по шагам на JObject: переименование ключей по всему дереву, слияние и перенос структур. Если шаг упал, сейв остаётся нетронутым |

## Рескин Spin Hero (`SpinHeroReskin/`)

| Файл | Что показывает |
|---|---|
| [ForestBoneMotion.cs](SpinHeroReskin/Gameplay/ForestCharacters/ForestBoneMotion.cs) | Процедурная вторичная анимация 2D-скелета: пружины костей с подшагами, инерция (follow-through) с жёсткостью по глубине кости, покачивание от скорости, плавный переход между клипами |
