using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace IndexingInterfaces
{
    public interface INode<TKey,TValue>:IGrainWithGuidKey where TKey:IComparable
    {
        /// <summary>
        /// Create a new leaf node
        /// </summary>
        /// <param name="order">max number of elements</param>
        /// <returns></returns>
        Task InitializeAsLeafNode(int order);

        /// <summary>
        /// Create a new node by copying half the items/nodes from the source node
        /// </summary>
        /// <param name="order">max number of elements</param>
        /// <param name="isLeaf"></param>
        /// <param name="items"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        Task<KeyValuePair<TKey,TValue>> InitializeAsSplitNode(int order,bool isLeaf,List<KeyValuePair<TKey,TValue>> items, List<INode<TKey, TValue>> nodes);

        /// <summary>
        /// Create a new parent as a result of a node split
        /// </summary>
        /// <param name="order">max elements</param>
        /// <param name="lowNode">Reference to the lower half of the split</param>
        /// <param name="lastLowItem"></param>
        /// <param name="higNode">Reference to the upper half of the split</param>
        /// <param name="lastHighItem"></param>
        /// <returns></returns>
        Task InitializeAsParentNode(int order,  INode<TKey, TValue> lowNode,KeyValuePair<TKey,TValue> lastLowItem, INode<TKey, TValue> higNode, KeyValuePair<TKey,TValue>lastHighItem);
        /// <summary>
        /// True if the node is a leaf
        /// </summary>
        /// <returns>True if the node is a leaf</returns>
        Task<bool> IsLeaf();
        /// <summary>
        /// Returns a reference to the parent node or null for the root
        /// </summary>
        /// <returns>
        /// The parent node <see cref="INode{TKey,TValue}"/>
        /// </returns>
        Task<INode<TKey,TValue>?> GetParent();
        /// <summary>
        /// Returns the list of items in the tree node
        /// </summary>
        /// <returns>All the items in the node</returns>
        Task<IList<KeyValuePair<TKey, TValue>>> Items();
        /// <summary>
        /// Sub nodes of the node
        /// </summary>
        /// <returns>
        /// Sub nodes of the node
        /// </returns>
        Task<IList<INode<TKey, TValue>>> Nodes();
        /// <summary>
        /// Find the value associated with a key in the tree
        /// </summary>
        /// <param name="key">The key used for the search</param>
        /// <returns>The value returned</returns>
        Task<TValue?> Value(TKey key);

        /// <summary>
        /// Adds an item to a leaf node. This method does not check that the value is allowed into this node
        /// </summary>
        /// <param name="item">Key/value to add</param>
        /// <returns>Parent node if node is split</returns>
        Task<INode<TKey, TValue>?> AddItemToLeaf(KeyValuePair<TKey, TValue> item);
        /// <summary>
        /// Adds a node to this node. The method does not check that the addition is valid
        /// </summary>
        /// <param name="node">Node to be added</param>
        /// <returns>New parent if node is split</returns>
        Task<INode<TKey, TValue>?> AddNode(INode<TKey, TValue> node);
        /// <summary>
        /// Set s the parent of the lownode after a split
        /// </summary>
        /// <param name="newParent">parent node</param>
        /// <returns></returns>
        Task SetParent(INode<TKey,TValue> newParent);
        /// <summary>
        /// navigate down the tree of nodes to find the node that should contain the key
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <returns>node</returns>
        Task<INode<TKey, TValue>> NodeWithValue(TKey key);
        /// <summary>
        /// Get the last item
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns>Last item</returns>
        Task<KeyValuePair<TKey, TValue>> LastItem() ;
        /// <summary>
        /// generate a jsonlike string
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        Task<object> ToJson(int level = 0);

        Task<int> NodeSize();
        Task Remove(TKey key);
        Task<INode<TKey,TValue>?> NextNode(TKey key) ;
    }

    
}
