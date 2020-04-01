﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SkiaSharp;

namespace SkiaSharpFiddle
{
    internal class Compiler
    {
        private readonly CSharpCompilationOptions compilationOptions;
        private readonly CSharpParseOptions parseOptions;

        private readonly object referencesLocker = new object();
        private Assembly[] assemblyReferences;
        private MetadataReference[] metadataReferences;
        private AssemblyLoader assemblyLoader;

        public Compiler()
        {
            assemblyLoader = new AssemblyLoader();
            compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            parseOptions = new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);
        }

        public async Task<CompilationResult> CompileAsync(string sourceCode, CancellationToken cancellationToken = default)
        {
            var result = await Task.Run(() =>
            {
                // load references
                lock (referencesLocker)
                {
                    assemblyReferences = assemblyLoader.GetReferences().ToArray();
                    metadataReferences = assemblyReferences.Select(a => MetadataReference.CreateFromFile(a.Location)).ToArray();
                }

                cancellationToken.ThrowIfCancellationRequested();

                // compile the code
                return CompileSourceCode(SourceText.From(sourceCode), cancellationToken);
            }, cancellationToken);

            return result;
        }

        private CompilationResult CompileSourceCode(SourceText sourceCode, CancellationToken cancellationToken = default)
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(
                sourceCode, parseOptions, cancellationToken: cancellationToken);

            var compilation = CSharpCompilation.CreateScriptCompilation(
                "SkiaSharpDrawing", syntaxTree, metadataReferences, compilationOptions);

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms, cancellationToken: cancellationToken);

                Type scriptType = null;
                if (result.Success)
                {
                    var assembly = Assembly.Load(ms.ToArray());
                    scriptType = assembly.GetType("Script");
                }

                return new CompilationResult
                {
                    CompilationMessages = GetCompilationMessages(result.Diagnostics),
                    ScriptType = scriptType,
                };
            }
        }

        private IEnumerable<CompilationMessage> GetCompilationMessages(IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken = default)
        {
            diagnostics = diagnostics
                .Where(d => d.Location.IsInSource)
                .OrderBy(d => d.Severity)
                .OrderBy(d => d.Location.SourceSpan.Start);

            foreach (var diag in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return new CompilationMessage
                {
                    Severity = diag.Severity.ToCompilationMessageSeverity(),
                    Message = $"{diag.Id}: {diag.GetMessage()}",
                    StartOffset = diag.Location.SourceSpan.Start,
                    EndOffset = diag.Location.SourceSpan.End,
                    LineNumber = diag.Location.GetMappedLineSpan().Span.Start.Line + 1
                };
            }
        }
    }
}
