using System;
using System.Drawing;

namespace data_compression_lab
{
    [Serializable]
    public class CustomPixel
    {
        public CustomPixel(Color pixel, int count)
        {
            Color = pixel;
            Count = count;
        }

        public Color Color { get; set; }
        public int Count { get; set; }
    }
}
