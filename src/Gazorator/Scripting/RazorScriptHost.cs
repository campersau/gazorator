using Gazorator.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;

namespace Gazorator.Scripting
{
    // See https://github.com/aspnet/Common/blob/master/shared/Microsoft.Extensions.RazorViews.Sources/BaseView.cs
    public abstract class RazorScriptHostBase
    {
        public dynamic ViewBag { get; }

        public HtmlRenderer Html { get; }

        protected TextWriter Output { get; }

        protected virtual HtmlEncoder HtmlEncoder { get; } = HtmlEncoder.Default;

        public RazorScriptHostBase(TextWriter output, DynamicViewBag viewBag)
        {
            Output = output ?? throw new ArgumentNullException(nameof(output));
            Html = new HtmlRenderer();
            ViewBag = viewBag;
        }

        public virtual void WriteLiteral(object value)
        {
           WriteLiteral(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        public virtual void WriteLiteral(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Output.Write(value);
            }
        }

        public virtual void Write(string value)
        {
            if (value == null)
            {
                return;
            }

            WriteLiteral(HtmlEncoder.Encode(value));
        }

        public virtual void Write(object value)
        {
            if (value is IRazorLiteral element)
            {
                WriteLiteral(element.Render());
                return;
            }

            Write(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        private string AttributeEnding { get; set; }
        private List<string> AttributeValues { get; set; }

        public virtual void BeginWriteAttribute(string name, string prefix, int prefixOffset, string suffix, int suffixOffset, int attributeValuesCount)
        {
            Output.Write(prefix);
            AttributeEnding = suffix;
        }

        public void WriteAttributeValue(string prefix, int prefixOffset, object value, int valueOffset, int valueLength, bool isLiteral)
        {
            if (AttributeValues == null)
            {
                AttributeValues = new List<string>();
            }

            AttributeValues.Add(value.ToString());
        }

        public virtual void EndWriteAttribute()
        {
            var attributes = string.Join(" ", AttributeValues);
            Output.Write(attributes);
            AttributeValues = null;

            Output.Write(AttributeEnding);
            AttributeEnding = null;
        }
    }

    public class RazorScriptHost : RazorScriptHostBase
    {
        public RazorScriptHost(TextWriter output, DynamicViewBag viewBag) : base(output, viewBag)
        {
        }
    }

    public class RazorScriptHost<TModel> : RazorScriptHostBase
    {
        public RazorScriptHost(TextWriter output, TModel model, DynamicViewBag viewBag) : base(output, viewBag)
        {
            if (typeof(TModel).IsNullable() && model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            Model = model;
        }

        public TModel Model { get; }
    }

    public class RazorScriptHostDynamic : RazorScriptHostBase
    {
        private readonly dynamic _model;

        public RazorScriptHostDynamic(TextWriter output, dynamic model, DynamicViewBag viewBag) : base(output, viewBag)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public dynamic Model => _model;
    }
}
