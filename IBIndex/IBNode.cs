using Orleans;

namespace IBIndex;

public interface IBNode<TKey, TValue>:IGrainWithGuidKey where TKey : IComparable 
{
    
}