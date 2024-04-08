# PreCompress
Simple tool to create pre-compressed version for static content files to be served by Kestler or IIS with ASP.NET Core.
The need arose from an issue with publishing, where the pre-compressed files had different (older) content as the non-compressed versions. 
This tool is intended to be used as a post-publish step to create the compressed files.

## Usage
### Options:
```
-i, --input <i> (REQUIRED)  The file to compress. [] 
-nb, --NoBrotli             Omit Brotli compressed version. [default: False] 
-ng, --NoGzip               Omit GZip compressed version. [default: False]         
-p, --pattern <p>           The regex pattern to use for files in a folder. [default: *.]
-d, --destination <d>       The output directory.
```

The input parameter can be multiple. Each can be a single file, a folder or a simple search patterm. If can be both absolute and relative, even mixed for the different parameter instances.

By default it will create both GZip and Brotli compressed versions of the file.

Destination directory should exist if specified.

### Examples:
```
PreCompress -i C:\temp\source\*.dll -d c:\temp\destination
PreCompress -i C:\temp\source\*.dll -i C:\temp\source\*.json
PreCompress -i C:\temp\source\ -p \.(dll|js.*)$
PreCompress -i source\file.dll
PreCompress -i *.dll
```

## Installation
The tool is available as a dotnet global or local tool. To install it follow the instructions on nuget:
https://www.nuget.org/packages/zorgoz.PreCompress/

Install as global tool with

```
dotnet tool install --global zorgoz.PreCompress`
```

or as local tool with (be aware to install in the correct path)

```
dotnet new tool-manifest
dotnet tool install zorgoz.PreCompress
```


## Usage as a tool
After installing as tool, you can add this to you `.csproj` file as a post-publish step.
```
<Target Name="PreCompress" AfterTargets="Publish">
	<Exec Command="dotnet tool run precompress -i $(PublishDir)wwwroot\_framework\ -p &quot;\.(dll|js|json|wasm|blat)$&quot;"></Exec>
</Target>
```

## Planned features
- [ ] Add support for recursive folder search (currently only one level deep), first without destination folder
- [ ] Add support for recursivity with destination folder (copy unmatching parts of the folder structure)
