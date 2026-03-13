using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Domain.Entities.Payments
{
    /// <summary>
    /// Bound from the "PesaPal" section of appsettings.json / environment vars.
    /// Injected via IOptions&lt;PesaPalSettings&gt; (read) and
    /// IWritablePesaPalSettings (write through the settings UI).
    /// </summary>
    public sealed class PesaPalSettings
    {
        public const string SectionName = "PesaPal";

        // ── PesaPal API credentials ───────────────────────────────────
        public string ConsumerKey { get; set; } = string.Empty;
        public string ConsumerSecret { get; set; } = string.Empty;

        // ── Endpoint URLs ─────────────────────────────────────────────
        /// <summary>
        /// Sandbox:    https://cybqa.pesapal.com/pesapalv3
        /// Production: https://pay.pesapal.com/v3
        /// </summary>
        public string BaseUrl { get; set; } = "https://cybqa.pesapal.com/pesapalv3";

        /// <summary>
        /// Server-to-server notification URL. Must be publicly reachable
        /// (i.e. not localhost) in production. Maps to GET /api/pesapal/ipn.
        /// </summary>
        public string IpnUrl { get; set; } = string.Empty;

        /// <summary>
        /// Browser redirect URL after PesaPal checkout.
        /// Maps to GET /api/pesapal/callback on the Angular app.
        /// </summary>
        public string CallbackUrl { get; set; } = string.Empty;

        // ── Runtime state ─────────────────────────────────────────────
        /// <summary>
        /// Populated by PesaPalService after a successful IPN registration.
        /// Persisted via IWritablePesaPalSettings so the process-level cache
        /// survives restarts when backed by a persistent settings store.
        /// </summary>
        public string? RegisteredIpnId { get; set; }
    }

}
