using Devken.CBC.SchoolManagement.Application.Services.Interfaces.Images;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Images
{
    /// <summary>
    /// Saves uploaded images to  wwwroot/uploads/{subFolder}  and returns
    /// a root-relative URL that can be stored in the database and served
    /// directly by ASP.NET Core's static-file middleware.
    /// </summary>
    public class ImageUploadService : IImageUploadService
    {
        // Only these MIME types are accepted.
        private static readonly string[] AllowedMimeTypes =
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "image/webp"
        };

        // Hard upper-bound (5 MB).
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ImageUploadService> _logger;

        public ImageUploadService(
            IWebHostEnvironment env,
            ILogger<ImageUploadService> logger)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<string> UploadImageAsync(IFormFile file, string subFolder = "general")
        {
            // ── Validation ────────────────────────────────────────────────────
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was provided or the file is empty.", nameof(file));

            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException(
                    $"File size {file.Length / 1024} KB exceeds the maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB.");

            if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                throw new InvalidOperationException(
                    $"File type '{file.ContentType}' is not allowed. " +
                    $"Accepted types: {string.Join(", ", AllowedMimeTypes)}");

            // ── Build physical path ───────────────────────────────────────────
            // e.g.  {wwwroot}/uploads/students/
            var safeSubFolder = Path.GetFileName(subFolder); // prevent path traversal
            var uploadFolder = Path.Combine(
                _env.WebRootPath, "uploads", safeSubFolder);

            Directory.CreateDirectory(uploadFolder); // no-op if already exists

            // Unique filename preserving original extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueName = $"{Guid.NewGuid()}{extension}";
            var physicalPath = Path.Combine(uploadFolder, uniqueName);

            // ── Write to disk ─────────────────────────────────────────────────
            await using var stream = new FileStream(
                physicalPath, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true);

            await file.CopyToAsync(stream);

            _logger.LogInformation(
                "Image uploaded: sub-folder={SubFolder}, file={FileName}, size={Size} bytes",
                safeSubFolder, uniqueName, file.Length);

            // ── Return URL ────────────────────────────────────────────────────
            // Root-relative URL served by static-file middleware.
            // e.g.  /uploads/students/abc123.jpg
            return $"/uploads/{safeSubFolder}/{uniqueName}";
        }

        /// <inheritdoc/>
        public Task<bool> DeleteImageAsync(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return Task.FromResult(false);

            try
            {
                // Convert  /uploads/students/abc.jpg  →  wwwroot/uploads/students/abc.jpg
                // Normalise separator and strip leading slash
                var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var physicalPath = Path.Combine(_env.WebRootPath, relativePath);

                // Security: ensure the resolved path is still inside wwwroot
                var fullPath = Path.GetFullPath(physicalPath);
                var webRoot = Path.GetFullPath(_env.WebRootPath);

                if (!fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Blocked attempt to delete file outside wwwroot: {Path}", relativeUrl);
                    return Task.FromResult(false);
                }

                if (!File.Exists(fullPath))
                    return Task.FromResult(false);

                File.Delete(fullPath);

                _logger.LogInformation("Image deleted: {RelativeUrl}", relativeUrl);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {RelativeUrl}", relativeUrl);
                return Task.FromResult(false);
            }
        }
    }
}