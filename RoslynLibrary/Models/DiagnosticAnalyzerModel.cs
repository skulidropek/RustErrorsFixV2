using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynLibrary.Models
{
    public class DiagnosticAnalyzerModel
    {
        public string DiagnosticId { get; set; }
        public string Title { get; set; }
        public string MessageFormat { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string RegexPattern { get; set; }

        public KeyValuePair<string, string> AnalyzeSource { get; set; }
    }
}
