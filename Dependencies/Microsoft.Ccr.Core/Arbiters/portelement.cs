namespace Microsoft.Ccr.Core.Arbiters
{
    // Token: 0x02000035 RID: 53
    public class PortElement<T> : IPortElement<T>, IPortElement
    {

        public IPort Owner
        {
            get
            {
                return this._Owner;
            }
            set
            {
                this._Owner = (Port<T>)value;
            }
        }

        public IPortElement Next
        {
            get
            {
                return this._next;
            }
            set
            {
                this._next = (PortElement<T>)value;
            }
        }

        public IPortElement Previous
        {
            get
            {
                return this._previous;
            }
            set
            {
                this._previous = (PortElement<T>)value;
            }
        }

        public object CausalityContext
        {
            get
            {
                return this._causalityContext;
            }
            set
            {
                this._causalityContext = value;
            }
        }

        public object Item
        {
            get
            {
                return this._item;
            }
        }

        public T TypedItem
        {
            get
            {
                return this._item;
            }
            internal set
            {
                this._item = value;
            }
        }

        public PortElement(T item)
        {
            this._item = item;
        }

        public PortElement(T item, Port<T> owner)
        {
            this._Owner = owner;
            this._item = item;
        }

        private Port<T> _Owner;

        internal PortElement<T> _next;

        internal PortElement<T> _previous;

        internal object _causalityContext;

        internal T _item;
    }
}
