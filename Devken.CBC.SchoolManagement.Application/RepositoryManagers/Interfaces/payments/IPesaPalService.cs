using Devken.CBC.SchoolManagement.Application.DTOs.Payments;
using Devken.CBC.SchoolManagement.Application.DTOs.PesaPal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.payments
{
    public interface IPesaPalService
    {
        /// <summary>Obtain (or return cached) bearer token from PesaPal.</summary>
        Task<string> GetTokenAsync();

        /// <summary>Register (once per process) our IPN URL with PesaPal and return its id.</summary>
        Task<string> RegisterIpnAsync();

        /// <summary>Submit an order and return the hosted-checkout redirect URL + tracking id.</summary>
        Task<PesaPalOrderResponse> SubmitOrderAsync(SubmitOrderRequestDto dto);

        /// <summary>Query current transaction status by PesaPal order tracking id.</summary>
        Task<PesaPalStatusResponse> GetTransactionStatusAsync(string orderTrackingId);

        /// <summary>List all IPN endpoints registered under the current credentials.</summary>
        Task<IEnumerable<PesaPalIpnResponse>> GetRegisteredIpnsAsync();
    }


}
