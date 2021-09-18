using Microsoft.Ccr.Core.Arbiters;
using System;
using System.Collections.Generic;

namespace Microsoft.Ccr.Core
{
    internal sealed class Store<T>
    {
        internal void AddReceiver(ReceiverTask r)
        {
            if (this.ActiveReceiver == null && (this.Receivers == null || this.Receivers.Count == 0))
            {
                this.ActiveReceiver = r;
                return;
            }
            if (this.Receivers == null)
            {
                this.Receivers = new List<ReceiverTask>();
            }
            if (this.ActiveReceiver != null)
            {
                this.Receivers.Add(this.ActiveReceiver);
                this.ActiveReceiver = null;
            }
            this.Receivers.Add(r);
        }

        internal void RemoveReceiver(ReceiverTask r)
        {
            if (this.ActiveReceiver == r)
            {
                this.ActiveReceiver = null;
                return;
            }
            if (this.Receivers == null)
            {
                return;
            }
            this.Receivers.Remove(r);
            if (this.Receivers.Count == 1)
            {
                this.ActiveReceiver = this.Receivers[0];
                this.Receivers.Clear();
            }
        }

        internal ReceiverTask[] ReceiverListAsObjectArray
        {
            get
            {
                if (this.ActiveReceiver != null)
                {
                    return new ReceiverTask[]
                    {
                        this.ActiveReceiver
                    };
                }
                ReceiverTask[] array = new ReceiverTask[this.Receivers.Count];
                this.Receivers.CopyTo(array, 0);
                return array;
            }
        }

        internal int ReceiverCount
        {
            get
            {
                if (this.ActiveReceiver != null)
                {
                    return 1;
                }
                if (this.Receivers == null)
                {
                    return 0;
                }
                return this.Receivers.Count;
            }
        }

        internal ReceiverTask GetReceiverAtIndex(int i)
        {
            if (this.ActiveReceiver == null)
            {
                return this.Receivers[i];
            }
            if (i > 0)
            {
                throw new ArgumentOutOfRangeException("i");
            }
            return this.ActiveReceiver;
        }

        internal bool IsElementListEmpty
        {
            get
            {
                return this.Elements == null;
            }
        }

        internal PortElement<T> ElementListFirst
        {
            get
            {
                return this.Elements;
            }
        }

        internal void ElementListAddFirst(PortElement<T> Item)
        {
            if (this.Elements == null)
            {
                this.Elements = Item;
                Item._next = Item;
                Item._previous = Item;
                this.ElementCount++;
                return;
            }
            if (this.Elements._next == this.Elements)
            {
                PortElement<T> elements = this.Elements;
                this.Elements = Item;
                this.Elements._next = elements;
                this.Elements._previous = elements;
                elements._next = this.Elements;
                elements._previous = this.Elements;
                this.ElementCount++;
                return;
            }
            PortElement<T> elements2 = this.Elements;
            this.Elements = Item;
            Item._next = elements2;
            Item._previous = elements2._previous;
            elements2._previous._next = Item;
            elements2._previous = Item;
            this.ElementCount++;
        }

        internal void ElementListAddLast(PortElement<T> Item)
        {
            if (this.Elements == null)
            {
                this.Elements = Item;
                Item._next = Item;
                Item._previous = Item;
            }
            else
            {
                this.Elements._previous._next = Item;
                Item._previous = this.Elements._previous;
                Item._next = this.Elements;
                this.Elements._previous = Item;
            }
            this.ElementCount++;
        }

        internal PortElement<T> ElementListRemoveFirst()
        {
            if (this.Elements == null)
            {
                return null;
            }
            if (this.Elements._next == this.Elements)
            {
                PortElement<T> elements = this.Elements;
                this.Elements = null;
                this.ElementCount--;
                return elements;
            }
            PortElement<T> elements2 = this.Elements;
            this.Elements = this.Elements._next;
            this.Elements._previous = elements2._previous;
            this.Elements._previous._next = this.Elements;
            this.ElementCount--;
            return elements2;
        }

        internal void ElementListRemove(PortElement<T> Item)
        {
            this.ElementCount--;
            if (Item == this.Elements)
            {
                if (this.ElementCount == 0)
                {
                    this.Elements = null;
                    return;
                }
                this.Elements = Item._next;
            }
            Item._previous._next = Item._next;
            Item._next._previous = Item._previous;
        }

        internal object[] ElementListAsObjectArray
        {
            get
            {
                if (this.IsElementListEmpty)
                {
                    return new object[0];
                }
                List<IPortElement> list = new List<IPortElement>();
                IPortElement portElement = this.ElementListFirst;
                do
                {
                    list.Add(portElement);
                    portElement = portElement.Next;
                }
                while (portElement != this.Elements);
                return list.ToArray();
            }
        }

        internal void Clear()
        {
            this.ElementCount = 0;
            this.Elements = null;
        }

        internal int Identity;

        private List<ReceiverTask> Receivers;

        internal ReceiverTask ActiveReceiver;

        private PortElement<T> Elements;

        internal int ElementCount;
    }
}
