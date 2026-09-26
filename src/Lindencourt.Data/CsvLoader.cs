using System.Globalization;
using Lindencourt.Core.Models;

namespace Lindencourt.Data;

/// <summary>
/// CSV loader supporting two formats:
///   - MT4 export:   Date, Time, Open, High, Low, Close, Volume (Date and Time in separate columns)
///   - forexsb.com:  Time, Open, High, Low, Close, Volume (Time combined)
///
/// Separator is auto-detected (comma, semicolon, tab, space).
/// </summary>
public static class CsvLoader
{
    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "yyyy.MM.dd", "yyyy/MM/dd",
        "yyyy-MM-dd HH:mm:ss", "yyyy.MM.dd HH:mm:ss",
        "yyyy-MM-dd HH:mm", "yyyy.MM.dd HH:mm"
    };

    private static readonly string[] TimeFormats =
    {
        "HH:mm:ss", "HH:mm"
    };

    private static readonly char[] PossibleSeparators = { ',', ';', '\t', ' ' };

    public static List<Bar> Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"CSV file not found: {path}");

        var lines = File.ReadLines(path).ToList();
        int firstDataLine = FindFirstDataLine(lines);
        if (firstDataLine < 0)
            throw new InvalidDataException($"No data found in {path}");

        char separator = DetectSeparator(lines[firstDataLine]);
        Console.WriteLine($"Detected separator: '{GetSeparatorName(separator)}'");

        // Detect format: MT4 (Date+Time separate) or forexsb (Time combined)
        bool mt4Format = IsMt4Format(lines[firstDataLine], separator);
        Console.WriteLine($"Format: {(mt4Format ? "MT4 (Date, Time separate)" : "forexsb (Time combined)")}");

        var bars = new List<Bar>();
        for (int i = firstDataLine; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var bar = mt4Format
                ? ParseMt4Line(line, i + 1, separator)
                : ParseForexsbLine(line, i + 1, separator);

            if (bar != null) bars.Add(bar);
        }

        bars.Sort((a, b) => a.Time.CompareTo(b.Time));
        return bars;
    }

    /// <summary>MT4 format: column 0 is date (yyyy.MM.dd), column 1 is time (HH:mm).</summary>
    private static bool IsMt4Format(string line, char separator)
    {
        var parts = line.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 6) return false;

        return DateTime.TryParseExact(parts[0], new[] { "yyyy.MM.dd", "yyyy-MM-dd", "yyyy/MM/dd" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            && TimeSpan.TryParseExact(parts[1], new[] { "hh\\:mm", "hh\\:mm\\:ss" },
            CultureInfo.InvariantCulture, out _);
    }

    private static Bar? ParseMt4Line(string line, int lineNumber, char separator)
    {
        var parts = line.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 6)
            throw new FormatException($"Line {lineNumber}: expected at least 6 columns, got {parts.Length}");

        // parts[0] = Date, parts[1] = Time
        if (!DateTime.TryParseExact(parts[0], new[] { "yyyy.MM.dd", "yyyy-MM-dd", "yyyy/MM/dd" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw new FormatException($"Line {lineNumber}: cannot parse date '{parts[0]}'");

        if (!TimeSpan.TryParseExact(parts[1], new[] { "hh\\:mm", "hh\\:mm\\:ss" },
            CultureInfo.InvariantCulture, out var time))
            throw new FormatException($"Line {lineNumber}: cannot parse time '{parts[1]}'");

        var dateTime = date + time;

        double open   = ParseDouble(parts[2], lineNumber, "Open");
        double high   = ParseDouble(parts[3], lineNumber, "High");
        double low    = ParseDouble(parts[4], lineNumber, "Low");
        double close  = ParseDouble(parts[5], lineNumber, "Close");
        double volume = parts.Length > 6 ? ParseDouble(parts[6], lineNumber, "Volume") : 0;

        return new Bar(dateTime, open, high, low, close, volume);
    }

    private static Bar? ParseForexsbLine(string line, int lineNumber, char separator)
    {
        var parts = line.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 5)
            throw new FormatException($"Line {lineNumber}: expected at least 5 columns, got {parts.Length}");

        if (!TryParseTime(parts[0], out var time))
            throw new FormatException($"Line {lineNumber}: cannot parse time '{parts[0]}'");

        double open   = ParseDouble(parts[1], lineNumber, "Open");
        double high   = ParseDouble(parts[2], lineNumber, "High");
        double low    = ParseDouble(parts[3], lineNumber, "Low");
        double close  = ParseDouble(parts[4], lineNumber, "Close");
        double volume = parts.Length > 5 ? ParseDouble(parts[5], lineNumber, "Volume") : 0;

        return new Bar(time, open, high, low, close, volume);
    }

    /// <summary>Skips header and comment lines; returns the index of the first data row.</summary>
    private static int FindFirstDataLine(List<string> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Skip header rows (contain "Time" and "Open")
            if (line.Contains("Time", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("Open", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip rows that do not start with a digit
            if (!char.IsDigit(line.TrimStart()[0]))
                continue;

            return i;
        }
        return -1;
    }

    /// <summary>Auto-detects the column separator by checking which one yields >= 5 fields.</summary>
    private static char DetectSeparator(string line)
    {
        foreach (var sep in new[] { ',', '\t', ';', ' ' })
        {
            var parts = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 5) return sep;
        }
        return ',';
    }

    private static string GetSeparatorName(char c) => c switch
    {
        ',' => "comma",
        ';' => "semicolon",
        '\t' => "tab",
        ' ' => "space",
        _ => c.ToString()
    };

    private static bool TryParseTime(string s, out DateTime time)
    {
        return DateTime.TryParseExact(s, DateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None, out time);
    }

    private static double ParseDouble(string s, int lineNumber, string field)
    {
        if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new FormatException($"Line {lineNumber}: cannot parse {field} '{s}'");
        return value;
    }
}