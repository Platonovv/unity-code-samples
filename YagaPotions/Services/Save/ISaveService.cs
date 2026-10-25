using Cysharp.Threading.Tasks;
using YagaPotions.GameData;

namespace YagaPotions.Services.Save
{
    public interface ISaveService
    {
        PlayerSave Data { get; }

        UniTask LoadAsync();
        UniTask SaveAsync();
        void ResetForTesting();
    }
}
