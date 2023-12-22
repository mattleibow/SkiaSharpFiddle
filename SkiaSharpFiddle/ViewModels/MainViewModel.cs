using System;
using System.Diagnostics;
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
        private string shaderSource;

        private int drawingWidth = 256;
        private int drawingHeight = 256;
        private ColorCombination colorCombination;

        private SKImage rasterDrawing;
        private SKImage gpuDrawing;

        private Mode mode = Mode.Ready;

        private CancellationTokenSource cancellation;
        private CompilationResult lastResult;
        private Stopwatch m_StopWatch = Stopwatch.StartNew();

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

        public string ShaderSource
        {
            get => shaderSource;
            set => SetProperty(ref shaderSource, value);
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

        public void GenerateDrawings()
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

                //ApplyShader(surface.Canvas);

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

                    Draw(surface, info);
                    ApplyShader(surface);

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

        public void ApplyShader(SKSurface surface)
        {
            var canvas = surface.Canvas;
            string errorText = @"";

            // shader
            try
            {
                using var effect = SKRuntimeEffect.Create(shaderSource, out errorText);
                // input values

                var inputs = new SKRuntimeEffectUniforms(effect);
                inputs["iResolution"] = new[] { DrawingWidth, drawingHeight, 400f };
                inputs["iTime"] = m_StopWatch.ElapsedMilliseconds / 100;

                // shader values
                using var snapshotImage = surface.Snapshot().ToRasterImage();
                using var textureShader = snapshotImage.ToShader();

                var children = new SKRuntimeEffectChildren(effect);
                children["color_map"] = textureShader;

                // create actual shader
                using var shader = effect.ToShader(true, inputs, children);

                // draw as normal
                canvas.Clear(SKColors.Black);
                using var paint = new SKPaint { Shader = shader };
                canvas.DrawRect(SKRect.Create(400, 400), paint);

                CompilationMessages.Clear();
            }
            catch
            {
                var result = new CompilationMessage() { 
                    Message = errorText,
                    Severity = CompilationMessageSeverity.Error};
                CompilationMessages.Add(result);
            }

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
