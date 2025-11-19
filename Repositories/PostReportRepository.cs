using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Data;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;

namespace MyEcologicCrowsourcingApp.Repositories
{
    public class PostReportRepository : IPostReportRepository
    {
        private readonly EcologicDbContext _context;

        public PostReportRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PostReport>> GetAllAsync(ReportStatus? status = null, int pageNumber = 1, int pageSize = 50)
        {
            var query = _context.PostReports
                .Include(r => r.Post)
                .Include(r => r.ReportedBy)
                .Include(r => r.ReviewedBy)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<PostReport?> GetByIdAsync(Guid id)
        {
            return await _context.PostReports
                .Include(r => r.Post)
                .Include(r => r.ReportedBy)
                .Include(r => r.ReviewedBy)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<PostReport>> GetByPostAsync(Guid postId)
        {
            return await _context.PostReports
                .Include(r => r.ReportedBy)
                .Include(r => r.ReviewedBy)
                .Where(r => r.PostId == postId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasUserReportedPostAsync(Guid userId, Guid postId)
        {
            return await _context.PostReports
                .AnyAsync(r => r.ReportedByUserId == userId && r.PostId == postId);
        }

        public async Task<PostReport> CreateAsync(PostReport report)
        {
            _context.PostReports.Add(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<PostReport> UpdateAsync(PostReport report)
        {
            _context.PostReports.Update(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.PostReports
                .CountAsync(r => r.Status == ReportStatus.Pending);
        }
    }
}