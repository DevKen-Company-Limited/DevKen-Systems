using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.payments;
using Devken.CBC.SchoolManagement.Domain.Entities.Payments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Infrastructure.Services.Finance
{
    /// <summary>
    /// Persists PesaPal settings back to appsettings.json on disk.
    /// Thread-safe via a per-instance SemaphoreSlim.
    /// </summary>
    public sealed class JsonWritablePesaPalSettings : IWritablePesaPalSettings
    {
        private readonly IOptionsMonitor<PesaPalSettings> _monitor;
        private readonly string _settingsPath;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private static readonly JsonSerializerOptions _writeOptions = new()
        {
            WriteIndented = true,
        };

        // ── IWebHostEnvironment replaces the raw string parameter ────────────
        // DI knows how to resolve IWebHostEnvironment; it does NOT know how to
        // inject a plain System.String, which caused:
        //   "Unable to resolve service for type 'System.String'"
        public JsonWritablePesaPalSettings(
            IOptionsMonitor<PesaPalSettings> monitor,
            IWebHostEnvironment env)
        {
            _monitor = monitor;

            // Resolve the path from the host's content root (where appsettings.json lives)
            _settingsPath = Path.Combine(env.ContentRootPath, "appsettings.json");

            if (!File.Exists(_settingsPath))
                throw new FileNotFoundException(
                    $"appsettings.json not found at expected path: {_settingsPath}");
        }

        public async Task UpdateAsync(Action<PesaPalSettings> update)
        {
            await _lock.WaitAsync();
            try
            {
                // 1. Apply mutations to an in-memory copy of current settings.
                var current = _monitor.CurrentValue;
                var working = new PesaPalSettings
                {
                    ConsumerKey = current.ConsumerKey,
                    ConsumerSecret = current.ConsumerSecret,
                    BaseUrl = current.BaseUrl,
                    IpnUrl = current.IpnUrl,
                    CallbackUrl = current.CallbackUrl,
                    RegisteredIpnId = current.RegisteredIpnId,
                };
                update(working);

                // 2. Read the full JSON file and patch only the PesaPal section.
                var json = await File.ReadAllTextAsync(_settingsPath);
                var root = JsonNode.Parse(json)?.AsObject()
                           ?? throw new InvalidOperationException("Invalid appsettings.json");

                var section = root[PesaPalSettings.SectionName]?.AsObject()
                              ?? new JsonObject();

                section[nameof(PesaPalSettings.ConsumerKey)] = working.ConsumerKey;
                section[nameof(PesaPalSettings.ConsumerSecret)] = working.ConsumerSecret;
                section[nameof(PesaPalSettings.BaseUrl)] = working.BaseUrl;
                section[nameof(PesaPalSettings.IpnUrl)] = working.IpnUrl;
                section[nameof(PesaPalSettings.CallbackUrl)] = working.CallbackUrl;
                section[nameof(PesaPalSettings.RegisteredIpnId)] = working.RegisteredIpnId;

                root[PesaPalSettings.SectionName] = section;

                // 3. Write back (atomic temp-file swap to avoid corruption).
                var tmp = _settingsPath + ".tmp";
                await File.WriteAllTextAsync(tmp, root.ToJsonString(_writeOptions));
                File.Move(tmp, _settingsPath, overwrite: true);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}