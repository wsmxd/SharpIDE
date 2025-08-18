using System.Linq;
using Godot;
using Godot.Collections;
using System.Text.RegularExpressions;

namespace SharpIDE.Godot;

public partial class CustomHighlighter : SyntaxHighlighter
{
    public override Dictionary _GetLineSyntaxHighlighting(int line)
    {
        var highlights = new Dictionary();
        var text = GetTextEdit().GetLine(line);

        var regex = new Regex(@"\bTODO\b");
        var matches = regex.Matches(text);

        foreach (Match match in matches)
        {
            highlights[match.Index] = new Dictionary
            {
                { "color", new Color(1, 0, 0) }, // red
                { "underline", true }, // not implemented
                { "length", match.Length } // not implemented
            };
        }

        return highlights;
    }
}
