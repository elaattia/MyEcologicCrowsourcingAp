using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.DTOs.Forum;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Services
{
    public class ReportService : IReportService
    {
        private readonly IPostReportRepository _reportRepo;
        private readonly IPostRepository _postRepo;

        public ReportService(IPostReportRepository reportRepo, IPostRepository postRepo)
        {
            _reportRepo = reportRepo;
            _postRepo = postRepo;
        }

        public async Task<ReportDto> ReportPostAsync(CreateReportDto dto, Guid userId)
        {
            if (!await _postRepo.ExistsAsync(dto.PostId))
                throw new KeyNotFoundException("Post not found");

            if (await _reportRepo.HasUserReportedPostAsync(userId, dto.PostId))
                throw new InvalidOperationException("You have already reported this post");

            var report = new PostReport
            {
                Id = Guid.NewGuid(),
                PostId = dto.PostId,
                ReportedByUserId = userId,
                Reason = dto.Reason,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepo.CreateAsync(report);
            
            var created = await _reportRepo.GetByIdAsync(report.Id);
            return MapToDto(created!);
        }

        public async Task<IEnumerable<ReportDto>> GetAllReportsAsync(ReportStatus? status = null, int pageNumber = 1, int pageSize = 50)
        {
            var reports = await _reportRepo.GetAllAsync(status, pageNumber, pageSize);
            return reports.Select(MapToDto);
        }

        public async Task<ReportDto?> GetReportByIdAsync(Guid id)
        {
            var report = await _reportRepo.GetByIdAsync(id);
            return report == null ? null : MapToDto(report);
        }

        public async Task<ReportDto> ReviewReportAsync(Guid id, ReviewReportDto dto, Guid adminUserId)
        {
            var report = await _reportRepo.GetByIdAsync(id);
            if (report == null)
                throw new KeyNotFoundException("Report not found");

            report.Status = dto.Status;
            report.AdminNotes = dto.AdminNotes;
            report.ReviewedByUserId = adminUserId;
            report.ReviewedAt = DateTime.UtcNow;

            await _reportRepo.UpdateAsync(report);
            
            var updated = await _reportRepo.GetByIdAsync(id);
            return MapToDto(updated!);
        }

        public async Task<int> GetPendingReportCountAsync()
        {
            return await _reportRepo.GetPendingCountAsync();
        }

        private ReportDto MapToDto(PostReport report)
        {
            return new ReportDto
            {
                Id = report.Id,
                Reason = report.Reason,
                Description = report.Description,
                Status = report.Status,
                AdminNotes = report.AdminNotes,
                CreatedAt = report.CreatedAt,
                ReviewedAt = report.ReviewedAt,
                PostId = report.PostId,
                PostTitle = report.Post?.Title ?? "Unknown",
                ReportedByUserId = report.ReportedByUserId,
                ReportedByUsername = report.ReportedBy?.Username ?? "Unknown",
                ReviewedByUserId = report.ReviewedByUserId,
                ReviewedByUsername = report.ReviewedBy?.Username
            };
        }
    }
}
