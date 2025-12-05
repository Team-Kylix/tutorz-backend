using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    public interface IUserSequenceRepository
    {
        // This method will handle the database logic to find, increment, and save the number
        Task<int> GetNextSequenceNumberAsync(string prefixKey);
    }
}
