using System.Text.RegularExpressions;

namespace llcom.Pages;

/// <summary>
/// Encoding conversion utilities - cross-platform.
/// </summary>
public static class ConvertUtil
{
    /// <summary>Convert text between encodings.</summary>
    public static string ConvertEncoding(string input, int fromEncoding, int toEncoding)
    {
        try
        {
            var fromEnc = System.Text.Encoding.GetEncoding(fromEncoding);
            var toEnc = System.Text.Encoding.GetEncoding(toEncoding);
            var bytes = fromEnc.GetBytes(input);
            return toEnc.GetString(bytes);
        }
        catch
        {
            return input;
        }
    }

    /// <summary>Get all available encoding code pages.</summary>
    public static List<(int CodePage, string Name)> GetAvailableEncodings()
    {
        var encodings = new List<(int, string)>();
        foreach (var enc in System.Text.Encoding.GetEncodings())
        {
            encodings.Add((enc.CodePage, $"{enc.Name} ({enc.DisplayName})"));
        }
        return encodings;
    }

    /// <summary>Fix garbled text by trying common encodings.</summary>
    public static List<(string Encoding, string Result)> TryFixGarbled(string input, int targetEncoding = 65001)
    {
        var results = new List<(string, string)>();
        int[] commonEncodings = { 936, 950, 932, 949, 1252, 1251, 28591 };

        foreach (var cp in commonEncodings)
        {
            try
            {
                var result = ConvertEncoding(input, cp, targetEncoding);
                if (!string.IsNullOrEmpty(result) && result != input)
                    results.Add((System.Text.Encoding.GetEncoding(cp).EncodingName, result));
            }
            catch { }
        }
        return results;
    }
}
