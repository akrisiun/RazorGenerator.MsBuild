using System;
using System.CodeDom;
using System.Diagnostics;
using System.Collections.Generic;

namespace RazorGenerator.Core
{
    public abstract class AggregateCodeTransformer : RazorCodeTransformerBase
    {

        public Exception LastError { get; set; }

        protected abstract IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get;
        }

        public override void Initialize(RazorHost razorHost, IDictionary<string, string> directives)
        {
            foreach (var transformer in CodeTransformers)
            {
                try
                {

                    transformer.Initialize(razorHost, directives);
                }
                catch (Exception ex) { 
                    LastError = ex;
                    if (Debugger.IsAttached)
                    {
                        Debugger.Log(0, "AggregateCodeTransformer", Environment.NewLine + ex.Message);
                        if (ex.InnerException != null)
                            Debugger.Log(0, "AggregateCodeTransformer", Environment.NewLine + ex.InnerException.Message);
                    }
                }
            }
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            foreach (var transformer in CodeTransformers)
            {
                transformer.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);
            }
        }

        public override string ProcessOutput(string codeContent)
        {
            foreach (var transformer in CodeTransformers)
            {
                codeContent = transformer.ProcessOutput(codeContent);
            }
            return codeContent;
        }
    }
}
