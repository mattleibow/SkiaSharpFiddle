using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SkiaSharpFiddle.GlContexts
{
	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}
}
