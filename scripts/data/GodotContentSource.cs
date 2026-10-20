using Godot;
using MistyInn.Core.Content;

namespace MistyInn.Data;

/// <summary>Reads content files from <c>res://data/</c> (DAT-02). The Windows export includes them (D-036).</summary>
public sealed class GodotContentSource : IContentSource
{
    public const string Folder = "res://data/";

    public string ReadText(string fileName)
    {
        var path = Folder + fileName;
        if (!FileAccess.FileExists(path))
        {
            throw new System.IO.FileNotFoundException($"{path} does not exist.", fileName);
        }

        return FileAccess.GetFileAsString(path);
    }
}
