namespace Microsoft.Ccr.Core
{
    public sealed class Tuple<ITEM0>
    {
        public ITEM0 Item0
        {
            get => item0;
            set => item0 = value;
        }

        public Tuple() {}
        public Tuple(ITEM0 item0) => this.item0 = item0;

        public static implicit operator ITEM0(Tuple<ITEM0> tuple)
        {
            if (tuple != null) return tuple.Item0;
            return default;
        }

        public ITEM0 ToItem0() => Item0;

        private ITEM0 item0;
    }

    public sealed class Tuple<ITEM0, ITEM1>
    {
        public ITEM0 Item0
        {
            get => item0;
            set => item0 = value;
        }
        public ITEM1 Item1
        {
            get => item1;
            set => item1 = value;
        }

        public Tuple() { }
        public Tuple(ITEM0 item0, ITEM1 item1)
        {
            this.item0 = item0;
            this.item1 = item1;
        }

        public static implicit operator ITEM0(Tuple<ITEM0, ITEM1> tuple)
        {
            if (tuple != null) return tuple.Item0;
            return default;
        }

        public ITEM0 ToItem0() => Item0;

        public static implicit operator ITEM1(Tuple<ITEM0, ITEM1> tuple)
        {
            if (tuple != null) return tuple.Item1;
            return default;
        }

        public ITEM1 ToItem1() => Item1;

        private ITEM0 item0;
        private ITEM1 item1;
    }
}
