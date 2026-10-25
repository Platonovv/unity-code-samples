using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace UnderworldSurvivors.Services.Purchases
{
    public interface IPurchaseProvider
    {
        UniTask<IReadOnlyList<string>> GetPurchasedAsync();
        UniTask<IReadOnlyList<ProductInfo>> GetCatalogAsync();
        UniTask<bool> PurchaseAsync(string productId);
        UniTask<bool> ConsumeAsync(string productId);
    }
}
