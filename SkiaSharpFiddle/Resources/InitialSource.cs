using System;
using SkiaSharp;

void Draw(SKCanvas canvas, int width, int height)
{
	var p = new SKPaint
	{
		Color = SKColors.Red,
		IsAntialias = true,
		Style = SKPaintStyle.Stroke,
		StrokeWidth = 10
	};

	canvas.DrawLine(20, 20, width/2, height/2, p);
}
