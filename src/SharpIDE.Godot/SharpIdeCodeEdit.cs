using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;

namespace SharpIDE.Godot;

public partial class SharpIdeCodeEdit : CodeEdit
{
	[Signal]
	public delegate void CodeFixesRequestedEventHandler();

	private int _currentLine;
	private int _selectionStartCol;
	private int _selectionEndCol;
	
	private CustomHighlighter _syntaxHighlighter = new();

	private ImmutableArray<Diagnostic> _diagnostics = [];
	
	public override void _Ready()
	{
		//AddThemeFontOverride("Cascadia Code", ResourceLoader.Load<Font>("res://CascadiaCode.ttf"));
		this.CodeCompletionRequested += OnCodeCompletionRequested;
		this.CodeFixesRequested += OnCodeFixesRequested;
		this.CaretChanged += () =>
		{
			_selectionStartCol = GetSelectionFromColumn();
			_selectionEndCol = GetSelectionToColumn();
			_currentLine = GetCaretLine();
			GD.Print($"Selection changed to line {_currentLine}, start {_selectionStartCol}, end {_selectionEndCol}");
		};
		this.SyntaxHighlighter = _syntaxHighlighter;
	}
	
	public void UnderlineRange(int line, int caretStartCol, int caretEndCol, Color color, float thickness = 1.5f)
	{
		if (line < 0 || line >= GetLineCount())
			return;

		if (caretStartCol >= caretEndCol) // nothing to draw
			return;

		// Clamp columns to line length
		int lineLength = GetLine(line).Length;
		caretStartCol = Mathf.Clamp(caretStartCol, 0, lineLength);
		caretEndCol   = Mathf.Clamp(caretEndCol, 0, lineLength);
		
		// GetRectAtLineColumn returns the rectangle for the character before the column passed in, or the first character if the column is 0.
		var startRect = GetRectAtLineColumn(line, caretStartCol);
		var endRect = GetRectAtLineColumn(line, caretEndCol);
		//DrawLine(startRect.Position, startRect.End, color);
		//DrawLine(endRect.Position, endRect.End, color);
		
		var startPos = startRect.End;
		if (caretStartCol is 0)
		{
			startPos.X -= startRect.Size.X;
		}
		var endPos = endRect.End;
		startPos.Y -= 1;
		endPos.Y   -= 1;
		DrawLine(startPos, endPos, color, thickness);
	}
	public override void _Draw()
	{
		UnderlineRange(_currentLine, _selectionStartCol, _selectionEndCol, new Color(1, 0, 0));
		foreach (var diagnostic in _diagnostics)
		{
			if (diagnostic.Location.IsInSource)
			{
				var mappedLineSpan = (diagnostic.Location.SourceTree?.GetMappedLineSpan(diagnostic.Location.SourceSpan))!.Value;
				var line = mappedLineSpan.StartLinePosition.Line;
				var startCol = mappedLineSpan.StartLinePosition.Character;
				var endCol = mappedLineSpan.EndLinePosition.Character;
				var color = diagnostic.Severity switch
				{
					DiagnosticSeverity.Error => new Color(1, 0, 0),
					DiagnosticSeverity.Warning => new Color(1, 1, 0),
					_ => new Color(0, 1, 0) // Info or other
				};
				UnderlineRange(line, startCol, endCol, color);
			}
		}
	}

	public void ProvideDiagnostics(ImmutableArray<Diagnostic> diagnostics)
	{
		_diagnostics = diagnostics;
	}
	public void ProvideSyntaxHighlighting(IEnumerable<(FileLinePositionSpan fileSpan, ClassifiedSpan classifiedSpan)> classifiedSpans)
	{
		_syntaxHighlighter.ClassifiedSpans = classifiedSpans;
		Callable.From(() =>
		{
			_syntaxHighlighter.ClearHighlightingCache();
			//_syntaxHighlighter.UpdateCache();
			SyntaxHighlighter = null;
			SyntaxHighlighter = _syntaxHighlighter; // Reassign to trigger redraw
			GD.Print("Provided syntax highlighting");
		}).CallDeferred();
	}

	private void OnCodeFixesRequested()
	{
		GD.Print("Code fixes requested");
	}

	private void OnCodeCompletionRequested()
	{
		var caretColumn = GetCaretColumn();
		var caretLine = GetCaretLine();
		GD.Print($"Code completion requested at line {caretLine}, column {caretColumn}");
	}
}