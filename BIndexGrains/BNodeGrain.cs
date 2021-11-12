using IBIndex;
using Orleans;
using Orleans.Providers;

namespace BIndexGrains;
[StorageProvider(ProviderName = "indexStore")]
public class BNodeGrain<TKey, TValue>:Grain,IBNode<TKey,TValue> where TKey:IComparable
{
    public async Task<IBNode<TKey, TValue>?> Add(IItem<TKey, TValue> item)
    {
        throw new NotImplementedException();
    }

    public async Task<TValue?> Item(TKey key)
    {
        throw new NotImplementedException();
    }

    public async Task<string> ToJson()
    {
        throw new NotImplementedException();
    }

    public async Task Initialize(int stateOrder, IBNode<TKey, TValue>? stateParent, SortedDictionary<TKey, IItem<TKey, TValue>> highContent)
    {
        throw new NotImplementedException();
    }
}