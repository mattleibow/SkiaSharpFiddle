namespace SkiaSharpFiddle
{
    public class CompilationMessage
    {
        public bool IsError { get; set; }

        public string Message { get; set; }

        public string DisplayMessage => $"[{LineNumber}] {Message}";

        public int StartOffset { get; set; }

        public int EndOffset { get; set; }

        public int LineNumber { get; set; }
    }
}
