using Gazorator.Extensions;
using Gazorator.Razor;
using Gazorator.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Gazorator
{
    public abstract class Gazorator
    {
        public static GazoratorFactory<TRazorScriptHost> Compile<TRazorScriptHost>(string filePath, IEnumerable<Assembly> references = null) where TRazorScriptHost : RazorScriptHost
        {
            var csharpScript = CSharpScriptRazorGenerator.Generate(filePath);

            var razorContentGenerator = new RazorContentGenerator<TRazorScriptHost>(references);
            var factory = razorContentGenerator.Generate(csharpScript);

            return new GazoratorFactory<TRazorScriptHost>(factory);
        }

        public static GazoratorFactory<TRazorScriptHost> CompileTemplate<TRazorScriptHost>(string template, IEnumerable<Assembly> references = null) where TRazorScriptHost : RazorScriptHost
        {
            return WriteTemplateFile(template, tempFile => Compile<TRazorScriptHost>(tempFile, references));
        }

        public static GazoratorFactory Compile(string filePath, IEnumerable<Assembly> references = null)
        {
            var csharpScript = CSharpScriptRazorGenerator.Generate(filePath);

            var razorContentGenerator = new RazorContentGenerator<RazorScriptHost>(references);
            var factory = razorContentGenerator.Generate(csharpScript);

            return new GazoratorFactory(factory);
        }

        public static GazoratorFactory CompileTemplate(string template, IEnumerable<Assembly> references = null)
        {
            return WriteTemplateFile(template, tempFile => Compile(tempFile, references));
        }

        public static GazoratorFactory<TRazorScriptHost> CompileModel<TRazorScriptHost, TModel>(string filePath, IEnumerable<Assembly> references = null) where TRazorScriptHost : RazorScriptHost<TModel>
        {
            if (typeof(TModel).IsDynamic())
            {
                throw new InvalidOperationException("The Model is dynamic please use CompileDynamicModel instead.");
            }

            var csharpScript = CSharpScriptRazorGenerator.Generate(filePath);

            var razorContentGenerator = new RazorContentGenerator<TRazorScriptHost, TModel>(references);
            var factory = razorContentGenerator.Generate(csharpScript);

            return new GazoratorFactory<TRazorScriptHost>(factory);
        }

        public static GazoratorFactory<TRazorScriptHost> CompileModelTemplate<TRazorScriptHost, TModel>(string template, IEnumerable<Assembly> references = null) where TRazorScriptHost : RazorScriptHost<TModel>
        {
            return WriteTemplateFile(template, tempFile => CompileModel<TRazorScriptHost, TModel>(tempFile, references));
        }

        public static GazoratorModelFactory<TModel> CompileModel<TModel>(string filePath, IEnumerable<Assembly> references = null)
        {
            if (typeof(TModel).IsDynamic())
            {
                throw new InvalidOperationException("The Model is dynamic please use CompileDynamicModel instead.");
            }

            var csharpScript = CSharpScriptRazorGenerator.Generate(filePath);

            var razorContentGenerator = new RazorContentGenerator<RazorScriptHost<TModel>, TModel>(references);
            var factory = razorContentGenerator.Generate(csharpScript);

            return new GazoratorModelFactory<TModel>(factory);
        }

        public static GazoratorModelFactory<TModel> CompileModelTemplate<TModel>(string template, IEnumerable<Assembly> references = null)
        {
            return WriteTemplateFile(template, tempFile => CompileModel<TModel>(tempFile, references));
        }

        public static GazoratorFactory<TRazorScriptHost> CompileDynamicModel<TRazorScriptHost, TModel>(string filePath, IEnumerable<Assembly> references = null) where TRazorScriptHost : RazorScriptHostDynamic
        {
            var csharpScript = CSharpScriptRazorGenerator.Generate(filePath);

            var razorContentGenerator = new RazorContentGenerator<TRazorScriptHost, TModel>(references);
            var factory = razorContentGenerator.Generate(csharpScript);

            return new GazoratorFactory<TRazorScriptHost>(factory);
        }

        public static GazoratorFactory<TRazorScriptHost> CompileDynamicModelTemplate<TRazorScriptHost, TModel>(string template, IEnumerable<Assembly> references = null) where TRazorScriptHost : RazorScriptHostDynamic
        {
            return WriteTemplateFile(template, tempFile => CompileDynamicModel<TRazorScriptHost, TModel>(tempFile, references));
        }

        public static GazoratorDynamicModelFactory<TModel> CompileDynamicModel<TModel>(string filePath, IEnumerable<Assembly> references = null)
        {
            var csharpScript = CSharpScriptRazorGenerator.Generate(filePath);

            var razorContentGenerator = new RazorContentGenerator<RazorScriptHostDynamic, TModel>(references);
            var factory = razorContentGenerator.Generate(csharpScript);

            return new GazoratorDynamicModelFactory<TModel>(factory);
        }

        public static GazoratorDynamicModelFactory<TModel> CompileDynamicModelTemplate<TModel>(string template, IEnumerable<Assembly> references = null)
        {
            return WriteTemplateFile(template, tempFile => CompileDynamicModel<TModel>(tempFile, references));
        }

        private static T WriteTemplateFile<T>(string template, Func<string, T> func)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, template);

                return func(tempFile);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }


    public class GazoratorFactory<TRazorScriptHost> where TRazorScriptHost : RazorScriptHostBase
    {
        private readonly Func<TRazorScriptHost, Task> _factory;

        internal GazoratorFactory(Func<TRazorScriptHost, Task> factory)
        {
            _factory = factory;
        }

        public virtual Task ProcessAsync(TRazorScriptHost razorScriptHost)
        {
            return _factory(razorScriptHost);
        }
    }

    public sealed class GazoratorFactory : GazoratorFactory<RazorScriptHost>
    {
        internal GazoratorFactory(Func<RazorScriptHost, Task> factory) : base(factory)
        {
        }

        public Task ProcessAsync(TextWriter output, Action<dynamic> configureViewBag = null)
        {
            var viewBag = new DynamicViewBag();
            configureViewBag?.Invoke(viewBag);

            return ProcessAsync(new RazorScriptHost(output, viewBag));
        }
    }

    public sealed class GazoratorModelFactory<TModel> : GazoratorFactory<RazorScriptHost<TModel>>
    {
        internal GazoratorModelFactory(Func<RazorScriptHost<TModel>, Task> factory) : base(factory)
        {
        }

        public Task ProcessAsync(TextWriter output, TModel model, Action<dynamic> configureViewBag = null)
        {
            var viewBag = new DynamicViewBag();
            configureViewBag?.Invoke(viewBag);

            return ProcessAsync(new RazorScriptHost<TModel>(output, model, viewBag));
        }
    }

    public sealed class GazoratorDynamicModelFactory<TModel> : GazoratorFactory<RazorScriptHostDynamic>
    {
        internal GazoratorDynamicModelFactory(Func<RazorScriptHostDynamic, Task> factory) : base(factory)
        {
        }

        public Task ProcessAsync(TextWriter output, TModel model, Action<dynamic> configureViewBag = null)
        {
            var viewBag = new DynamicViewBag();
            configureViewBag?.Invoke(viewBag);

            return ProcessAsync(new RazorScriptHostDynamic(output, model, viewBag));
        }
    }
}
