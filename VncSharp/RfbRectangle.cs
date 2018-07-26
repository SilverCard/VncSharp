using System.Drawing;

namespace VncSharp
{
    public class RfbRectangle
    {
        public Rectangle Rectangle { get; set; }
        public RfbEncodingType EncodingType { get; set; }

        public RfbRectangle(Rectangle r, RfbEncodingType e)
        {
            Rectangle = r;
            EncodingType = e;
        }
    }
}
