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

            _ = LoadInitialSourceAsync();

            editor.TextArea.TextView.LineTransformers.Add(new CompilationResultsTransformer(ViewModel));

            editor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Colors.Transparent);
            editor.TextArea.TextView.CurrentLineBorder = new Pen(new SolidColorBrush(Color.FromRgb(234, 234, 234)), 2);

            VisualStateManager.GoToElementState(this, ViewModel.Mode.ToString(), false);
            VisualStateManager.GoToElementState(this, WindowState.ToString(), false);
        }

        public MainViewModel ViewModel => DataContext as MainViewModel;

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            VisualStateManager.GoToElementState(this, WindowState.ToString(), false);
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.RasterDrawing) ||
                e.PropertyName == nameof(MainViewModel.GpuDrawing))
            {
                preview.InvalidateVisual();
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

        private async Task LoadInitialSourceAsync()
        {
            var type = typeof(MainWindow);
            var assembly = type.Assembly;

            var resource = $"{type.Namespace}.Resources.InitialSource.cs";

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
    }
}
