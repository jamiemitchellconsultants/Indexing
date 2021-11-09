using IBIndex;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace BIndexGrains;

public class BLeafGrain<TKey,TValue>:IBLeaf<TKey,TValue> where TKey : IComparable
{
    private readonly ILogger<BLeafGrain<TKey, TValue>> _logger;
    private readonly IPersistentState<BLeafState<TKey, TValue>> _store;

    public BLeafGrain([PersistentState("bleaf", "indexStore")] IPersistentState<BLeafState<TKey, TValue>> store,
        ILogger<BLeafGrain<TKey, TValue>> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<List<IItem<TKey, TValue>>> Contents()
    {
        return await Task.FromResult( _store.State.Items);
    }

    public Task<IBNode<TKey, TValue>> Add(IItem<TKey, TValue> item)
    {
        throw new NotImplementedException();
    }

    public Task<TValue> Item(TKey key)
    {
        throw new NotImplementedException();
    }

    public Task<string> ToJson()
    {
        throw new NotImplementedException();
    }
}

[Serializable]
public class BLeafState<TKey, TValue> where TKey : IComparable
{
    public int Order { get; set; }
    public List<IItem<TKey, TValue>> Items { get; set; } = new();
    public IBLeaf<TKey,TValue>? NextLeaf { get; set; }
    public IBNode<TKey,TValue>? Parent { get; set; }
}