using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class UserSequenceRepository : IUserSequenceRepository
    {
        private readonly TutorzDbContext _context;

        public UserSequenceRepository(TutorzDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetNextSequenceNumberAsync(string prefixKey)
        {
            var sequence = await _context.UserSequences
                .FirstOrDefaultAsync(s => s.PrefixKey == prefixKey);

            if (sequence == null)
            {
                sequence = new UserSequence
                {
                    PrefixKey = prefixKey,
                    LastNumber = 0,
                    LastUpdated = DateTime.UtcNow
                };
                await _context.UserSequences.AddAsync(sequence);
            }

            // Increment
            sequence.LastNumber++;
            sequence.LastUpdated = DateTime.UtcNow;

            // Save changes
            await _context.SaveChangesAsync();

            return sequence.LastNumber;
        }
    }
}