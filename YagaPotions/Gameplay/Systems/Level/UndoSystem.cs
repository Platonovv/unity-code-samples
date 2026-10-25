using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using YagaPotions.Services.Analytics;
using YagaPotions.Services.Presenters;

namespace YagaPotions.Gameplay
{
    sealed class UndoSystem : IProtoRunSystem
    {
        [DI] private GameAspect _aspect;
        [DI] private LevelPresenter _presenter;
        [DI] private AnalyticsService _analytics;

        public void Run()
        {
            foreach (var entity in _aspect.UndoRequestIt)
            {
                _aspect.UndoRequest.Del(entity);
                Undo();
            }
        }

        private void Undo()
        {
            if (_presenter.IsCompleted || !_presenter.Board.Undo())
                return;

            _analytics.Undo();
            _aspect.GetProgress().IsOnPlan = false;
            _presenter.SelectedFlask = LevelPresenter.NoFlask;
            _presenter.HasHint = false;
            _aspect.LevelChangedMessage.NewEntity();
        }
    }
}
