using Cysharp.Threading.Tasks;
using UnderworldSurvivors.Core.Save;

namespace UnderworldSurvivors.Services.Save
{
    public interface ISaveService
    {
        PlayerSave Data { get; }

        UniTask LoadAsync();
        UniTask SaveAsync();
        void ResetForTesting();
    }
}
