using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IndexingInterfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;

namespace IndexingGrains
{
    /// <inheritdoc cref="INode{TKey,TValue}"/>
    [StorageProvider(ProviderName = "indexStore")]
    public class NodeGrain<TKey, TValue> : Grain, INode<TKey, TValue> where TKey : IComparable
    {
        private readonly ILogger<NodeGrain<TKey, TValue>> _logger;
        private readonly IPersistentState<NodeState<TKey, TValue>> _store;

        public NodeGrain([PersistentState("node", "indexStore")] IPersistentState<NodeState<TKey, TValue>> store,
            ILogger<NodeGrain<TKey, TValue>> logger)
        {
            _store = store;
            _logger = logger;
        }

        public async Task<INode<TKey, TValue>?> AddItemToLeaf(KeyValuePair<TKey, TValue> item)
        {
            if (!_store.State.IsLeaf)
                throw new InvalidOperationException("Attempt to add item to non-leaf node");
            var i = _store.State.Items.Count;
            //if there are no items or the new one is bigger than the biggest
            if (i == 0 || item.Key.CompareTo(_store.State.Items[i-1].Key) >= 0)
            {
                //add to end
                _logger.LogTrace($"{item.Key} added to end");
                _store.State.Items.Add(item);
            }
            else
            {
                //where to put the new item
                _store.State.Items.Add(default);
                var itemAddedInPlace = false;
                //walk from high to low
                while (i > 0 && itemAddedInPlace == false)
                {
                    //copy the item below the item we are looking at to the point we are looking at
                    _store.State.Items[i] = _store.State.Items[i-1];
                    //if the item we have is > the one in the position we are looking at
                    if (item.Key.CompareTo(_store.State.Items[i].Key) > 0)
                    {
                        //then over ride the one in the position we are looking at (remeber this is coppied from the on below. 
                        _store.State.Items[i] = item;
                        itemAddedInPlace = true;
                        _logger.LogTrace($"{item.Key} added in place");
                        //so if we put this item here then it is > the one below
                    }
                    i--;
                }
                if (!itemAddedInPlace)
                {
                    _logger.LogTrace($"{item.Key} added at start");
                    _store.State.Items[1] = _store.State.Items[0];
                    _store.State.Items[0] = item;
                }
            }

            if (_store.State.Items.Count >= _store.State.Order) return await NewParent();
            await _store.WriteStateAsync();
            return await Task.FromResult<INode<TKey, TValue>?>(default);

        }
        public async Task<INode<TKey, TValue>?> AddNode(INode<TKey, TValue> node)
        {
            var nodeItems = await node.Items();
            var maxItem = nodeItems[^1];
            var maxKey = maxItem.Key;
            var i = _store.State.Items.Count;
            if (i == 0 || maxKey.CompareTo(_store.State.Items[i-1].Key) >= 0)
            {
                _store.State.Items.Add(maxItem);
                _store.State.Nodes.Add(node);
            }
            else
            {
                _store.State.Items.Add(maxItem);
                _store.State.Nodes.Add(node);
                var itemAddedInPlace = false;
                while (i > 0 && !itemAddedInPlace)
                {
                    _store.State.Items[i] = _store.State.Items[i-1];
                    _store.State.Nodes[i] = _store.State.Nodes[i-1];
                    if (maxKey.CompareTo(_store.State.Items[i].Key) > 0)
                    {
                        _store.State.Items[i] = maxItem;
                        _store.State.Nodes[i] = node;
                        itemAddedInPlace = true;
                    }
                    i--;
                }
                if (!itemAddedInPlace)
                {
                    _store.State.Items[1] = _store.State.Items[0];
                    _store.State.Nodes[1] = _store.State.Nodes[0];
                    _store.State.Items[0] = maxItem;
                    _store.State.Nodes[0] = node;
                }
            }

            if (_store.State.Items.Count >= _store.State.Order) return await NewParent();
            await _store.WriteStateAsync();
            return await Task.FromResult<INode<TKey, TValue>?>(default);

        }

        private async Task<INode<TKey, TValue>?> NewParent()
        {
            _logger.LogTrace("Split");
            var lowNode = GrainFactory.GetGrain<INode<TKey, TValue>>(Guid.NewGuid());
            KeyValuePair<TKey,TValue>  lastItem=await lowNode.InitializeAsSplitNode(_store.State.Order,_store.State.IsLeaf, _store.State.Items,_store.State.Nodes);
            _store.State.Items.RemoveRange(0, _store.State.Order / 2);
            if (!_store.State.IsLeaf)
                _store.State.Nodes.RemoveRange(0, _store.State.Order / 2);
            await _store.WriteStateAsync();

            if (_store.State.ParentNode != null) return await _store.State.ParentNode.AddNode(lowNode);
            var newParent = GrainFactory.GetGrain<INode<TKey, TValue>>(Guid.NewGuid());
            await newParent.InitializeAsParentNode(_store.State.Order, lowNode,lastItem,  GrainFactory.GetGrain<INode<TKey, TValue>>(this.GetPrimaryKey())
                , _store.State.Items[^1]);
            _store.State.ParentNode = newParent;
            
            await _store.WriteStateAsync();
            await lowNode.SetParent(newParent);
            return newParent;
        }

        public async Task<object> ToJson(int level = 0)
        {
            if (_store.State.IsLeaf)
            {
                //var jString = JsonConvert.SerializeObject(_store.State.Items);
                (int level,object items) response=( level, _store.State.Items);
                return response;
            }

            var subNodes = new List<(TKey, object)>();
            for (var i = 0; i < _store.State.Items.Count; i++)
            {
                var substring = await _store.State.Nodes[i].ToJson(level+1);
                var key = _store.State.Items[i].Key;
                (TKey key, object nodes) nodes = (key, substring);
                subNodes.Add(nodes);
            }
            (int Level, object nodes) subNode = (level, subNodes);

            return subNode;
        }

        public async Task Remove(TKey key)
        {
            if (!_store.State.IsLeaf) throw new InvalidOperationException("Only remove from leaf");
            var count = _store.State.Items.Count;
            var removeAt = count;
            int i;
            for (i = 0; i < count; i++)
            {
                if (_store.State.Items[i].Key.CompareTo(key) == 0)
                {
                    break;
                }
            }
            if (i == count) return;
            if (_store.State.ParentNode == null)
            {
                //this is the root node
                //delete the key
                _store.State.Items.RemoveAt(i);
                await _store.WriteStateAsync();
                return;
            }
            var nextNode = await _store.State.ParentNode.NextNode(key);
            if (nextNode == null)
            {
                // this is the last node under the parent
                return;
            }
            // check the next node to see if we need to merge
            var nextNodeSize = await nextNode.NodeSize();
            // if we do then add these items to next node
            // tell parent to delete this node
        }

        public async Task RemoveNode(TKey key)
        {
            await Task.CompletedTask;
        }

        public async Task<int> NodeSize()
        {
            return await Task.FromResult(  _store.State.Items.Count);
        }

        public async Task<INode<TKey, TValue>?> NextNode(TKey key)
        {
            await Task.CompletedTask;
            //we know this node contains the key, that why we want the next one
            var i = _store.State.Items.Count - 1;
            while (i >= 0)
            {
                if (key.CompareTo(_store.State.Items[i].Key) == 0)
                {
                    //found the one, so want the one above
                    //if it is there
                    if (i == _store.State.Items.Count - 1)
                    {
                        //at the end
                        return null;
                    }
                    return _store.State.Nodes[i + 1];
                }
                i--;
            }

            return null; //wont happen
        }


        public async Task<INode<TKey, TValue>?> GetParent()
        {
            await Task.CompletedTask;
            return _store.State.ParentNode;
        }

        public async Task InitializeAsLeafNode(int order)
        {
            _store.State.IsInitialized = true;
            _store.State.Order = order;
            _store.State.IsLeaf = true;
            _store.State.Items = new List<KeyValuePair<TKey, TValue>>();
            _store.State.Nodes = new List<INode<TKey, TValue>>();
            _store.State.ParentNode = null;
            await _store.WriteStateAsync();
        }

        public async Task InitializeAsParentNode(int order,  INode<TKey, TValue> lowNode,KeyValuePair<TKey,TValue> lastLowItem, INode<TKey, TValue> higNode, KeyValuePair<TKey,TValue>lastHighItem)
        {
            await InitializeAsLeafNode(order);
            _store.State.IsLeaf = false;
            _store.State.Items.Add(lastLowItem);
            _store.State.Items.Add(lastHighItem);
            _store.State.Nodes.Add(lowNode);
            _store.State.Nodes.Add(higNode);
            await _store.WriteStateAsync();
        }

        public async Task<KeyValuePair<TKey,TValue>> InitializeAsSplitNode(int order,bool isLeaf,List<KeyValuePair<TKey,TValue>> items, List<INode<TKey, TValue>> nodes)
        {
            await InitializeAsLeafNode(order);
            int itemCount = order / 2;
            _store.State.Items.AddRange(items.GetRange(0,itemCount));
            _store.State.IsLeaf = isLeaf;
            if (!_store.State.IsLeaf)
            {
                _store.State.Nodes.AddRange(nodes.GetRange(0,itemCount));
            }

            await _store.WriteStateAsync();
            var lastItem = _store.State.Items[^1];
            return lastItem;
        }

        public async Task<bool> IsLeaf()
        {
            return await Task.FromResult(_store.State.IsLeaf);
        }

        public async Task<IList<KeyValuePair<TKey, TValue>>> Items()
        {
            return await Task.FromResult( (_store.State.Items.Count==0)?new List<KeyValuePair<TKey, TValue>>(): _store.State.Items.GetRange(0,_store.State.Items.Count));
        }

        public async Task<IList<INode<TKey, TValue>>> Nodes()
        {
            return await Task.FromResult((_store.State.Nodes.Count == 0) ? new List<INode<TKey, TValue>>() : _store.State.Nodes.GetRange(0, _store.State.Nodes.Count));
        }

        public async Task<INode<TKey, TValue>> NodeWithValue(TKey key)
        {
            if (_store.State.IsLeaf)
            {
                return GrainFactory.GetGrain<INode<TKey, TValue>>(this.GetPrimaryKey());
            }

            var nodeCount = _store.State.Items.Count;
            for (var i = 0; i < nodeCount; i++)
            {
                if (key.CompareTo(_store.State.Items[i].Key) <= 0)
                {
                    return await _store.State.Nodes[i].NodeWithValue(key);
                }
            }
            return await _store.State.Nodes[nodeCount-1].NodeWithValue(key);
        }

        public async Task<KeyValuePair<TKey, TValue>> LastItem()
        {
            await Task.CompletedTask;
            return _store.State.Items[_store.State.Items.Count];
        }

        public async Task SetParent(INode<TKey, TValue> newParent)
        {
            if (_store.State.ParentNode != null)
                throw new InvalidOperationException("Cannot set parent when parent has a value");
            var parentsParent = await newParent.GetParent();
            if (parentsParent != null)
                throw new InvalidOperationException("Parent must be the new root");
            _store.State.ParentNode = newParent;
            await _store.WriteStateAsync();
        }

        public async Task<TValue?> Value(TKey key)
        {
            await Task.CompletedTask;
            if (!_store.State.IsLeaf)
            {
                throw new InvalidOperationException("Only leaves have values");
            }

            return _store.State.Items.FirstOrDefault(o => o.Key.CompareTo(key) == 0).Value;

        }
    }
    [Serializable]
    public class NodeState<TKey, TValue> where TKey : IComparable
    {
        public bool IsInitialized { get; set; } = false;
        public int Order { get; set; }
        public bool IsLeaf { get; set; }
        public INode<TKey, TValue>? ParentNode { get; set; }
        public List<KeyValuePair<TKey, TValue>> Items { get; set; } = new();
        public List<INode<TKey, TValue>> Nodes { get; set; } = new();
    }
}
