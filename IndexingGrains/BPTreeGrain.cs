using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IndexingInterfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;

namespace IndexingGrains
{
    [StorageProvider(ProviderName = "indexStore")]
    public class BPTreeGrain<TKey,TValue>:Grain,IBPTree<TKey,TValue> where TKey:IComparable where TValue:struct
    {
        private readonly IPersistentState<BPTreeState<TKey, TValue>> _store;
        private readonly ILogger<BPTreeGrain<TKey, TValue>> _logger;

        public BPTreeGrain([PersistentState("tree", "indexStore")] IPersistentState<BPTreeState<TKey, TValue>> store,
            ILogger<BPTreeGrain<TKey, TValue>> logger)
        {
            _store = store;
            _logger = logger;
        }

        public async Task Add(KeyValuePair<TKey, TValue> item)
        {
            try
            {
                _logger.LogTrace($"Add item{item}");
                if (!_store.State.IsInitialized)
                {
                    _logger.LogError($"Not initialized {this.GetPrimaryKey()}");
                    throw new InvalidOperationException("Not initialized");
                }

                if (_store.State.Node == null)
                {
                    _logger.LogError($"Not initialized {this.GrainReference}");
                    throw new InvalidOperationException("Not initialized");
                }

                var leaf = await _store.State.Node.NodeWithValue(item.Key);

                var node = await leaf.AddItemToLeaf(item);
                if (node != null)
                {
                    _logger.LogTrace($"New root {node.GetGrainIdentity().PrimaryKey}");
                    _store.State.Node = node;
                    await _store.WriteStateAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Add {item.Key}, {item.Value}, error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ToJson()
        {
            if (_store.State.Node == null) return "{}";
            var nodeJson =await _store.State.Node.ToJson(0);
            return JsonConvert.SerializeObject( nodeJson);
        }

        public async Task Remove(TKey key)
        {
            if (!_store.State.IsInitialized)
                throw new InvalidOperationException("Not initialized");
            if (_store.State.Node == null)
                return;
            var nodeWithValue = await _store.State.Node.NodeWithValue(key);
            await nodeWithValue.Remove(key);
        }

        public async Task<TValue> Get(TKey key)
        {
            if (!_store.State.IsInitialized)
                throw new InvalidOperationException("Not initialized");
            if (_store.State.Node == null)
                return default;
            var nodeWithValue = await _store.State.Node.NodeWithValue(key);
            var value = await nodeWithValue.Value(key);
            return value;
        }

        public async Task InitializeTree(int order)
        {
            _logger.LogTrace($"Initialize tree {this.GrainReference} ");
            _store.State.Order = order;
            var rootGuid = Guid.NewGuid();
            _logger.LogInformation($"Root Guid {rootGuid}");
            _store.State.Node =  GrainFactory.GetGrain<INode<TKey, TValue>>(Guid.NewGuid());
            await _store.State.Node.InitializeAsLeafNode(order);
            _store.State.IsInitialized = true;
            await _store.WriteStateAsync();
        }
    }

    [Serializable]
    public class BPTreeState<TKey, TValue> where TKey : IComparable
    {
        public int Order { get; set; }
        public INode<TKey, TValue>? Node { get; set; }
        public bool IsInitialized { get; set; }
    }
}
