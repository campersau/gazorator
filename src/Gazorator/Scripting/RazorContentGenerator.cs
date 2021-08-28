using Gazorator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Gazorator.Scripting
{
    internal abstract class RazorContentGeneratorBase<TRazorScriptHost>
    {
        private readonly IReadOnlyCollection<Assembly> _references;

        protected RazorContentGeneratorBase(IEnumerable<Assembly> references)
        {
            _references = new List<Assembly>(references ?? Enumerable.Empty<Assembly>());
        }

        public Func<TRazorScriptHost, Task> Generate(string csharpScript)
        {
            var options = ScriptOptions.Default
                .WithReferences(GetMetadataReferences())
                .WithImports("System")
                .WithMetadataResolver(ScriptMetadataResolver.Default);

            var roslynScript = CSharpScript.Create(csharpScript, options, typeof(TRazorScriptHost));

            var compilation = roslynScript.GetCompilation();
            var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);

            if (diagnostics.Any())
            {
                var errorMessages = string.Join(Environment.NewLine, diagnostics.Select(x => x.ToString()));
                throw new InvalidOperationException($"Error(s) occurred when compiling build script:{Environment.NewLine}{errorMessages}");
            }

            var @delegate = roslynScript.CreateDelegate();

            return globalsObject => @delegate(globalsObject);
        }

        protected virtual IEnumerable<MetadataReference> GetMetadataReferences()
        {
            yield return MetadataReference.CreateFromFile(typeof(Action).Assembly.Location); // mscorlib or System.Private.Core
            yield return MetadataReference.CreateFromFile(typeof(IQueryable).Assembly.Location); // System.Core or System.Linq.Expressions
            yield return MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location); // System
            yield return MetadataReference.CreateFromFile(typeof(System.Xml.XmlReader).Assembly.Location); // System.Xml
            yield return MetadataReference.CreateFromFile(typeof(System.Xml.Linq.XDocument).Assembly.Location); // System.Xml.Linq
            yield return MetadataReference.CreateFromFile(typeof(System.Data.DataTable).Assembly.Location); // System.Data
            yield return MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location); // dynamic

            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                if (entryAssembly.Location != null)
                {
                    yield return MetadataReference.CreateFromFile(entryAssembly.Location);
                }

                foreach (var reference in entryAssembly.GetReferencedAssemblies())
                {
                    var referencedAssembly = Assembly.Load(reference);
                    if (referencedAssembly.Location != null)
                    {
                        yield return MetadataReference.CreateFromFile(referencedAssembly.Location);
                    }
                }
            }

            foreach (var reference in _references.Where(r => r.Location != null))
            {
                yield return MetadataReference.CreateFromFile(reference.Location);
            }
        }
    }


    internal sealed class RazorContentGenerator<TRazorScriptHost> : RazorContentGeneratorBase<TRazorScriptHost>
    {
        public RazorContentGenerator(IEnumerable<Assembly> references) : base(references)
        {
        }
    }

    internal sealed class RazorContentGenerator<TRazorScriptHost, TModel> : RazorContentGeneratorBase<TRazorScriptHost>
    {
        private readonly bool _isDynamicAssembly;

        public RazorContentGenerator(IEnumerable<Assembly> references) : base(references)
        {
            if (typeof(TModel).IsNotPublic)
            {
                throw new ArgumentException($"{typeof(TModel).GetType().FullName} must be public.");
            }
            _isDynamicAssembly = typeof(TModel).IsDynamic();
        }

        protected override IEnumerable<MetadataReference> GetMetadataReferences()
        {
            return _isDynamicAssembly ?
                base.GetMetadataReferences() :
                base.GetMetadataReferences()
                    .Append(MetadataReference.CreateFromFile(typeof(TModel).Assembly.Location));
        }
    }
}
