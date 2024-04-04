# PreCompress
Simple tool to crete pre-compressed version for static content files to be served by Kestler or IIS with ASP.NET Core.
The need arose from an issue with publishing, where the pre-compressed files had different (older) content ss the non-compressed versions. 
This tool is intended to be used as a post-publish step to create the compressed files.

## Usage
### Options:
```
-i, --input <i> (REQUIRED)  The file to compress. [] 
-nb, --NoBrotli             Omit Brotli compressed version. [default: False] 
-ng, --NoGzip               Omit GZip compressed version. [default: False]         
-d, --destination <d>       The output directory.
```

The input parameter can be multiple. Each can be a single file, a folder or a simple search patterm. If can be both absolute and relative, even mixed for the different parameter instances.

By default it will create both GZip and Brotli compressed versions of the file.

Destination directory should exist if specified.

### Examples:
```
PreCompress.exe -i C:\temp\source\*.dll -d c:\temp\destination
PreCompress.exe -i C:\temp\source
PreCompress.exe -i source\file.dll
PreCompress.exe -i *.dll
```