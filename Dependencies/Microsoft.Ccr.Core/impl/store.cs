using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    internal sealed class Store<T>
    {
        internal void AddReceiver(ReceiverTask r)
        {
            if (ActiveReceiver == null && (Receivers == null || Receivers.Count == 0))
            {
                ActiveReceiver = r;
                return;
            }

            if (Receivers == null) Receivers = new List<ReceiverTask>();

            if (ActiveReceiver != null)
            {
                Receivers.Add(ActiveReceiver);
                ActiveReceiver = null;
            }

            Receivers.Add(r);
        }

        internal void RemoveReceiver(ReceiverTask r)
        {
            if (ActiveReceiver == r)
            {
                ActiveReceiver = null;
                return;
            }
            if (Receivers == null) return;

            Receivers.Remove(r);
            if (Receivers.Count == 1)
            {
                ActiveReceiver = Receivers[0];
                Receivers.Clear();
            }
        }

        internal ReceiverTask[] ReceiverListAsObjectArray
        {
            get
            {
                if (ActiveReceiver != null)
                    return new[]
                    {
                        ActiveReceiver
                    };

                var newReceivers = new ReceiverTask[Receivers.Count];
                Receivers.CopyTo(newReceivers, 0);
                return newReceivers;
            }
        }
        internal int ReceiverCount
        {
            get
            {
                if (ActiveReceiver != null) return 1;
                if (Receivers == null) return 0;

                return Receivers.Count;
            }
        }

        internal ReceiverTask GetReceiverAtIndex(int i)
        {
            if (ActiveReceiver == null) return Receivers[i];
            if (i > 0) throw new ArgumentOutOfRangeException(nameof(i));

            return ActiveReceiver;
        }

        internal bool IsElementListEmpty => Elements == null;
        internal PortElement<T> ElementListFirst => Elements;

        internal void ElementListAddFirst(PortElement<T> Item)
        {
            if (Elements == null)
            {
                Elements = Item;
                Item._next = Item;
                Item._previous = Item;
                ElementCount++;
                return;
            }

            if (Elements._next == Elements)
            {
                var refElements = Elements;
                Elements = Item;
                Elements._next = refElements;
                Elements._previous = refElements;
                refElements._next = Elements;
                refElements._previous = Elements;
                ElementCount++;
                return;
            }

            var refEls = Elements;
            Elements = Item;
            Item._next = refEls;
            Item._previous = refEls._previous;
            refEls._previous._next = Item;
            refEls._previous = Item;
            ElementCount++;
        }
        internal void ElementListAddLast(PortElement<T> Item)
        {
            if (Elements == null)
            {
                Elements = Item;
                Item._next = Item;
                Item._previous = Item;
            }
            else
            {
                Elements._previous._next = Item;
                Item._previous = Elements._previous;
                Item._next = Elements;
                Elements._previous = Item;
            }
            ElementCount++;
        }
        internal PortElement<T> ElementListRemoveFirst()
        {
            if (Elements == null) return null;

            if (Elements._next == Elements)
            {
                var refElements = Elements;
                Elements = null;
                ElementCount--;
                return refElements;
            }

            var refEl = Elements;
            Elements = Elements._next;
            Elements._previous = refEl._previous;
            Elements._previous._next = Elements;
            ElementCount--;
            return refEl;
        }
        internal void ElementListRemove(PortElement<T> Item)
        {
            ElementCount--;
            if (Item == Elements)
            {
                if (ElementCount == 0)
                {
                    Elements = null;
                    return;
                }
                Elements = Item._next;
            }
            Item._previous._next = Item._next;
            Item._next._previous = Item._previous;
        }

        internal object[] ElementListAsObjectArray
        {
            get
            {
                if (IsElementListEmpty) return new object[0];

                var list = new List<IPortElement>();
                IPortElement refCurrentElement = ElementListFirst;
                do
                {
                    list.Add(refCurrentElement);
                    refCurrentElement = refCurrentElement.Next;
                } while (refCurrentElement != Elements);

                return list.ToArray();
            }
        }

        internal void Clear()
        {
            ElementCount = 0;
            Elements = null;
        }

        internal int Identity;
        private List<ReceiverTask> Receivers;
        internal ReceiverTask ActiveReceiver;
        private PortElement<T> Elements;
        internal int ElementCount;
    }
}
