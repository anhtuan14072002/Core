//  Copyright (c) 2020-present amlovey
//  
#if PLAYMAKER
using UnityEngine;

namespace Yade.Runtime.PlayMaker
{
    public class FsmCell : Object
    {
        public FsmCell()
        {
            Value = null;
        }

        public Cell Value { get; set; }
    }
}
#endif