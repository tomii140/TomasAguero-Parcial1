using System;
using System.Collections.Generic;

public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private struct NodeItem : IComparable<NodeItem>
    {
        public TElement Element;
        public TPriority Priority;

        public int CompareTo(NodeItem other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }

    private List<NodeItem> elements = new List<NodeItem>();

    public int Count => elements.Count;

    public void Enqueue(TElement element, TPriority priority)
    {
        elements.Add(new NodeItem { Element = element, Priority = priority });
        int ci = elements.Count - 1;
        while (ci > 0)
        {
            int pi = (ci - 1) / 2;
            if (elements[ci].CompareTo(elements[pi]) >= 0) break;
            NodeItem tmp = elements[ci]; elements[ci] = elements[pi]; elements[pi] = tmp;
            ci = pi;
        }
    }

    public TElement Dequeue()
    {
        int li = elements.Count - 1;
        TElement frontItem = elements[0].Element;
        elements[0] = elements[li];
        elements.RemoveAt(li);

        --li;
        int pi = 0;
        while (true)
        {
            int ci = pi * 2 + 1;
            if (ci > li) break;
            int rc = ci + 1;
            if (rc <= li && elements[rc].CompareTo(elements[ci]) < 0)
                ci = rc;
            if (elements[pi].CompareTo(elements[ci]) <= 0) break;
            NodeItem tmp = elements[pi]; elements[pi] = elements[ci]; elements[ci] = tmp;
            pi = ci;
        }
        return frontItem;
    }

    public void Clear() => elements.Clear();
}
