using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace GodotResourceGroups;

public class PathVerifier {
    private readonly List<Regex> _includeRegexes = [];
    private readonly List<Regex> _excludeRegexes = [];

    public PathVerifier(string baseFolder, IEnumerable<string> includePatterns, IEnumerable<string> excludePatterns) {
        // Compile the include and exclude patterns to regular expressions, so we don't have to do it for each file.
        foreach (var pattern in includePatterns) {
            if (string.IsNullOrEmpty(pattern)) {
                continue;
            }

            _includeRegexes.Add(CompilePattern(baseFolder, pattern));
        }

        foreach (var pattern in excludePatterns) {
            if (string.IsNullOrEmpty(pattern)) {
                continue;
            }

            _excludeRegexes.Add(CompilePattern(baseFolder, pattern));
        }
    }

    /// <summary>
    /// Compiles the given pattern to a regular expression.
    /// </summary>
    private Regex CompilePattern(string baseFolder, string pattern) {
        // ** - matches zero or more characters (including "/")
        // * - matches zero or more characters (excluding "/")
        // ? - matches one character

        // We convert the pattern to a regular expression
        // ** becomes .*
        // * becomes [^/]* (any number of characters except /)
        // ? becomes [^/] (any character except /)
        // All other characters are escaped
        // The pattern is anchored at the beginning and end of the string
        // The pattern is case-sensitive
        var regex = new StringBuilder("^");
        regex.Append(Regex.Escape(baseFolder));

        // Fix for #21, only append trailing slash if the incoming path doesn't already have one.
        if (!baseFolder.EndsWith('/') && !baseFolder.EndsWith('\\')) {
            regex.Append('/');
        }

        var i = 0;
        var len = pattern.Length;

        while (i < len) {
            var c = pattern[i];
            if (c == '*') {
                if (i + 1 < len && pattern[i + 1] == '*') {
                    // ** - matches zero or more characters (including "/")
                    regex.Append(".*");
                    i += 2;
                } else {
                    // * - matches zero or more characters (excluding "/")
                    regex.Append("[^\\/]*");
                    i += 1;
                }
            } else if (c == '?') {
                // ? - matches one character
                regex.Append("[^\\/]");
                i += 1;
            } else {
                // Escape all other characters.
                regex.Append(EscapeCharacter(c));
                i += 1;
            }
        }

        regex.Append('$');
        return new(regex.ToString());
    }

    /// <summary>
    /// Escapes the given character for use in a regular expression.
    /// No clue why this is not built-in.
    /// </summary>
    private string EscapeCharacter(char c) {
        return c switch {
            '\\' => "\\\\",
            '^' => "\\^",
            '$' => "\\$",
            '.' => "\\.",
            '|' => "\\|",
            '?' => "\\?",
            '*' => "\\*",
            '+' => "\\+",
            '(' => "\\(",
            ')' => "\\)",
            '{' => "\\{",
            '}' => "\\}",
            '[' => "\\[",
            ']' => "\\]",
            '/' => "\\/",
            _ => c.ToString(),
        };
    }

    public bool Matches(string file) {
        // The group definition has a list of include and exclude patterns if the list of include patterns is empty,
        // all files match any file that matches an exclude pattern is excluded.
        // We allow * as a wildcard for a single path segment.
        // We allow ** as a wildcard for multiple path segments.
        if (_includeRegexes.Count > 0) {
            // The file must match at least one include pattern.
            if (!_includeRegexes.Any(item => item.IsMatch(file))) {
                if (file.Contains(".txt")) {
                    GD.Print($"File {file} did not match any regex");
                }

                return false;
            }
        }

        // The file must not match any exclude pattern.
        if (_excludeRegexes.Any(item => item.IsMatch(file))) {
            if (file.Contains(".txt")) {
                GD.Print($"File {file} was excluded");
            }

            return false;
        }

        return true;
    }
}
