using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;
using SkiaSharpFiddle.ViewModels;

namespace SkiaSharpFiddle.Win
{
    public class CompilationResultsTransformer : ColorizingTransformer
    {
        private static readonly TextDecorationCollection errorTextDecorations;
        private static readonly TextDecorationCollection warningTextDecorations;

        static CompilationResultsTransformer()
        {
            errorTextDecorations = new TextDecorationCollection
            {
                new TextDecoration
                {
                    Location = TextDecorationLocation.Underline,
                    Pen = new Pen(new SolidColorBrush(Colors.Red), 2)
                    {
                        DashStyle =  DashStyles.Dash
                    }
                }
            };
            warningTextDecorations = new TextDecorationCollection
            {
                new TextDecoration
                {
                    Location = TextDecorationLocation.Underline,
                    Pen = new Pen(new SolidColorBrush(Colors.Green), 1)
                    {
                        DashStyle =  DashStyles.Dot
                    }
                }
            };
        }

        public CompilationResultsTransformer(MainViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public MainViewModel ViewModel { get; }

        protected override void Colorize(ITextRunConstructionContext context)
        {
            var lineStart = context.VisualLine.StartOffset;
            var lineLength = context.VisualLine.VisualLengthWithEndOfLineMarker;
            var lineEnd = lineStart + lineLength;

            foreach (var message in ViewModel.CompilationMessages)
            {
                var spanStart = message.StartOffset;
                var spanEnd = message.EndOffset;

                if (spanEnd < lineStart || spanStart > lineEnd)
                    continue;

                if (spanStart < lineStart)
                    spanStart = lineStart;
                if (spanEnd > lineEnd)
                    spanEnd = lineEnd;

                var startColumn = context.VisualLine.GetVisualColumn(spanStart - lineStart);
                var endColumn = context.VisualLine.GetVisualColumn(spanEnd - lineStart);

                if (spanStart == spanEnd)
                {
                    if (endColumn < lineLength)
                        endColumn++;
                    else if (startColumn > 0)
                        startColumn--;
                }

                ChangeVisualElements(startColumn, endColumn, element =>
                {
                    var decorations = message.IsError ? errorTextDecorations : warningTextDecorations;
                    element.TextRunProperties.SetTextDecorations(decorations);
                });
            }
        }
    }
}
