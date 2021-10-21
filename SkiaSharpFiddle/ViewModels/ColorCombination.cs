using SkiaSharp;

namespace SkiaSharpFiddle.ViewModels
{
    public class ColorCombination
    {
        public ColorCombination(string name, SKColorType colorType, SKColorSpace colorSpace)
        {
            Name = name;
            ColorType = colorType;
            ColorSpace = colorSpace;
        }

        public string Name { get; }

        public SKColorType ColorType { get; set; }

        public SKColorSpace ColorSpace { get; set; }
    }
}
