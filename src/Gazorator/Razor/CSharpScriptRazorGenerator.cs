using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Gazorator.Razor
{
    internal static class CSharpScriptRazorGenerator
    {
        public static string Generate(string filePath)
        {
            var directoryRoot = Path.GetDirectoryName(filePath);

            var fileSystem = RazorProjectFileSystem.Create(directoryRoot);
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, builder =>
            {
                // Register directives.
                SectionDirective.Register(builder);

                // We replace the default document classifier, because we can't have namespace declaration ins script.
                var defaultDocumentClassifier = builder.Features
                    .OfType<IRazorDocumentClassifierPass>()
                    .FirstOrDefault(x => x.Order == 1000);
                builder.Features.Remove(defaultDocumentClassifier);
                builder.Features.Add(new CSharpScriptDocumentClassifierPass());
            });

            var razorItem = projectEngine.FileSystem.GetItem(filePath);
            var codeDocument = projectEngine.Process(razorItem);
            var csharpDocument = codeDocument.GetCSharpDocument();

            if (csharpDocument.Diagnostics.Any())
            {
                var diagnostics = string.Join(Environment.NewLine, csharpDocument.Diagnostics);
                throw new InvalidOperationException($"One or more parse errors encountered. This will not prevent the generator from continuing: {Environment.NewLine}{diagnostics}.");
            }

            return csharpDocument.GeneratedCode;
        }
    }
}
