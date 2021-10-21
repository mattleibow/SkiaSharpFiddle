using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using SkiaSharpFiddle.ViewModels;
using Xamarin.Forms;

namespace SkiaSharpFiddle.XF
{
    public partial class MainWindow : ContentPage
    {
        private static readonly SKColor PaneColor = 0xFFF5F5F5;
        private static readonly SKColor AlternatePaneColor = 0xFFF0F0F0;
        private const int BaseBlockSize = 8;

        public Editor Editor;

        public MainWindow()
        {
            InitializeComponent();

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

            _ = LoadInitialSourceAsync();

            this.Editor = editor;
            
            VisualStateManager.GoToState(this, ViewModel.Mode.ToString());
        }

        public MainViewModel ViewModel => BindingContext as MainViewModel;

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.RasterDrawing) ||
                e.PropertyName == nameof(MainViewModel.GpuDrawing))
            {
                preview.InvalidateSurface();
            }
            else if (e.PropertyName == nameof(MainViewModel.Mode))
            {
                VisualStateManager.GoToState(this, ViewModel.Mode.ToString());
            }
        }

        private async Task LoadInitialSourceAsync()
        {
            var assembly = typeof(MainViewModel).Assembly;
            var resource = $"{typeof(MainViewModel).Namespace.Split(".")[0]}.Resources.InitialSource.cs";

            using var stream = assembly.GetManifestResourceStream(resource);
            using (var reader = new StreamReader(stream))
            {
                editor.Text = await reader.ReadToEndAsync();
            }
        }

        private void Canvas_OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var self = sender as SKCanvasView;
            var scale = self.CanvasSize.Width / self.Width;
            var width = e.Info.Width;
            var height = e.Info.Height;

            var canvas = e.Surface.Canvas;

            canvas.Clear(PaneColor);

            canvas.ClipRect(SKRect.Create(ViewModel.DrawingSize));

            DrawTransparencyBackground(canvas, width, height, (float)scale);

            if (ViewModel.RasterDrawing != null)
                canvas.DrawImage(ViewModel.RasterDrawing, 0, 0);
        }

        private void DrawTransparencyBackground(SKCanvas canvas, int width, int height, float scale)
        {
            var blockSize = BaseBlockSize * scale;

            var offsetMatrix = SKMatrix.MakeScale(2 * blockSize, blockSize);
            var skewMatrix = SKMatrix.MakeSkew(0.5f, 0);
            SKMatrix.PreConcat(ref offsetMatrix, ref skewMatrix);

            using (var path = new SKPath())
            using (var paint = new SKPaint())
            {
                path.AddRect(SKRect.Create(blockSize / -2, blockSize / -2, blockSize, blockSize));

                paint.PathEffect = SKPathEffect.Create2DPath(offsetMatrix, path);
                paint.Color = AlternatePaneColor;

                canvas.DrawRect(SKRect.Create(width + blockSize, height + blockSize), paint);
            }
        }

        private void Editor_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var source = e.NewTextValue;
            if (string.IsNullOrWhiteSpace(source))
                return;
            ViewModel.SourceCode = source;
        }
    }
}
