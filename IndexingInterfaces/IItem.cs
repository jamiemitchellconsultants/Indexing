using System;
namespace IndexingInterfaces
{
    /// <summary>
    /// Items are the elements that are indexed in the tree
    /// </summary>
    /// <typeparam name="TKey">Type of the key</typeparam>
    /// <typeparam name="TValue">Type of the value</typeparam>
    public interface IItem<TKey,TValue> where TKey:IComparable
    {
        /// <summary>
        /// Key in the index. Secondary index of the grain
        /// </summary>
        public TKey Key { get; set; }
        /// <summary>
        /// Value to be indexed. The id of the grain that stores grain id's that are indexed with the key
        /// </summary>
        public TValue Value { get; set; }
    }
}
