//  Copyright (c) 2020-present amlovey
//  
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Yade.Editor
{
    public class ElementsPool<T> where T: VisualElement, new()
    {
        private Queue<T> usings;
        public Queue<T> Usings
        {
            get
            {
                return usings;
            }
        }

        private Queue<T> unused;
        private VisualElement root;
        
        public ElementsPool(VisualElement root)
        {
            this.root = root;
            usings = new Queue<T>();
            unused = new Queue<T>();
        }
        
        public T GetOne()
        {
            T element;
            
            if (unused.Count == 0)
            {
                element = Activator.CreateInstance<T>();
                usings.Enqueue(element);
                this.root.Add(element);
                return element;
            }
            
            element = unused.Dequeue();
            element.visible = true;
            usings.Enqueue(element);
            return element;
        }
        
        public void PreparePooling()
        {
            while(usings.Count != 0)
            {
                var element = usings.Dequeue();
                element.visible = false;
                unused.Enqueue(element);
            }
        }
    } 
}