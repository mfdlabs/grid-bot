using System.Drawing;

namespace MFDLabs.Drawing.Models
{
    public struct BaseRectangle
    {
        public BaseRectangle(BaseRectangle Rectangle)
        {
            this = new BaseRectangle(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom);
        }

        public BaseRectangle(int Left, int Top, int Right, int Bottom)
        {
            _Left = Left;
            _Top = Top;
            _Right = Right;
            _Bottom = Bottom;
        }

        public int X
        {
            get
            {
                return _Left;
            }
            set
            {
                _Left = value;
            }
        }

        public int Y
        {
            get
            {
                return _Top;
            }
            set
            {
                _Top = value;
            }
        }

        public int Left
        {
            get
            {
                return _Left;
            }
            set
            {
                _Left = value;
            }
        }

        public int Top
        {
            get
            {
                return _Top;
            }
            set
            {
                _Top = value;
            }
        }

        public int Right
        {
            get
            {
                return _Right;
            }
            set
            {
                _Right = value;
            }
        }

        public int Bottom
        {
            get
            {
                return _Bottom;
            }
            set
            {
                _Bottom = value;
            }
        }

        public int Height
        {
            get
            {
                return _Bottom - _Top;
            }
            set
            {
                _Bottom = value + _Top;
            }
        }

        public int Width
        {
            get
            {
                return _Right - _Left;
            }
            set
            {
                _Right = value + _Left;
            }
        }

        public Point Location
        {
            get
            {
                return new Point(Left, Top);
            }
            set
            {
                _Left = value.X;
                _Top = value.Y;
            }
        }

        public Size Size
        {
            get
            {
                return new Size(Width, Height);
            }
            set
            {
                _Right = value.Width + _Left;
                _Bottom = value.Height + _Top;
            }
        }

        public static implicit operator Rectangle(BaseRectangle Rectangle)
        {
            return new Rectangle(Rectangle.Left, Rectangle.Top, Rectangle.Width, Rectangle.Height);
        }

        public static implicit operator BaseRectangle(Rectangle Rectangle)
        {
            return new BaseRectangle(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom);
        }

        public static bool operator ==(BaseRectangle Rectangle1, BaseRectangle Rectangle2)
        {
            return Rectangle1.Equals(Rectangle2);
        }

        public static bool operator !=(BaseRectangle Rectangle1, BaseRectangle Rectangle2)
        {
            return !Rectangle1.Equals(Rectangle2);
        }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                "{Left: ",
                _Left,
                "; Top: ",
                _Top,
                "; Right: ",
                _Right,
                "; Bottom: ",
                _Bottom,
                "}"
            });
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool Equals(BaseRectangle Rectangle)
        {
            return Rectangle.Left == _Left && Rectangle.Top == _Top && Rectangle.Right == _Right && Rectangle.Bottom == _Bottom;
        }

        public override bool Equals(object Object)
        {
            bool flag = Object is BaseRectangle;
            bool result;
            if (flag)
            {
                result = Equals((BaseRectangle)Object);
            }
            else
            {
                bool flag2 = Object is Rectangle;
                result = (flag2 && Equals(new BaseRectangle((Rectangle)Object)));
            }
            return result;
        }

        private int _Left;

        private int _Top;

        private int _Right;

        private int _Bottom;
    }
}
