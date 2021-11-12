using Orleans;

namespace IBIndex;

public interface IBNode<TKey, TValue>:IGrainWithGuidKey where TKey : IComparable 
{
    //Task <List<IItem<TKey,TValue>>> Contents();
    Task<IBNode<TKey, TValue>?> Add(IItem<TKey, TValue> item);
    Task<TValue?> Item(TKey key);
    Task<string> ToJson();

    Task Initialize(int stateOrder, IBNode<TKey, TValue>? stateParent, SortedDictionary<TKey, IItem<TKey, TValue>> highContent);
}