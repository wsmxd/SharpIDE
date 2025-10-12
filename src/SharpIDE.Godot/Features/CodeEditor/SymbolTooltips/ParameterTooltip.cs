using Godot;
using Microsoft.CodeAnalysis;

namespace SharpIDE.Godot.Features.CodeEditor;

public static partial class SymbolInfoComponents
{
    public static Control GetParameterSymbolInfo(IParameterSymbol symbol)
    {
        var label = new RichTextLabel();
        label.FitContent = true;
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.PushColor(CachedColors.White);
        label.PushFont(MonospaceFont);
        label.AddAttributes(symbol);
        label.AddText("parameter ");
        label.AddAccessibilityModifier(symbol);
        label.AddStaticModifier(symbol);
        label.AddVirtualModifier(symbol);
        label.AddAbstractModifier(symbol);
        label.AddOverrideModifier(symbol);
        label.AddParameterTypeName(symbol);
        label.AddParameterName(symbol);
        label.AddContainingNamespaceAndClass(symbol);
        label.Newline();
        //label.AddTypeParameterArguments(symbol);
        label.Pop(); // font
        label.AddDocs(symbol);
        
        label.Pop();
        return label;
    }
    
    private static void AddParameterTypeName(this RichTextLabel label, IParameterSymbol symbol)
    {
        label.PushColor(GetSymbolColourByType(symbol.Type));
        label.AddText(symbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        label.Pop();
        label.AddText(" ");
    }
    
    private static void AddParameterName(this RichTextLabel label, IParameterSymbol symbol)
    {
        label.PushColor(CachedColors.VariableBlue);
        label.AddText(symbol.Name);
        label.Pop();
    }
}