using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Interfaces
{
    public interface IHallRepository : IGenericRepository<Hall>
    {
        // Add specific methods if needed, currently inheriting generic CRUD
    }
}
