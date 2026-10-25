using Cysharp.Threading.Tasks;
using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using YagaPotions.Gameplay.Puzzle;
using YagaPotions.Gameplay.Settings;
using YagaPotions.Services.Analytics;
using YagaPotions.Services.Assets;
using YagaPotions.Services.Presenters;
using YagaPotions.Services.Save;

namespace YagaPotions.Gameplay
{
    sealed class TutorialSystem : IProtoInitSystem, IProtoRunSystem
    {
        [DI] private GameAspect _aspect;
        [DI] private LevelPresenter _presenter;
        [DI] private ISaveService _save;
        [DI] private AnalyticsService _analytics;
        [DI] private AssetProvider _assetProvider;
        private TutorialSettings _settings;
        private PuzzleSolver _solver;

        public void Init(IProtoSystems systems)
        {
            _settings = _assetProvider.GetTutorialSettings();
            _solver = new PuzzleSolver(_assetProvider.GetPuzzleSettings().SolverNodeLimit);
        }

        public void Run()
        {
            if (_aspect.LevelChangedMessageIt.IsEmptySlow())
                return;

            _presenter.IsTutorial = IsTutorialLevel();
            if (_presenter.IsTutorial)
                Guide();
        }

        private bool IsTutorialLevel() =>
            _save.Data.IsTutorialDone == false && _presenter.Level == _settings.Level && _presenter.IsCompleted == false;

        private void Guide()
        {
            ref var progress = ref _aspect.GetProgress();
            CountGuidedMoves(ref progress);
            if (progress.TutorialStep >= _settings.GuidedMoves)
            {
                Complete();
                return;
            }

            if (!progress.IsOnPlan)
                BuildPlan(ref progress);

            _presenter.IsTutorial = progress.IsOnPlan;
            if (_presenter.IsTutorial)
                _presenter.TutorialMove = progress.Plan[progress.PlanStep];
        }

        private void CountGuidedMoves(ref LevelProgress progress)
        {
            foreach (var entity in _aspect.PouredMessageIt)
            {
                progress.TutorialStep++;
                _analytics.TutorialStep(_presenter.Level, progress.TutorialStep, _settings.GuidedMoves);
            }
        }

        private void BuildPlan(ref LevelProgress progress)
        {
            progress.PlanStep = 0;
            progress.IsOnPlan = _solver.TrySolve(_presenter.Board, progress.Plan) && progress.Plan.Count > 0;
        }

        private void Complete()
        {
            _presenter.IsTutorial = false;
            _save.Data.IsTutorialDone = true;
            _save.SaveAsync().Forget();
        }
    }
}
