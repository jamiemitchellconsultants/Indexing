using Orleans;

namespace IBIndex
{
    public interface IBPTree<TKey,TValue>:IGrainWithGuidKey where TKey:IComparable {
        Task InitializeTree(int order);
        Task<TValue?> Get(TKey key);
        Task Add(IItem<TKey, TValue> item);
        Task<string> ToJson();
    }
}
