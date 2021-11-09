using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBIndex;

namespace BIndexGrains;

public class Item<TKey, TValue> : IItem<TKey, TValue> where TKey : IComparable 
{
    public Item(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }

    public TKey Key { get; set; }
    public TValue Value { get; set; }

    public string ToJson()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(this);
    }
}