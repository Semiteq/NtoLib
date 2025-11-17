using System;
using System.IO;
using System.Linq;

using BuildScripts.Infrastructure;

using Nuke.Common.IO;

using Serilog;

namespace BuildScripts.Services;

public sealed class DeploymentService
{
    private readonly BuildContext _ctx;

    public DeploymentService(BuildContext ctx)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
    }

    public bool AreFilesUpToDate()
    {
        var destDir = _ctx.DestinationDirectory;
        var sourceDir = _ctx.TargetDirectory;

        if (!destDir.DirectoryExists())
            return false;

        foreach (var name in _ctx.FilesToDeploy)
        {
            var src = sourceDir / name;
            var dst = destDir / name;

            if (!src.FileExists())
                continue;

            if (!dst.FileExists())
            {
                Log.Debug("Destination missing file: {File}", name);
                return false;
            }

            if (File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(dst))
            {
                Log.Debug("File is newer in source: {File}", name);
                return false;
            }
        }

        var cfgSrc = _ctx.NtoLibTableConfigSource;
        var cfgDst = destDir / "NtoLibTableConfig";

        if (cfgSrc.DirectoryExists())
        {
            if (!cfgDst.DirectoryExists())
            {
                Log.Debug("Destination missing config directory.");
                return false;
            }

            var srcFiles = cfgSrc.GlobFiles("**/*").ToArray();
            foreach (var sf in srcFiles)
            {
                var rel = Path.GetRelativePath(cfgSrc, sf);
                var df = cfgDst / rel;
                if (!df.FileExists() || File.GetLastWriteTimeUtc(sf) > File.GetLastWriteTimeUtc(df))
                {
                    Log.Debug("Config file requires update: {File}", rel);
                    return false;
                }
            }
        }

        Log.Information("Destination is up to date.");
        return true;
    }

    public void CopyToLocal()
    {
        var dest = _ctx.DestinationDirectory;
        var source = _ctx.TargetDirectory;

        dest.CreateDirectory();

        var copied = 0;
        foreach (var name in _ctx.FilesToDeploy)
        {
            var srcPath = source / name;
            if (!srcPath.FileExists())
            {
                Log.Warning("File not found: {File}", name);
                continue;
            }

            File.Copy(srcPath, dest / name, overwrite: true);
            var fi = new FileInfo(srcPath);
            Log.Debug("Copied: {File} ({Size} KB)", name, fi.Length / 1024);
            copied++;
        }

        Log.Information("Copied {Count} files", copied);

        if (_ctx.NtoLibTableConfigSource.DirectoryExists())
        {
            var cfgDest = dest / "NtoLibTableConfig";
            if (cfgDest.DirectoryExists())
                cfgDest.DeleteDirectory();

            CopyDirectoryRecursively(_ctx.NtoLibTableConfigSource, cfgDest);
            Log.Information("Copied NtoLibTableConfig folder");
        }
        else
        {
            Log.Warning("Config folder not found: {Path}", _ctx.NtoLibTableConfigSource);
        }
    }

    private static void CopyDirectoryRecursively(AbsolutePath source, AbsolutePath destination)
    {
        destination.CreateDirectory();

        foreach (var file in Directory.GetFiles(source))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, destination / fileName, overwrite: true);
            Log.Debug("Copied file: {FileName}", fileName);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            var dirName = Path.GetFileName(dir);
            CopyDirectoryRecursively(dir, destination / dirName);
        }
    }
}