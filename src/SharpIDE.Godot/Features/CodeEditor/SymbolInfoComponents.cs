using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace SharpIDE.Godot.Features.CodeEditor;

public static class SymbolInfoComponents
{
    private static readonly FontVariation MonospaceFont = ResourceLoader.Load<FontVariation>("uid://cctwlwcoycek7");
    public static RichTextLabel GetMethodSymbolInfo(IMethodSymbol methodSymbol)
    {
        var label = new RichTextLabel();
        label.FitContent = true;
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.PushColor(CachedColors.White);
        label.PushFont(MonospaceFont);
        label.PushColor(CachedColors.KeywordBlue);
        // TODO: Attributes
        label.AddText(methodSymbol.DeclaredAccessibility.GetAccessibilityString());
        label.Pop();
        label.AddText(" ");
        label.AddStaticModifier(methodSymbol);
        label.AddText(" ");
        label.AddMethodReturnType(methodSymbol);
        label.AddText(" ");
        label.AddMethodName(methodSymbol);
        label.AddTypeParameters(methodSymbol);
        label.AddText("(");
        label.AddParameters(methodSymbol);
        label.AddText(")");
        label.Newline();
        label.AddText("in class ");
        label.AddContainingNamespaceAndClass(methodSymbol);
        label.Newline(); // TODO: Make this only 1.5 lines high
        label.Newline(); //
        label.AddTypeParameterArguments(methodSymbol);
        label.AddHr(100, 1, CachedColors.Gray);
        label.Newline();
        label.Pop(); // font
        label.AddDocs(methodSymbol);
        label.Pop(); // default white
        return label;
    }
    
    private static string GetAccessibilityString(this Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Private => "private",
        Accessibility.Protected => "protected",
        Accessibility.Internal => "internal",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => "unknown"
    };
    
    private static void AddStaticModifier(this RichTextLabel label, IMethodSymbol methodSymbol)
    {
        if (methodSymbol.IsStatic || methodSymbol.ReducedFrom?.IsStatic is true)
        {
            label.PushColor(CachedColors.KeywordBlue);
            label.AddText("static");
            label.Pop();
        }
    }
    
    private static void AddMethodReturnType(this RichTextLabel label, IMethodSymbol methodSymbol)
    {
        if (methodSymbol.ReturnsVoid)
        {
            label.PushColor(CachedColors.KeywordBlue);
            label.AddText("void");
            label.Pop();
            return;
        }

        label.PushColor(CachedColors.ClassGreen);
        label.AddText(methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        label.Pop();
    }
    
    private static void AddMethodName(this RichTextLabel label, IMethodSymbol methodSymbol)
    {
        label.PushColor(CachedColors.Yellow);
        label.AddText(methodSymbol.Name);
        label.Pop();
    }
    
    private static void AddTypeParameters(this RichTextLabel label, IMethodSymbol methodSymbol)
    {
        if (methodSymbol.TypeParameters.Length == 0) return;
        label.PushColor(CachedColors.White);
        label.AddText("<");
        label.Pop();
        foreach (var (index, typeParameter) in methodSymbol.TypeParameters.Index())
        {
            label.PushColor(CachedColors.ClassGreen);
            label.AddText(typeParameter.Name);
            label.Pop();
            if (index < methodSymbol.TypeParameters.Length - 1)
            {
                label.AddText(", ");
            }
        }
        label.PushColor(CachedColors.White);
        label.AddText(">");
        label.Pop();
    }
    
    private static void AddParameters(this RichTextLabel label, IMethodSymbol methodSymbol)
    {
        if (methodSymbol.IsExtensionMethod)
        {
            label.PushColor(CachedColors.KeywordBlue);
            label.AddText("this");
            label.Pop();
            label.AddText(" ");
        }
        foreach (var (index, parameterSymbol) in methodSymbol.Parameters.Index())
        {
            var attributes = parameterSymbol.GetAttributes();
            if (attributes.Length is not 0)
            {
                foreach (var (attrIndex, attribute) in attributes.Index())
                {
                    label.AddText("[");
                    label.PushColor(CachedColors.ClassGreen);
                    var displayString = attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                    if (displayString?.EndsWith("Attribute") is true) displayString = displayString[..^9]; // remove last 9 chars
                    label.AddText(displayString ?? "unknown");
                    label.Pop();
                    label.AddText("]");
                    label.AddText(" ");
                }
            }
            if (parameterSymbol.RefKind != RefKind.None) // ref, in, out
            {
                label.PushColor(CachedColors.KeywordBlue);
                label.AddText(parameterSymbol.RefKind.ToString().ToLower());
                label.Pop();
                label.AddText(" ");
            }
            else if (parameterSymbol.IsParams)
            {
                label.PushColor(CachedColors.KeywordBlue);
                label.AddText("params");
                label.Pop();
                label.AddText(" ");
            }
            label.PushColor(parameterSymbol.Type.GetSymbolColourByType());
            label.AddText(parameterSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            label.Pop();
            label.AddText(" ");
            label.PushColor(CachedColors.VariableBlue);
            label.AddText(parameterSymbol.Name);
            label.Pop();
            // default value
            if (parameterSymbol.HasExplicitDefaultValue)
            {
                label.AddText(" = ");
                if (parameterSymbol.ExplicitDefaultValue is null)
                {
                    label.PushColor(CachedColors.KeywordBlue);
                    label.AddText("null");
                    label.Pop();
                }
                else if (parameterSymbol.Type.TypeKind == TypeKind.Enum)
                {
                    var explicitDefaultValue = parameterSymbol.ExplicitDefaultValue;
                    // Find the enum field with the same constant value
                    var enumMember = parameterSymbol.Type.GetMembers()
                        .OfType<IFieldSymbol>()
                        .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, explicitDefaultValue));

                    if (enumMember != null)
                    {
                        label.PushColor(CachedColors.InterfaceGreen);
                        label.AddText(parameterSymbol.Type.Name);
                        label.Pop();
                        label.PushColor(CachedColors.White);
                        label.AddText(".");
                        label.Pop();
                        label.PushColor(CachedColors.White);
                        label.AddText(enumMember.Name);
                        label.Pop();
                    }
                    else
                    {
                        label.PushColor(CachedColors.InterfaceGreen);
                        label.AddText(parameterSymbol.Type.Name);
                        label.Pop();
                        label.AddText($"({explicitDefaultValue})");
                    }
                }
                else if (parameterSymbol.ExplicitDefaultValue is string str)
                {
                    label.PushColor(CachedColors.LightOrangeBrown);
                    label.AddText($"""
                                   "{str}"
                                   """);
                    label.Pop();
                }
                else if (parameterSymbol.ExplicitDefaultValue is bool b)
                {
                    label.PushColor(CachedColors.KeywordBlue);
                    label.AddText(b ? "true" : "false");
                    label.Pop();
                }
                else
                {
                    label.AddText(parameterSymbol.ExplicitDefaultValue.ToString() ?? "unknown");
                }
            }

            if (index < methodSymbol.Parameters.Length - 1)
            {
                label.AddText(", ");
            }
        }
    }
    
    private static void AddContainingNamespaceAndClass(this RichTextLabel label, IMethodSymbol methodSymbol)
    {
        if (methodSymbol.ContainingNamespace is null || methodSymbol.ContainingNamespace.IsGlobalNamespace) return;
        var namespaces = methodSymbol.ContainingNamespace.ToDisplayString().Split('.');
        label.PushMeta("TODO", RichTextLabel.MetaUnderline.OnHover);
        foreach (var ns in namespaces)
        {
            label.PushColor(CachedColors.KeywordBlue);
            label.AddText(ns);
            label.Pop();
            label.AddText(".");
        }
        label.PushColor(CachedColors.ClassGreen);
        label.AddText(methodSymbol.ContainingType.Name);
        label.Pop();
        label.Pop(); // meta
    }
    
    private static void AddTypeParameterArguments(this RichTextLabel label, IMethodSymbol methodSymbol)
    {
        if (methodSymbol.TypeArguments.Length == 0) return;
        var typeParameters = methodSymbol.TypeParameters;
        var typeArguments = methodSymbol.TypeArguments;
        if (typeParameters.Length != typeArguments.Length) throw new Exception("Type parameters and type arguments length mismatch.");
        foreach (var (index, (typeArgument, typeParameter)) in methodSymbol.TypeArguments.Zip(typeParameters).Index())
        {
            label.PushColor(CachedColors.ClassGreen);
            label.AddText(typeParameter.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            label.Pop();
            label.AddText(" is ");
            label.PushColor(typeArgument.GetSymbolColourByType());
            label.AddText(typeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            label.Pop();
            if (index < methodSymbol.TypeArguments.Length - 1)
            {
                label.Newline();
            }
        }
    }

    private static void AddDocs(this RichTextLabel label, IMethodSymbol methodSymbol)
    {
        var xmlDocs = methodSymbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xmlDocs)) return;
        var docComment = DocumentationComment.FromXmlFragment(xmlDocs);
        if (docComment.SummaryText is not null)
        {
            label.AddText(docComment.SummaryText);
            label.Newline();
        }
        label.PushTable(2);
        if (docComment.ParameterNames.Length is not 0)
        {
            label.PushCell();
            label.PushColor(CachedColors.Gray);
            label.AddText("Params: ");
            label.Pop();
            label.Pop();
            foreach (var (index, parameterName) in docComment.ParameterNames.Index())
            {
                var parameterText = docComment.GetParameterText(parameterName);
                if (parameterText is null) continue;
                label.PushCell();
                label.PushColor(CachedColors.VariableBlue);
                label.AddText(parameterName);
                label.Pop();
                label.AddText(" - ");
                label.AddText(parameterText);
                label.Pop(); // cell
                if (index < docComment.ParameterNames.Length - 1)
                {
                    label.PushCell();
                    label.Pop();
                }
            }
        }

        if (docComment.TypeParameterNames.Length is not 0)
        {
            label.PushCell();
            label.PushColor(CachedColors.Gray);
            label.AddText("Type Params: ");
            label.Pop();
            label.Pop();
            foreach (var (index, typeParameterName) in docComment.TypeParameterNames.Index())
            {
                var typeParameterText = docComment.GetTypeParameterText(typeParameterName);
                if (typeParameterText is null) continue;
                label.PushCell();
                label.PushColor(CachedColors.ClassGreen);
                label.AddText(typeParameterName);
                label.Pop();
                label.AddText(" - ");
                label.AddText(typeParameterText);
                label.Pop(); // cell
                if (index < docComment.TypeParameterNames.Length - 1)
                {
                    label.PushCell();
                    label.Pop();
                }
            }
        }
        if (docComment.ReturnsText is not null)
        {
            label.PushCell();
                label.PushColor(CachedColors.Gray);
                    label.AddText("Returns: ");
                label.Pop();
            label.Pop();
            label.PushCell();
                label.AddText(docComment.ReturnsText);
            label.Pop(); // cell
        }

        if (docComment.ExceptionTypes.Length is not 0)
        {
            label.PushCell();
                label.PushColor(CachedColors.Gray);
                    label.AddText("Exceptions: ");
                label.Pop();
            label.Pop();
            foreach (var (index, exceptionTypeName) in docComment.ExceptionTypes.Index())
            {
                var exceptionText = docComment.GetExceptionTexts(exceptionTypeName).FirstOrDefault();
                if (exceptionText is null) continue;
                label.PushCell();
                label.PushColor(CachedColors.InterfaceGreen);
                label.AddText(exceptionTypeName);
                label.Pop();
                label.AddText(" - ");
                label.AddText(exceptionText);
                label.Pop(); // cell
                if (index < docComment.ExceptionTypes.Length - 1)
                {
                    label.PushCell();
                    label.Pop();
                }
            }
        }

        if (docComment.RemarksText is not null)
        {
            label.PushCell();
                label.PushColor(CachedColors.Gray);
                    label.AddText("Remarks: ");
                label.Pop();
            label.Pop();
            label.PushCell();
            label.AddText(docComment.RemarksText);
            label.Pop(); // cell
            label.PushCell();
            label.Pop();
        }
        
        label.Pop(); // table
    }
    
    // TODO: handle arrays etc, where there are multiple colours in one type
    private static Color GetSymbolColourByType(this ITypeSymbol symbol)
    {
        Color colour = symbol switch
        {
            {SpecialType: not SpecialType.None} => CachedColors.KeywordBlue,
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.TypeKind switch
            {
                TypeKind.Class => CachedColors.ClassGreen,
                TypeKind.Interface => CachedColors.InterfaceGreen,
                TypeKind.Struct => CachedColors.ClassGreen,
                TypeKind.Enum => CachedColors.InterfaceGreen,
                TypeKind.Delegate => CachedColors.ClassGreen,
                _ => CachedColors.Orange
            },
            _ => CachedColors.Orange
        };
        return colour;
    }
}