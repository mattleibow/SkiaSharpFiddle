using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using MvvmHelpers;
using SkiaSharp;
using SkiaSharpFiddle.GlContexts;

namespace SkiaSharpFiddle
{
    public class MainViewModel : BaseViewModel
    {
        private readonly Compiler compiler = new Compiler();

        private string sourceCode;

        private int drawingWidth = 256;
        private int drawingHeight = 256;
        private ColorCombination colorCombination;

        private SKImage rasterDrawing;
        private SKImage gpuDrawing;

        private Mode mode = Mode.Ready;

        private CancellationTokenSource cancellation;
        private CompilationResult lastResult;

        public MainViewModel()
        {
            var color = SKImageInfo.PlatformColorType;
            var colorString = color == SKColorType.Bgra8888 ? "BGRA" : "RGBA";
            ColorCombinations = new ColorCombination[]
            {
                new ColorCombination(colorString, color, null),
                new ColorCombination($"{colorString} (sRGB)", color, SKColorSpace.CreateSrgb()),
                new ColorCombination("F16 (sRGB Linear)", SKColorType.RgbaF16, SKColorSpace.CreateSrgbLinear()),
            };

            CompilationMessages = new ObservableRangeCollection<CompilationMessage>();

            var skiaAss = typeof(SKSurface).Assembly;
            if (skiaAss.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute)) is AssemblyInformationalVersionAttribute informational)
                SkiaSharpVersion = informational.InformationalVersion;
            else if (skiaAss.GetCustomAttribute(typeof(AssemblyFileVersionAttribute)) is AssemblyFileVersionAttribute fileVersion)
                SkiaSharpVersion = fileVersion.Version;
            else if (skiaAss.GetCustomAttribute(typeof(AssemblyVersionAttribute)) is AssemblyVersionAttribute version)
                SkiaSharpVersion = version.Version;
            else
                SkiaSharpVersion = "<unknown>";
        }

        public ColorCombination[] ColorCombinations { get; }

        public SKSizeI DrawingSize => new SKSizeI(DrawingWidth, DrawingHeight);

        public SKImageInfo ImageInfo => new SKImageInfo(DrawingWidth, DrawingHeight);

        public ObservableRangeCollection<CompilationMessage> CompilationMessages { get; }

        public string SkiaSharpVersion { get; }

        public string SourceCode
        {
            get => sourceCode;
            set => SetProperty(ref sourceCode, value, onChanged: OnSourceCodeChanged);
        }

        public int DrawingWidth
        {
            get => drawingWidth;
            set => SetProperty(ref drawingWidth, value, onChanged: OnDrawingSizeChanged);
        }

        public int DrawingHeight
        {
            get => drawingHeight;
            set => SetProperty(ref drawingHeight, value, onChanged: OnDrawingSizeChanged);
        }

        public ColorCombination ColorCombination
        {
            get => colorCombination;
            set => SetProperty(ref colorCombination, value, onChanged: OnColorCombinationChanged);
        }

        public SKImage RasterDrawing
        {
            get => rasterDrawing;
            private set => SetProperty(ref rasterDrawing, value);
        }

        public SKImage GpuDrawing
        {
            get => gpuDrawing;
            private set => SetProperty(ref gpuDrawing, value);
        }

        public Mode Mode
        {
            get => mode;
            private set => SetProperty(ref mode, value);
        }

        private void OnDrawingSizeChanged()
        {
            OnPropertyChanged(nameof(DrawingSize));
            OnPropertyChanged(nameof(ImageInfo));

            GenerateDrawings();
        }

        private async void OnSourceCodeChanged()
        {
            cancellation?.Cancel();
            cancellation = new CancellationTokenSource();

            Mode = Mode.Working;

            try
            {
                lastResult = await compiler.CompileAsync(SourceCode, cancellation.Token);
                CompilationMessages.ReplaceRange(lastResult.CompilationMessages);

                Mode = lastResult.HasErrors ? Mode.Error : Mode.Ready;
            }
            catch (OperationCanceledException)
            {
            }

            GenerateDrawings();
        }

        private void OnColorCombinationChanged()
        {
            GenerateDrawings();
        }

        private void GenerateDrawings()
        {
            GenerateRasterDrawing();
            GenerateGpuDrawing();
        }

        private void GenerateRasterDrawing()
        {
            var old = RasterDrawing;

            var info = ImageInfo;
            using (var surface = SKSurface.Create(info))
            {
                Draw(surface, info);

                ApplyShader(surface.Canvas);

                RasterDrawing = surface.Snapshot();
            }

            old?.Dispose();
        }

        private void GenerateGpuDrawing()
        {
            var old = gpuDrawing;
            var info = ImageInfo;

            using (var context = new WglContext())
            {
                context.MakeCurrent();

                using (var grContext = GRContext.CreateGl())
                using (var surface = SKSurface.Create(grContext, true, info))
                {
                    var canvas = surface.Canvas;

                    ApplyShader(canvas);

                    Draw(surface, info);
                    gpuDrawing = surface.Snapshot().ToRasterImage();
                }
            }

            old?.Dispose();
        }

        private void Draw(SKSurface surface, SKImageInfo info)
        {
            var messages = lastResult?.Draw(surface, info.Size);

            if (messages?.Any() == true)
                CompilationMessages.ReplaceRange(messages);
        }

        public void ApplyShader(SKCanvas canvas)
        {
            float threshold = 1.05f;
            float exponent = 1.5f;

            // shader
            var src = @"
                in fragmentProcessor color_map;

                uniform float scale;
                uniform half exp;
                uniform float3 in_colors0;

                half4 main(float2 p) {
                    half4 texColor = sample(color_map, p);
                    if (length(abs(in_colors0 - pow(texColor.rgb, half3(exp)))) < scale)
                        discard;
                    return texColor;
                }";
            using var effect = SKRuntimeEffect.Create(src, out var errorText);

            // input values
            var inputs = new SKRuntimeEffectUniforms(effect);
            inputs["scale"] = threshold;
            inputs["exp"] = exponent;
            inputs["in_colors0"] = new[] { 1f, 1f, 1f };

            // shader values
            using var blueShirt = CreateTestBitmap();
            using var textureShader = blueShirt.ToShader();
            var children = new SKRuntimeEffectChildren(effect);
            children["color_map"] = textureShader;

            // create actual shader
            using var shader = effect.ToShader(true, inputs, children);

            // draw as normal
            canvas.Clear(SKColors.Black);
            using var paint = new SKPaint { Shader = shader };
            canvas.DrawRect(SKRect.Create(400, 400), paint);
        }

        protected static SKBitmap CreateTestBitmap(byte alpha = 255)
        {
            var bmp = new SKBitmap(40, 40);
            bmp.Erase(SKColors.Transparent);

            using (var canvas = new SKCanvas(bmp))
            {
                DrawTestBitmap(canvas, 40, 40, alpha);
            }

            return bmp;
        }

        private static void DrawTestBitmap(SKCanvas canvas, int width, int height, byte alpha = 255)
        {
            using var paint = new SKPaint();

            var x = width / 2;
            var y = height / 2;

            canvas.Clear(SKColors.Transparent);

            paint.Color = SKColors.Red.WithAlpha(alpha);
            canvas.DrawRect(SKRect.Create(0, 0, x, y), paint);

            paint.Color = SKColors.Green.WithAlpha(alpha);
            canvas.DrawRect(SKRect.Create(x, 0, x, y), paint);

            paint.Color = SKColors.Blue.WithAlpha(alpha);
            canvas.DrawRect(SKRect.Create(0, y, x, y), paint);

            paint.Color = SKColors.Yellow.WithAlpha(alpha);
            canvas.DrawRect(SKRect.Create(x, y, x, y), paint);
        }
    }
}
