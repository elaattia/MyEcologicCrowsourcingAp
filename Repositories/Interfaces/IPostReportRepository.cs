using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Forum;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IPostReportRepository
    {
        Task<IEnumerable<PostReport>> GetAllAsync(ReportStatus? status = null, int pageNumber = 1, int pageSize = 50);
        Task<PostReport?> GetByIdAsync(Guid id);
        Task<IEnumerable<PostReport>> GetByPostAsync(Guid postId);
        Task<bool> HasUserReportedPostAsync(Guid userId, Guid postId);
        Task<PostReport> CreateAsync(PostReport report);
        Task<PostReport> UpdateAsync(PostReport report);
        Task<int> GetPendingCountAsync();
    }
}