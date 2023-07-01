﻿using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StabilityMatrix.Models.FileInterfaces;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class FilePath : FileSystemPath, IPathObject
{
    private FileInfo? _info;
    // ReSharper disable once MemberCanBePrivate.Global
    public FileInfo Info => _info ??= new FileInfo(FullPath);

    public bool IsSymbolicLink
    {
        get
        {
            Info.Refresh();
            return Info.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
    }
    
    public bool Exists => Info.Exists;

    public FilePath(string path) : base(path)
    {
    }
    
    public FilePath(FileSystemPath path) : base(path)
    {
    }
    
    public FilePath(params string[] paths) : base(paths)
    {
    }

    public long GetSize()
    {
        Info.Refresh();
        return Info.Length;
    }
    
    public long GetSize(bool includeSymbolicLinks)
    {
        if (!includeSymbolicLinks && IsSymbolicLink) return 0;
        return GetSize();
    }

    public Task<long> GetSizeAsync(bool includeSymbolicLinks)
    {
        return Task.Run(() => GetSize(includeSymbolicLinks));
    }
    
    /// <summary> Creates an empty file. </summary>
    public void Create() => File.Create(FullPath).Close();
    
    /// <summary> Deletes the file </summary>
    public void Delete() => File.Delete(FullPath);
    
    // Methods specific to files
    
    /// <summary> Read text </summary>
    public string ReadAllText() => File.ReadAllText(FullPath);
    
    /// <summary> Read text asynchronously </summary>
    public Task<string> ReadAllTextAsync(CancellationToken ct = default)
    {
        return File.ReadAllTextAsync(FullPath, ct);
    }
    
    /// <summary> Read bytes </summary>
    public byte[] ReadAllBytes() => File.ReadAllBytes(FullPath);
    
    /// <summary> Read bytes asynchronously </summary>
    public Task<byte[]> ReadAllBytesAsync(CancellationToken ct = default)
    {
        return File.ReadAllBytesAsync(FullPath, ct);
    }
    
    /// <summary> Write bytes </summary>
    public void WriteAllBytes(byte[] bytes) => File.WriteAllBytes(FullPath, bytes);
    
    /// <summary> Write bytes asynchronously </summary>
    public Task WriteAllBytesAsync(byte[] bytes, CancellationToken ct = default)
    {
        return File.WriteAllBytesAsync(FullPath, bytes, ct);
    }

    // Implicit conversions to and from string
    public static implicit operator string(FilePath path) => path.FullPath;
    public static implicit operator FilePath(string path) => new(path);
}