using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Services.Interfaces.Images
{
    public interface IImageUploadService
    {
        /// <summary>
        /// Saves an uploaded image file to wwwroot/uploads and returns the relative URL.
        /// </summary>
        /// <param name="file">The uploaded image file.</param>
        /// <param name="subFolder">Optional sub-folder inside uploads (e.g. "students", "teachers").</param>
        /// <returns>Relative URL string, e.g. "/uploads/students/abc123.jpg"</returns>
        Task<string> UploadImageAsync(IFormFile file, string subFolder = "general");

        /// <summary>
        /// Deletes a previously uploaded image given its relative URL.
        /// Returns true if the file was found and deleted.
        /// </summary>
        Task<bool> DeleteImageAsync(string relativeUrl);
    }
}