using Godot;

namespace SharpIDE.Godot;

public partial class SharpIdeCodeEdit : CodeEdit
{
	[Signal]
	public delegate void CodeFixesRequestedEventHandler();

	[Export]
	public int HighlightStartOffset = 0;
	
	[Export]
	public int HighlightEndOffset = 0;

	private int _currentLine;
	private int _selectionStartCol;
	private int _selectionEndCol;
	
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
		this.SyntaxHighlighter = new CustomHighlighter();
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

		var charRect = GetRectAtLineColumn(line, caretEndCol);
		var charWidth = charRect.Size.X;
		
		var startPos = GetPosAtLineColumn(line, caretStartCol);
		if (caretStartCol is 0)
		{
			startPos.X -= 9; // Seems to be a bug or intended "feature" of GetPosAtLineColumn
		}
		var endPos   = GetPosAtLineColumn(line, caretEndCol);
		startPos.X += charWidth;
		endPos.X   += charWidth;
		startPos.Y -= 1;
		endPos.Y   -= 1;
		DrawLine(startPos, endPos, color, thickness);
	}
	public override void _Draw()
	{
		UnderlineRange(_currentLine, _selectionStartCol, _selectionEndCol, new Color(1, 0, 0));
		//UnderlineRange(_currentLine, 0, 7, new Color(1, 0, 0));
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