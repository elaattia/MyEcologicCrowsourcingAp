using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.DTOs.Forum;
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IPostReportService
    {
        Task<ReportDto> CreateReportAsync(CreateReportDto dto, Guid userId);
        Task<ReportDto?> GetReportByIdAsync(Guid id);
        Task<IEnumerable<ReportDto>> GetAllReportsAsync(ReportStatus? status = null, int pageNumber = 1, int pageSize = 50);
        Task<ReportDto> ReviewReportAsync(Guid id, ReviewReportDto dto, Guid reviewerId);
        Task<int> GetPendingReportsCountAsync();
    }
}