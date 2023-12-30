using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SkiaSharpFiddle.GlContexts
{
	internal enum XVisualClass : int {
		StaticGray = 0,
		GrayScale = 1,
		StaticColor = 2,
		PseudoColor = 3,
		TrueColor = 4,
		DirectColor = 5
	}
}
