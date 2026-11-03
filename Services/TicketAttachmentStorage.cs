using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace WebApplication1.Services;

public sealed record StoredAttachment(string FileName, string StoredFileName, string ContentType, long FileSize);

public sealed class TicketAttachmentValidationException(string message) : Exception(message);

/// <summary>Stores untrusted uploads outside wwwroot under a server-generated name.</summary>
public sealed class TicketAttachmentStorage
{
    public const long MaximumFileSize = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".txt"] = "text/plain",
            [".log"] = "text/plain",
            [".csv"] = "text/csv",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

    private readonly string _storageDirectory;

    public TicketAttachmentStorage(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["FileStorage:AttachmentsPath"] ?? "App_Data/attachments";
        _storageDirectory = Path.GetFullPath(Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath));
    }

    public async Task<StoredAttachment> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
            throw new TicketAttachmentValidationException("The selected file is empty.");
        if (file.Length > MaximumFileSize)
            throw new TicketAttachmentValidationException("Files must be 10 MB or smaller.");

        var originalName = Path.GetFileName(file.FileName.Replace('\\', '/')).Trim();
        originalName = new string(originalName.Where(c => !char.IsControl(c)).ToArray());
        if (string.IsNullOrWhiteSpace(originalName) || originalName.Length > 255)
            throw new TicketAttachmentValidationException("Use a file name of 255 characters or fewer.");

        var extension = Path.GetExtension(originalName);
        if (!AllowedTypes.TryGetValue(extension, out var contentType))
            throw new TicketAttachmentValidationException(
                "Allowed files: PDF, PNG, JPG, GIF, WebP, TXT, LOG, CSV, DOC, DOCX, and XLSX.");

        await ValidateFileSignatureAsync(file, extension, cancellationToken);

        Directory.CreateDirectory(_storageDirectory);
        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = ResolveStoredPath(storedName);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await file.CopyToAsync(output, cancellationToken);
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }

        return new StoredAttachment(originalName, storedName, contentType, file.Length);
    }

    public Stream? OpenRead(string storedName)
    {
        if (!IsStoredNameValid(storedName)) return null;
        var path = ResolveStoredPath(storedName);
        return File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
    }

    public Task DeleteAsync(string storedName)
    {
        if (IsStoredNameValid(storedName))
        {
            var path = ResolveStoredPath(storedName);
            if (File.Exists(path)) File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private string ResolveStoredPath(string storedName)
    {
        var path = Path.GetFullPath(Path.Combine(_storageDirectory, storedName));
        var prefix = _storageDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? _storageDirectory
            : _storageDirectory + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Attachment path escaped the configured storage directory.");
        return path;
    }

    private static bool IsStoredNameValid(string storedName)
    {
        var extension = Path.GetExtension(storedName);
        return Guid.TryParseExact(Path.GetFileNameWithoutExtension(storedName), "N", out _) &&
               AllowedTypes.ContainsKey(extension);
    }

    private static async Task ValidateFileSignatureAsync(IFormFile file, string extension,
        CancellationToken cancellationToken)
    {
        var bytes = new byte[16];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(bytes.AsMemory(), cancellationToken);
        var valid = extension.ToLowerInvariant() switch
        {
            ".pdf" => StartsWith(bytes, read, "%PDF-"u8),
            ".png" => StartsWith(bytes, read, new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
            ".jpg" or ".jpeg" => StartsWith(bytes, read, new byte[] { 0xff, 0xd8, 0xff }),
            ".gif" => StartsWith(bytes, read, "GIF87a"u8) || StartsWith(bytes, read, "GIF89a"u8),
            ".webp" => read >= 12 && StartsWith(bytes, read, "RIFF"u8) && bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P',
            ".doc" => StartsWith(bytes, read, new byte[] { 0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1 }),
            ".docx" or ".xlsx" => StartsWith(bytes, read, new byte[] { 0x50, 0x4b, 0x03, 0x04 }),
            ".txt" or ".log" or ".csv" => !bytes.AsSpan(0, read).Contains((byte)0),
            _ => false
        };

        if (!valid)
            throw new TicketAttachmentValidationException("The file contents do not match the selected file type.");
    }

    private static bool StartsWith(byte[] value, int length, ReadOnlySpan<byte> prefix) =>
        length >= prefix.Length && value.AsSpan(0, prefix.Length).SequenceEqual(prefix);
}
