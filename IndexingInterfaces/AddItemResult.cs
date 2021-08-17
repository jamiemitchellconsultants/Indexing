using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexingInterfaces
{
    public class AddItemResult<TKey,TValue> where TKey:IComparable
    {
        public bool NewNode { get; set; } = false;
        public INode<TKey,TValue>? Node { get; set; }
    }
}
