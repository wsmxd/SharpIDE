using Godot;
using Microsoft.CodeAnalysis;

namespace SharpIDE.Godot.Features.CodeEditor;

public partial class SharpIdeCodeEdit
{
    private readonly Texture2D _csharpMethodIcon = ResourceLoader.Load<Texture2D>("uid://b17p18ijhvsep");
    private readonly Texture2D _csharpClassIcon = ResourceLoader.Load<Texture2D>("uid://b027uufaewitj");
    private readonly Texture2D _csharpInterfaceIcon = ResourceLoader.Load<Texture2D>("uid://bdwmkdweqvowt");
    private readonly Texture2D _localVariableIcon = ResourceLoader.Load<Texture2D>("uid://vwvkxlnvqqk3");

    private Texture2D? GetIconForCompletion(SymbolKind? symbolKind, TypeKind? typeKind, Accessibility? accessibility)
    {
        var texture = (symbolKind, typeKind, accessibility) switch
        {
            (SymbolKind.Method, _, _) => _csharpMethodIcon,
            (_, TypeKind.Interface, _) => _csharpInterfaceIcon,
            (SymbolKind.NamedType, _, _) => _csharpClassIcon,
            (SymbolKind.Local, _, _) => _localVariableIcon,
            //SymbolKind.Property => ,
            //SymbolKind.Field => ,
            _ => null
        };    
        return texture;
    }
}