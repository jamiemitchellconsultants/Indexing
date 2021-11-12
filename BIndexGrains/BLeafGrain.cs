using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IBIndex;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.CodeGeneration;
using Orleans.Providers;
using Orleans.Runtime;

namespace BIndexGrains;
[StorageProvider(ProviderName = "indexStore")]
public class BLeafGrain<TKey,TValue>:Grain,IBLeaf<TKey,TValue> where TKey : IComparable
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
        return await Task.FromResult( _store.State.Items.Values.ToList());
    }

    public async Task<IBLeaf<TKey, TValue>?> Add(IItem<TKey, TValue> item)
    {
        if (!_store.State.Initialized)
        {
            throw new InvalidOperationException("Not initialized");
        }
        _store.State.Items.Add(item.Key, item);

        if (_store.State.Items.Count <= _store.State.Order)
        {
            await _store.WriteStateAsync();
            return default;
        }

        var indexStart = _store.State.Items.Count / 2; // 9 items. so start = 4.. want to keep 0..3
        var removeCount = _store.State.Items.Count - indexStart; // 9 items so remove =5, want to remove 4..9
        var newDict = new SortedDictionary<TKey, IItem<TKey, TValue>>();
        for (var i=0;i<removeCount;i++  )
        {
            var lastItem = _store.State.Items.Last();
            _store.State.Items.Remove(lastItem.Key);
            newDict.Add(lastItem.Key,lastItem.Value);
        }
        //gotta create a new bleaf, link it and return it
        var newLeafGuid=Guid.NewGuid();
        var splitLeaf = GrainFactory.GetGrain<IBLeaf<TKey,TValue>>(newLeafGuid);
        await splitLeaf.Initialize(_store.State.Order, _store.State.Parent, newDict);
        await _store.WriteStateAsync();
        return splitLeaf;
    }

    public Task<TValue> Item(TKey key)
    {
        return Task.FromResult(_store.State.Items[key].Value);
    }

    public Task<string> ToJson()
    {
        var sb = new StringBuilder();
        sb.Append(@"""Leaf"":[");
        foreach (var item in _store.State.Items)
        {
            sb.Append( item.Value.ToJson() + ",");
        }

        sb.Remove(sb.Length - 1, 1);
        sb.Append("]");
        return Task.FromResult(sb.ToString());
    
    }

    public async Task Initialize(int stateOrder, IBNode<TKey, TValue>? stateParent, SortedDictionary<TKey, IItem<TKey, TValue>> highContent)
    {
        if (_store.State.Initialized)
        {
            throw new InvalidOperationException("Leaf already initialized");
        }

        _store.State.Initialized = true;
        _store.State.Order=stateOrder;
        _store.State.Parent=stateParent;
        _store.State.Items = highContent;
        await _store.WriteStateAsync();
    }
}

[Serializable]
public class BLeafState<TKey, TValue> where TKey : IComparable
{
    public bool Initialized { get; set; }
    public int Order { get; set; }
    public SortedDictionary<TKey,IItem<TKey, TValue>> Items { get; set; } = new();
    public IBLeaf<TKey,TValue>? NextLeaf { get; set; }
    public IBNode<TKey,TValue>? Parent { get; set; }
}