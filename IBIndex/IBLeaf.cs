using Orleans;

namespace IBIndex;

public interface IBLeaf<TKey, TValue>:IGrainWithGuidKey where TKey : IComparable 
{
    Task <List<IItem<TKey,TValue>>> Contents();
    Task<IBNode<TKey, TValue>> Add(IItem<TKey, TValue> item);
    Task<TValue> Item(TKey key);
    Task<string> ToJson();

}