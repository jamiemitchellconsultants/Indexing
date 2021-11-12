using IBIndex;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;

namespace BIndexGrains;
[StorageProvider(ProviderName = "indexStore")]
public class BPTreeGrain<TKey,TValue>:Grain,IBPTree<TKey,TValue> where TKey:IComparable
{
    private readonly ILogger<BPTreeGrain<TKey, TValue>> _logger;
    private readonly IPersistentState<BPTreeState<TKey,TValue>> _store;

    public BPTreeGrain([PersistentState("bpTree", "indexStore")] IPersistentState<BPTreeState<TKey,TValue>> store,
        ILogger<BPTreeGrain<TKey, TValue>> logger)
    {
        _store = store;
        _logger = logger;
    }
    public async Task InitializeTree(int order)
    {
        if (_store.State.Initialized)
        {
            throw new InvalidCastException("Already Initialized");
        }
        _store.State.Order = order;
        _store.State.Initialized = true;
        await _store.WriteStateAsync();

    }

    public async Task<TValue?> Get(TKey key)
    {
        if (!_store.State.Initialized)
        {
            throw new InvalidOperationException("Not initialized");
        }

        if (_store.State.RootNode != null)
            return await _store.State.RootNode.Item(key);
#pragma warning disable CS8602
        return await _store.State.RootLeaf?.Item(key);
#pragma warning restore CS8602

    }

    public async Task Add(IItem<TKey, TValue> item)
    {
        if (!_store.State.Initialized)
        {
            throw new InvalidOperationException("Not initialized");
        }
        
        throw new NotImplementedException();
    }

    public async Task<string> ToJson()
    {
        throw new NotImplementedException();
    }
}

[Serializable]
public class BPTreeState<TKey,TValue> where TKey:IComparable
{
    public bool Initialized { get; set; }
    public int Order { get; set; }
    
    public IBNode<TKey,TValue>? RootNode { get; set; }
    public IBLeaf<TKey,TValue>? RootLeaf { get; set; }
}