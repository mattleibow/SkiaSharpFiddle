using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SkiaSharp;

namespace SkiaSharpFiddle.Compiler
{
    public class CompilationResult
    {
        private MethodInfo drawMethod = null;
        private object instance = null;

        public IEnumerable<CompilationMessage> CompilationMessages { get; set; }

        public Type ScriptType { get; set; }

        public bool HasErrors => CompilationMessages.Any(m => m.IsError);

        public IEnumerable<CompilationMessage> Draw(SKSurface surface, SKSizeI drawingSize)
        {
            var messages = new List<CompilationMessage>();

            if (ScriptType != null && drawMethod == null)
            {
                drawMethod = ScriptType.GetMethod(
                     "Draw",
                     BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                     null,
                     new[] { typeof(SKCanvas), typeof(int), typeof(int) },
                     null);

                if (drawMethod == null)
                {
                    messages.Add(new CompilationMessage
                    {
                        Message = "Unable to find entry method 'void Draw(SKCanvas canvas, int width, int height)'.",
                        Severity = CompilationMessageSeverity.Error
                    });
                }
                else if (!drawMethod.IsStatic)
                {
                    instance = Activator.CreateInstance(ScriptType, new[] { new object[2] });
                }
            }

            try
            {
                drawMethod?.Invoke(instance, new object[] { surface.Canvas, drawingSize.Width, drawingSize.Height });
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException tiex)
                    ex = tiex.InnerException;

                messages.Add(new CompilationMessage
                {
                    Message = $"An error occured during execution: {ex.Message}",
                    Severity = CompilationMessageSeverity.Error
                });
            }

            return messages;
        }
    }
}
