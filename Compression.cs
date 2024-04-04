using System.IO.Compression;

namespace PreCompress;

internal static class Compression
{
    public static async Task CompressAsync(FileInfo file, bool bratli, bool gzip, DirectoryInfo? destination, CancellationToken token)
    {
        var tasks = new List<Task>();

        var dest = Path.Combine(destination?.FullName ?? Path.GetDirectoryName(file.FullName)!, Path.GetFileName(file.Name));

        if (bratli)
        {
            tasks.Add(CompressFileAsync(file.FullName, dest + ".br", BrotliCompressor, token));
        }

        if (gzip)
        {
            tasks.Add(CompressFileAsync(file.FullName, dest + ".gz", GZipCompressor, token));
        }

        await Task.WhenAll(tasks);
    }
    
    private static CompressionLevel GetCompressionLevel()
    {
        if (Enum.IsDefined(typeof(CompressionLevel), 3)) // NOTE: CompressionLevel.SmallestSize == 3 is not supported in .NET Core 3.1
        {
            return (CompressionLevel)3;
        }
        return CompressionLevel.Optimal;
    }

    private static async Task CompressFileAsync(string originalFileName, string compressedFileName, Func<Stream, Stream, CancellationToken, Task> compressor, CancellationToken cancel = default)
    {
        using (FileStream originalStream = File.Open(originalFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            using (FileStream compressedStream = File.Create(compressedFileName))
            {
                await compressor(originalStream, compressedStream, cancel);
            }
        }
    }

    private static async Task BrotliCompressor(Stream originalStream, Stream compressedStream, CancellationToken cancel = default)
    {
        using (var compressor = new BrotliStream(compressedStream, GetCompressionLevel()))
        {
            await originalStream.CopyToAsync(compressor, cancel);
        }
    }

    private static async Task GZipCompressor(Stream originalStream, Stream compressedStream, CancellationToken cancel = default)
    {
        using (var compressor = new GZipStream(compressedStream, GetCompressionLevel()))
        {
            await originalStream.CopyToAsync(compressor, cancel);
        }
    }
}
