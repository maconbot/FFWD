﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PressPlay.FFWD.Components
{
    internal class RenderQueue : IComparer<RenderItem>
    {
        public RenderQueue(int capacity)
        {
            list = new List<RenderItem>(capacity);
        }

        private List<RenderItem> list;
        private static Queue<Renderer> updatedRenderers = new Queue<Renderer>(ApplicationSettings.DefaultCapacities.RenderCullingQueue);

        public void Add(RenderItem item)
        {
            int index = list.BinarySearch(item, this);
            if (index < 0)
            {
                list.Insert(~index, item);
            }
            else
            {
                if (list[index] != item)
                {
                    list.Insert(index, item);
                }
            }
        }

        public void Remove(RenderItem item)
        {
            int index = list.BinarySearch(item, this);
            if (index >= 0)
            {
                list.RemoveAt(index);
            }
        }

        public RenderItem this[int index]
        {
            get
            {
                return list[index];
            }
        }

        public int Count
        {
            get 
            {
                return list.Count;
            }
        }

        public int Compare(RenderItem x, RenderItem y)
        {
            int idx = x.Priority.CompareTo(y.Priority);
            if (idx == 0)
	        {
                return x.ToString().CompareTo(y.ToString());
	        }
            return idx;
        }

        internal static void ReconsiderForCulling(Renderer r)
        {
            updatedRenderers.Enqueue(r);
        }

        internal static Renderer GetUpdatedRenderer()
        {
            if (updatedRenderers.Count == 0)
            {
                return null;
            }
            return updatedRenderers.Dequeue();
        }

        internal void Clear()
        {
            list.Clear();
        }
    }
}
