using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SkiaSharp;

namespace SkiaSharpFiddle
{
    internal class Compiler
    {
        private static readonly string[] WhitelistedAssemblies =
        {
            "netstandard",
            "mscorlib",
            "System",
            "System.Core",
            "SkiaSharp",
        };

        private readonly Assembly[] assemblyReferences;
        private readonly MetadataReference[] metadataReferences;
        private readonly CSharpCompilationOptions compilationOptions;
        private readonly CSharpParseOptions parseOptions;

        private CSharpCompilation lastSuccessfulCompilation;
        private Assembly currentScriptAssembly;
        private Type currentScriptType;
        private MethodInfo currentDrawMethod;

        public Compiler()
        {
            assemblyReferences = GetReferences().ToArray();

            metadataReferences = assemblyReferences.Select(a => MetadataReference.CreateFromFile(a.Location)).ToArray();
            compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            parseOptions = new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);
        }

        public IEnumerable<Diagnostic> Compile(string sourceCode) =>
            CompileSourceCode(SourceText.From(sourceCode));

        public void Draw(SKSurface surface, SKSizeI drawingSize) =>
            currentDrawMethod?.Invoke(null, new object[] { surface.Canvas, drawingSize.Width, drawingSize.Height });

        private IEnumerable<Assembly> GetReferences() =>
            AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => WhitelistedAssemblies.Any(wl => wl.Equals(a.GetName().Name, StringComparison.OrdinalIgnoreCase)));

        private IEnumerable<Diagnostic> CompileSourceCode(SourceText sourceCode)
        {
            currentScriptAssembly = null;
            currentScriptType = null;
            currentDrawMethod = null;

            var syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceCode, parseOptions);
            var compilation = CSharpCompilation.CreateScriptCompilation(
                "SkiaSharpDrawing", syntaxTree, metadataReferences, compilationOptions, lastSuccessfulCompilation);

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);
                if (result.Success)
                {
                    lastSuccessfulCompilation = compilation;

                    currentScriptAssembly = Assembly.Load(ms.ToArray());
                    currentScriptType = currentScriptAssembly.GetType("Script");
                    currentDrawMethod = currentScriptType.GetMethod("Draw");

                }

                return result.Diagnostics;
            }
        }
    }
}
