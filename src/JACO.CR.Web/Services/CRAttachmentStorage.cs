using Microsoft.AspNetCore.Http;

namespace JACO.CR.Web.Services;

public sealed class CRAttachmentStorage(IWebHostEnvironment env)
{
    private string Root =>
        Path.Combine(env.ContentRootPath, "App_Data", "CRAttachments");

    public async Task<(string storedFileName, string physicalPath)> SaveAsync(
        long crId,
        IFormFile file,
        CancellationToken ct = default)
    {
        var folder = Path.Combine(Root, crId.ToString());
        Directory.CreateDirectory(folder);

        var extension = Path.GetExtension(file.FileName);
        var stored = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(folder, stored);

        await using var stream = new FileStream(
            path, FileMode.CreateNew, FileAccess.Write, FileShare.None);

        await file.CopyToAsync(stream, ct);
        return (stored, path);
    }

    public string GetPath(long crId, string storedFileName) =>
        Path.Combine(Root, crId.ToString(), storedFileName);
}
