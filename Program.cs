using System.CommandLine;
using System.CommandLine.Parsing;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
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

var destinationOption = new Option<DirectoryInfo?>(name: "-d", description: "The output directory.");
destinationOption.AddAlias("--destination");

var rootCommand = new RootCommand("PreCompression tool for ASP.NET Core");
rootCommand.AddOption(fileOption);
rootCommand.AddOption(bratliOption);
rootCommand.AddOption(gzipOption);
rootCommand.AddOption(destinationOption);
rootCommand.SetHandler(ReCompress, fileOption, bratliOption, gzipOption, destinationOption);

return await rootCommand.InvokeAsync(args);

static async Task ReCompress(FileInfo[]? files, bool omitBratli, bool omitGZip, DirectoryInfo? destination)
{
    if(files is null) return;

    foreach (var file in files)
    {
        try
        {
            Console.WriteLine($"Processing {file.FullName}");
            await Compression.CompressAsync(file, !omitBratli, !omitGZip, destination, default);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing {file.FullName}: {ex.Message}");
        }
        
    }
}

static string? EmptyCoalesce(params string?[] values) => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));