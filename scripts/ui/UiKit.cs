using Godot;

namespace MistyInn.UI;

/// <summary>Small factories for the code-built placeholder UI (until the UI pass, step 9).</summary>
public static class UiKit
{
    public static Label AddLabel(Control parent, int fontSize, Color? color = null)
    {
        var label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 6);
        if (color is { } c)
        {
            label.AddThemeColorOverride("font_color", c);
        }

        parent.AddChild(label);
        return label;
    }

    public static Button AddButton(Control parent, string text)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 32) };
        button.AddThemeFontSizeOverride("font_size", 16);
        parent.AddChild(button);
        return button;
    }

    public static void Clear(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            child.QueueFree();
        }
    }
}
