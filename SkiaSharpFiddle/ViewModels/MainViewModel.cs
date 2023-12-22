using System;
using System.Linq;
using System.Reflection;
using System.Threading;
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
                    gpuDrawing = surface.Snapshot();
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
    }
}
