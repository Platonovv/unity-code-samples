using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using YagaPotions.Gameplay.Puzzle;
using YagaPotions.Gameplay.Settings;
using YagaPotions.Services.Analytics;
using YagaPotions.Services.DailyQuests;
using YagaPotions.Services.Presenters;

namespace YagaPotions.Gameplay
{
    sealed class PourSystem : IProtoRunSystem
    {
        [DI] private GameAspect _aspect;
        [DI] private LevelPresenter _presenter;
        [DI] private AnalyticsService _analytics;
        [DI] private DailyQuestService _quests;

        public void Run()
        {
            foreach (var entity in _aspect.PourRequestIt)
            {
                var move = _aspect.PourRequest.Get(entity).Move;
                _aspect.PourRequest.Del(entity);
                Pour(move);
            }
        }

        private void Pour(PourMove move)
        {
            var volume = _presenter.Board.Pour(move.From, move.To);
            if (volume > 0)
                ApplyPour(move, volume);

            _aspect.LevelChangedMessage.NewEntity();
        }

        private void ApplyPour(PourMove move, int volume)
        {
            _analytics.Pour();
            _quests.Count(QuestKind.Pours, 1);
            FollowPlan(move);
            _presenter.HasHint = false;
            if (_presenter.Board.IsSolved)
                _aspect.LevelSolvedMessage.NewEntity();

            ref var poured = ref _aspect.PouredMessage.NewEntity();
            poured.Move = move;
            poured.Volume = volume;
        }

        private void FollowPlan(PourMove move)
        {
            ref var progress = ref _aspect.GetProgress();
            if (!progress.IsOnPlan)
                return;

            var next = progress.Plan[progress.PlanStep];
            progress.IsOnPlan = next.From == move.From && next.To == move.To
                && ++progress.PlanStep < progress.Plan.Count;
        }
    }
}
