using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace IndexingInterfaces
{
    public interface IBPTree<TKey,TValue>:IGrainWithGuidKey where TKey:IComparable 
    {
        Task InitializeTree(int order);
        Task<TValue> Get(TKey key);
        Task Add(KeyValuePair<TKey, TValue> item);
        Task<string> ToJson();
    }
}
