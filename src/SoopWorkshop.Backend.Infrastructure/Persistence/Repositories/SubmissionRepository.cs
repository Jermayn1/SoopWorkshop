using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Application.Repositories.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Repositories
{
    public class SubmissionRepository : ISubmissionRepository
    {
        private readonly AppDbContext _context;

        public SubmissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Submission?> GetByIdAsync(Guid id)
        {
            return await _context.Submissions
                .Include(s => s.Files)
                .Include(s => s.Task)
                    .ThenInclude(t => t.Tests)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Submission>> GetByTaskIdAsync(Guid taskId)
        {
            return await _context.Submissions
                .Where(s => s.TaskItemId == taskId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Submission submission)
        {
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Submission submission)
        {
            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();
        }
    }
}