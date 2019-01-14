using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using MvvmHelpers;
using SkiaSharp;

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
        }

        public ColorCombination[] ColorCombinations { get; }

        public SKSizeI DrawingSize => new SKSizeI(DrawingWidth, DrawingHeight);

        public SKImageInfo ImageInfo => new SKImageInfo(DrawingWidth, DrawingHeight);

        public ObservableRangeCollection<CompilationMessage> CompilationMessages { get; }

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

        private void OnDrawingSizeChanged()
        {
            OnPropertyChanged(nameof(DrawingSize));
            OnPropertyChanged(nameof(ImageInfo));

            GenerateDrawings();
        }

        private void OnSourceCodeChanged()
        {
            CompilationMessages.Clear();

            var diagnostics = compiler.Compile(SourceCode);

            var messages = GetCompilationMessages(diagnostics);
            CompilationMessages.ReplaceRange(messages);

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
                compiler.Draw(surface, info.Size);
                RasterDrawing = surface.Snapshot();
            }

            old?.Dispose();
        }

        private void GenerateGpuDrawing()
        {
        }

        private IEnumerable<CompilationMessage> GetCompilationMessages(IEnumerable<Diagnostic> diagnostics)
        {
            diagnostics = diagnostics
                .Where(d => d.Location.IsInSource)
                .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
                .OrderBy(d => d.Severity)
                .OrderBy(d => d.Location.SourceSpan.Start);

            foreach (var diag in diagnostics)
            {
                yield return new CompilationMessage
                {
                    IsError = diag.Severity == DiagnosticSeverity.Error,
                    Message = $"{diag.Severity.ToString().ToLowerInvariant()} {diag.Id}: {diag.GetMessage()}",
                    StartOffset = diag.Location.SourceSpan.Start,
                    EndOffset = diag.Location.SourceSpan.End,
                    LineNumber = diag.Location.GetMappedLineSpan().Span.Start.Line + 1
                };
            }
        }
    }
}
