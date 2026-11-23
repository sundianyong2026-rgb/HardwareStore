using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace HardwareStore.Services;

/// <summary>
/// Saves uploaded images (raw file or cropped data URL produced by cropper.js)
/// into wwwroot/uploads/&lt;folder&gt; and returns the public relative path.
/// Every disk failure is turned into a null result so that a missing or read-only
/// storage folder can never crash a request with a 500 page again.
/// </summary>
public class FileStorage
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorage> _logger;

    private static readonly string[] Allowed =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public FileStorage(IWebHostEnvironment env, ILogger<FileStorage> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Absolute web root. Falls back to content root when the host did not resolve
    /// wwwroot, which is what made Path.Combine throw before.
    /// </summary>
    private string WebRoot()
    {
        var root = _env.WebRootPath;

        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot");

        if (!Directory.Exists(root))
        {
            try { Directory.CreateDirectory(root); }
            catch (Exception ex) { _logger.LogError(ex, "Cannot create web root {Root}", root); }
        }

        return root;
    }

    public string SaveCroppedDataUrl(string dataUrl, string folder = "products")
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;

        var comma = dataUrl.IndexOf(',');
        if (comma < 0) return null;

        var meta = dataUrl.Substring(0, comma);
        if (!meta.Contains("base64")) return null;

        var payload = dataUrl.Substring(comma + 1);
        var ext = meta.Contains("png") ? ".png" : ".jpg";

        try
        {
            var bytes = Convert.FromBase64String(payload);
            if (bytes.Length == 0) return null;
            return WriteBytes(bytes, ext, folder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot decode cropped image");
            return null;
        }
    }

    public string SaveFile(IFormFile file, string folder = "products")
    {
        if (file == null || file.Length == 0) return null;

        var ext = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        if (!Allowed.Contains(ext)) return null;

        try
        {
            var dir = EnsureDir(folder);
            var name = Guid.NewGuid().ToString("N") + ext;
            using var stream = new FileStream(Path.Combine(dir, name), FileMode.Create, FileAccess.Write);
            file.CopyTo(stream);
            return $"/uploads/{SafeFolder(folder)}/{name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot save uploaded file {File}", file.FileName);
            return null;
        }
    }

    private string WriteBytes(byte[] bytes, string ext, string folder)
    {
        try
        {
            var dir = EnsureDir(folder);
            var name = Guid.NewGuid().ToString("N") + ext;
            File.WriteAllBytes(Path.Combine(dir, name), bytes);
            return $"/uploads/{SafeFolder(folder)}/{name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot write image bytes");
            return null;
        }
    }

    private static string SafeFolder(string folder)
    {
        var clean = (folder ?? "products").Trim().Trim('/', '\\');
        return string.IsNullOrWhiteSpace(clean) ? "products" : clean.Replace('\\', '/');
    }

    private string EnsureDir(string folder)
    {
        var dir = Path.Combine(WebRoot(), "uploads", SafeFolder(folder));

        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        return dir;
    }
}
