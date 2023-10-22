using StackExchange.Redis;

namespace Redis;

/// <summary>
/// Simple extension methods.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Get the IP port combo for the specified multiplexer
    /// </summary>
    /// <param name="multiplexer">The <see cref="ConnectionMultiplexer"/></param>
    /// <returns>The IP port combo.</returns>
    public static string GetIpPortCombo(this ConnectionMultiplexer multiplexer) 
        => ParseIpPortCombo(multiplexer?.Configuration);

    // Token: 0x0600001A RID: 26 RVA: 0x00002774 File Offset: 0x00000974
    private static string ParseIpPortCombo(string configuration)
    {
        if (string.IsNullOrEmpty(configuration)) return string.Empty;

        var opts = ConfigurationOptions.Parse(configuration);
        if (opts.EndPoints.Count > 0)
            return opts.EndPoints[0].ToString();

        return string.Empty;
    }
}
