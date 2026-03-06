using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Email
{
    /// <summary>
    /// Minimal Mustache-style template engine for {{ Property }} substitution.
    /// Used by IEmailService.SendTemplateAsync when you want to keep templates
    /// in files rather than in code (optional — EmailTemplates.cs is preferred).
    /// </summary>
    public static class EmailTemplateEngine
    {
        public static string Render<TModel>(string templateName, TModel model)
        {
            // Serialize model to a flat dictionary for substitution
            var json = JsonSerializer.Serialize(model,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json)
                       ?? new();

            // Read template from embedded resources or a fixed path
            var template = _LoadTemplate(templateName);

            // Replace {{ Key }} placeholders
            return Regex.Replace(template, @"\{\{\s*(\w+)\s*\}\}", m =>
            {
                var key = m.Groups[1].Value;
                return dict.TryGetValue(key, out var val)
                    ? System.Net.WebUtility.HtmlEncode(val?.ToString() ?? string.Empty)
                    : m.Value;
            });
        }

        private static string _LoadTemplate(string name)
        {
            // Convention: templates live in Infrastructure/EmailTemplates/<name>.html
            var path = System.IO.Path.Combine(
                AppContext.BaseDirectory, "EmailTemplates", $"{name}.html");

            return System.IO.File.Exists(path)
                ? System.IO.File.ReadAllText(path)
                : $"<p>Template '{name}' not found.</p>";
        }
    }
}
