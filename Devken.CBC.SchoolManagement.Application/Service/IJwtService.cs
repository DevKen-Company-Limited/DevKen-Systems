using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Service
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateSuperAdminAccessToken(SuperAdmin admin);
        string GenerateRefreshTokenString();
    }
}
