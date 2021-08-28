using Gazorator.Scripting;
using System.IO;

namespace Gazorator.Console
{
    public class CustomRazorScriptHost : RazorScriptHost
    {
        public CustomRazorScriptHost(TextWriter output, DynamicViewBag viewBag) : base(output, viewBag)
        {
        }

        public string PageTitle =>  "Hello World!";
    }
}
