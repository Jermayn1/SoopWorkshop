using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Application.Repositories.Interfaces;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Repositories
{
    public class TaskItemRepository : ITaskItemRepository
    {
        private readonly AppDbContext _context;

        public TaskItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskItem>> GetAllAsync()
        {
            return await _context.TaskItems
                .Include(t => t.Hints)
                .OrderBy(t => t.Order)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetVisibleByCategoryAsync(Guid categoryId)
        {
            return await _context.TaskItems
                .Include(t => t.Hints)
                .Where(t => t.TaskCategoryId == categoryId && t.IsVisible)
                .OrderBy(t => t.Order)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetByIdAsync(Guid id)
        {
            return await _context.TaskItems
                .Include(t => t.Hints)
                .Include(t => t.Tests)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(TaskItem item)
        {
            _context.TaskItems.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TaskItem item)
        {
            _context.TaskItems.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var item = await _context.TaskItems.FindAsync(id);
            if (item is not null)
            {
                _context.TaskItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}