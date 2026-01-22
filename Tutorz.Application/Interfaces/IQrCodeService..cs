using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    public interface IQrCodeService
    {
        Task<string> GenerateUserQrCodeAsync(string userId, string name, string mobile, string role);
    }
}
