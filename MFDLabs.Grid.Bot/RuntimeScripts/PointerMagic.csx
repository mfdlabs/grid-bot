struct LOL
{
    public int Deez { get; set; }
    public int Hello() { return 123; }

    //public static LOL* operator ->() { return &this; }
}

var m = new LOL { Deez = 1 };

unsafe
{
    fixed (LOL* e = &m)
    {
        return e->Hello();
    }
}