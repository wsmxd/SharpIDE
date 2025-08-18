using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;

namespace SharpIDE.Godot;

public partial class CustomHighlighter : SyntaxHighlighter
{
    public IEnumerable<(FileLinePositionSpan fileSpan, ClassifiedSpan classifiedSpan)> ClassifiedSpans = [];
    public override Dictionary _GetLineSyntaxHighlighting(int line)
    {
        var highlights = MapClassifiedSpansToHighlights(line);

        return highlights;
    }
    
    private static readonly StringName ColorStringName = "color";
    private Dictionary MapClassifiedSpansToHighlights(int line)
    {
        var highlights = new Dictionary();
        
        // consider no linq or ZLinq
        var spansGroupedByFileSpan = ClassifiedSpans
            .Where(s => s.fileSpan.StartLinePosition.Line == line && s.classifiedSpan.TextSpan.Length is not 0)
            .GroupBy(span => span.fileSpan)
            .Select(group => (fileSpan: group.Key, classifiedSpans: group.Select(s => s.classifiedSpan).ToList()));

        foreach (var (fileSpan, classifiedSpans) in spansGroupedByFileSpan)
        {
            if (classifiedSpans.Count > 2) throw new NotImplementedException("More than 2 classified spans is not supported yet.");
            if (classifiedSpans.Count is not 1)
            {
                ClassifiedSpan? staticClassifiedSpan = classifiedSpans.FirstOrDefault(s => s.ClassificationType == ClassificationTypeNames.StaticSymbol);
                if (staticClassifiedSpan is not null) classifiedSpans.Remove(staticClassifiedSpan.Value);
            }
            // Column index of the first character in this span
            int columnIndex = fileSpan.StartLinePosition.Character;

            // Build the highlight entry
            var highlightInfo = new Dictionary
            {
                { ColorStringName, GetColorForClassification(classifiedSpans.Single().ClassificationType) }
            };

            highlights[columnIndex] = highlightInfo;
        }

        return highlights;
    }
    
    private static Color GetColorForClassification(string classificationType)
    {
        return classificationType switch
        {
            // Keywords
            "keyword" => new Color("569cd6"),
            "keyword - control" => new Color("569cd6"),
            "preprocessor keyword" => new Color("569cd6"),

            // Literals & comments
            "string" => new Color("d69d85"),
            "comment" => new Color("57a64a"),
            "number" => new Color("b5cea8"),

            // Types (User Types)
            "class name" => new Color("4ec9b0"),
            "struct name" => new Color("4ec9b0"),
            "interface name" => new Color("b8d7a3"),
            "namespace name" => new Color("dcdcdc"),
            
            // Identifiers & members
            "identifier" => new Color("dcdcdc"),
            "method name" => new Color("dcdcaa"),
            "extension method name" => new Color("dcdcaa"),
            "property name" => new Color("dcdcdc"),
            "static symbol" => new Color("dcdcaa"),
            "parameter name" => new Color("9cdcfe"),
            "local name" => new Color("9cdcfe"),

            // Punctuation & operators
            "operator" => new Color("dcdcdc"),
            "punctuation" => new Color("dcdcdc"),

            // Misc
            "excluded code" => new Color("a9a9a9"),

            _ => new Color("dcdcdc")
        };
    }
}
