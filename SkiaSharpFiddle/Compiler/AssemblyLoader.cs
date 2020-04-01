using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SkiaSharpFiddle
{
    public class AssemblyLoader
    {
        private static readonly string[] WhitelistedAssemblies =
        {
            "netstandard",
            "mscorlib",
            "System",
            "System.Core",
            "System.Private.CoreLib",
            "System.Runtime",
            "SkiaSharp",
            "SkiaSharp.Extended",
            "SkiaSharp.Extended.Svg",
            "System.Xml.ReaderWriter",
            "System.Private.Xml",
        };

        public AssemblyLoader()
        {
            AddExternalAssemblies();
        }

        public IEnumerable<Assembly> GetReferences() =>
            AppDomain.CurrentDomain
                     .GetAssemblies()
                     .Where(a => WhitelistedAssemblies.Any(wl => wl.Equals(a.GetName().Name, StringComparison.OrdinalIgnoreCase)));

        private void AddExternalAssemblies()
        {
            var skiasharpExtendedReference = SkiaSharp.Extended.SKGeometry.PI;
            var skiasharpSvgReference = new SkiaSharp.Extended.Svg.SKSvg();
        }
    }
}
