using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;
using PreCompress;

var fileOption = new Option<FileInfo[]?>(
    name: "-i", 
    description: "The file to compress.",
    isDefault: true,
    parseArgument: result =>
    {
        if (result.Tokens.Count == 0)
        {
            result.ErrorMessage = "No input sepcified";
            return null;
        }

        char[] wildcards = [ '*', '?' ] ;

        var res = result.Tokens
            .Select(x => (pattern: x.Value, dir: EmptyCoalesce(Path.GetDirectoryName(x.Value), Directory.GetCurrentDirectory())!))
            .SelectMany(x =>
            x switch
            {
                _ when Directory.Exists(x.pattern) => 
                    new DirectoryInfo(x.pattern).GetFiles(),
                _ when x.pattern.Intersect(wildcards).Any() && Directory.Exists(x.dir) => 
                    Directory.GetFiles(x.dir, Path.GetFileName(x.pattern)).Select(f => new FileInfo(f)),
                _ when File.Exists(x.pattern) => 
                    [ new FileInfo(x.pattern) ],
                _ => 
                    []
            }
        ).ToArray();

        if(res.Length == 0)
        {
            result.ErrorMessage = "No files found";
            return null;
        }

        return res;
    })
{
    AllowMultipleArgumentsPerToken = true,
    IsRequired = true,
};
fileOption.AddAlias("--input");

var bratliOption = new Option<bool>(name: "-nb", description: "Omit Brotli compressed version.", getDefaultValue: () => false);
bratliOption.AddAlias("--NoBrotli");

var gzipOption = new Option<bool>(name: "-ng", description: "Omit GZip compressed version.", getDefaultValue: () => false);
gzipOption.AddAlias("--NoGzip");

var patternOption = new Option<string?>(
    name: "-p",
    description: "Use regular expression pattern to filter files",
    parseArgument: result => {
        var res = result.Tokens.FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(res)) return "*.";

        if(!IsValidRegexPattern(res))
        {
            result.ErrorMessage = "Invalid pattern specified";
            return null;
        }

        return res;
    }
    );
patternOption.AddAlias("--pattern");

var destinationOption = new Option<DirectoryInfo?>(name: "-d", description: "The output directory.");
destinationOption.AddAlias("--destination");

var rootCommand = new RootCommand("PreCompression tool for ASP.NET Core");
rootCommand.AddOption(fileOption);
rootCommand.AddOption(bratliOption);
rootCommand.AddOption(gzipOption);
rootCommand.AddOption(patternOption);
rootCommand.AddOption(destinationOption);
rootCommand.SetHandler(ReCompress, fileOption, bratliOption, gzipOption, patternOption, destinationOption);

rootCommand.AddValidator(result =>
{
    if (result.GetValueForOption(destinationOption) is not null && result.GetValueForOption(fileOption)?.Length > 0)
    { 
        result.ErrorMessage = "Destination can only be specified when a single file is being processed";
    }
});

return await rootCommand.InvokeAsync(args);

static async Task ReCompress(FileInfo[]? files, bool omitBratli, bool omitGZip, string? pattern, DirectoryInfo? destination)
{
    if(files is null) return;

    var opts = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled;

#if NET8_0_OR_GREATER
    opts |= RegexOptions.NonBacktracking;
#endif

    Predicate<string> match = 
        string.IsNullOrWhiteSpace(pattern) ? 
            _ => true : 
            new Regex(pattern, opts).IsMatch;

    var list = files.Where(f => match(f.FullName));

    var tasks = list.Select(async file =>
    {
        try
        {
            Console.WriteLine($"Compressing {file.FullName}");
            await Compression.CompressAsync(file, !omitBratli, !omitGZip, destination, default);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing {file.FullName}: {ex.Message}");
        }
    });

    await Task.WhenAll(tasks);
    Console.WriteLine($"Compression finished");
}

static string? EmptyCoalesce(params string?[] values) => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

static bool IsValidRegexPattern(string pattern, string testText = "", int maxMillisecondTimeOut = 10)
{
    if (string.IsNullOrEmpty(pattern)) return false;
    try 
    {
        Regex re = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(maxMillisecondTimeOut));
        re.IsMatch(testText); 
    }
    catch 
    { 
        return false; 
    } 

    return true;
}