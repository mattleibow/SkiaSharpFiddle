using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace SkiaSharpFiddle
{
    public partial class MainWindow : Window
    {
        private static readonly SKColor PaneColor = 0xFFF5F5F5;
        private static readonly SKColor AlternatePaneColor = 0xFFF0F0F0;
        private const int BaseBlockSize = 8;

        public MainWindow()
        {
            InitializeComponent();

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            ViewModel.CompilationMessages.CollectionChanged += OnCompilationMessagesChanged;

            Observable.FromEventPattern(editor, nameof(editor.TextChanged))
                .Select(evt => (evt.Sender as TextEditor)?.Text)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Throttle(TimeSpan.FromMilliseconds(250))
                .DistinctUntilChanged()
                .Subscribe(source => Dispatcher.BeginInvoke(new Action(() => ViewModel.SourceCode = source)));

            _ = LoadInitialSourceAsync(editor, @$"{typeof(MainWindow).Namespace}.Resources.InitialSource.cs");

            InitializeEditor(editor);

            VisualStateManager.GoToElementState(this, ViewModel.Mode.ToString(), false);
            VisualStateManager.GoToElementState(this, WindowState.ToString(), false);
        }

        private void InitializeEditor(TextEditor editor)
        {
            editor.TextArea.TextView.LineTransformers.Add(new CompilationResultsTransformer(ViewModel));
            editor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Colors.Transparent);
            editor.TextArea.TextView.CurrentLineBorder = new Pen(new SolidColorBrush(Color.FromRgb(234, 234, 234)), 2);
        }

        public MainViewModel ViewModel => DataContext as MainViewModel;

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            VisualStateManager.GoToElementState(this, WindowState.ToString(), false);
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.RasterDrawing))
            {
                preview.InvalidateVisual();
            }
            else if (e.PropertyName == nameof(MainViewModel.GpuDrawing) || e.PropertyName == nameof(MainViewModel.ShaderSource))
            {
                previewGpu.InvalidateVisual();
            }
            else if (e.PropertyName == nameof(MainViewModel.Mode))
            {
                VisualStateManager.GoToElementState(this, ViewModel.Mode.ToString(), false);
            }
        }

        private void OnCompilationMessagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            editor.TextArea.TextView.Redraw();
        }

        private async Task LoadInitialSourceAsync(TextEditor editor, string resource)
        {
            var type = typeof(MainWindow);
            var assembly = type.Assembly;

            using (var stream = assembly.GetManifestResourceStream(resource))
            using (var reader = new StreamReader(stream))
            {
                editor.Text = await reader.ReadToEndAsync();
            }
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var scale = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
            var width = e.Info.Width;
            var height = e.Info.Height;

            var canvas = e.Surface.Canvas;

            canvas.Clear(PaneColor);

            canvas.ClipRect(SKRect.Create(ViewModel.DrawingSize));

            DrawTransparencyBackground(canvas, width, height, (float)scale);

            // On-screen (CPU) canvas rendering
            if (ViewModel.RasterDrawing != null && sender.Equals(preview))
            {
                canvas.DrawImage(ViewModel.RasterDrawing, 0, 0);
            }
            // Off-screen (GPU) canvas rendering (SKSL Enabled)
            else if (ViewModel.GpuDrawing != null && sender.Equals(previewGpu))
            {
                canvas.DrawImage(ViewModel.GpuDrawing, 0, 0);
            }

        }
        private void DrawTransparencyBackground(SKCanvas canvas, int width, int height, float scale)
        {
            var blockSize = BaseBlockSize * scale;

            var offsetMatrix = SKMatrix.CreateScale(2 * blockSize, blockSize);
            var skewMatrix = SKMatrix.CreateSkew(0.5f, 0);
            offsetMatrix = offsetMatrix.PreConcat(skewMatrix);

            using (var path = new SKPath())
            using (var paint = new SKPaint())
            {
                path.AddRect(SKRect.Create(blockSize / -2, blockSize / -2, blockSize, blockSize));

                paint.PathEffect = SKPathEffect.Create2DPath(offsetMatrix, path);
                paint.Color = AlternatePaneColor;

                canvas.DrawRect(SKRect.Create(width + blockSize, height + blockSize), paint);
            }
        }
    }
}
