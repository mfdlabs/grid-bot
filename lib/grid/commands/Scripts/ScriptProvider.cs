namespace Grid.Commands;

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;

/// <summary>
/// Provides the ability to read embedded scripts.
/// </summary>
public static class ScriptProvider
{
    private static readonly Assembly _Assembly = Assembly.GetExecutingAssembly();
    private static readonly string[] _Scripts = _Assembly.GetManifestResourceNames();

    /// <summary>
    /// Gets the script with the specified name.
    /// </summary>
    /// <param name="name">The name of the script.</param>
    /// <returns>The script.</returns>
    /// <exception cref="ArgumentException">The name cannot be null or empty.</exception>
    /// <exception cref="InvalidOperationException">The script could not be found.</exception>
    public static string GetScript(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("The name cannot be null or empty.", nameof(name));

        var script = _Scripts.FirstOrDefault(s => s.EndsWith($"{name}.lua", StringComparison.OrdinalIgnoreCase)) 
                    ?? throw new InvalidOperationException("The script could not be found.");

        using var stream = _Assembly.GetManifestResourceStream(script);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Gets the script by the <see cref="ThumbnailCommandType"/>
    /// </summary>
    /// <param name="type">The <see cref="ThumbnailCommandType"/>.</param>
    /// <returns>The script.</returns>
    /// <exception cref="InvalidOperationException">The script could not be found.</exception>
    /// <exception cref="NotSupportedException">The type is not supported.</exception>
    public static string GetScript(ThumbnailCommandType type)
        => GetScript(type.ToString());
}
