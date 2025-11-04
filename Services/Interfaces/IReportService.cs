using MyEcologicCrowsourcingApp.DTOs.Forum;
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IReportService
    {
        Task<ReportDto> ReportPostAsync(CreateReportDto dto, Guid userId);
        Task<IEnumerable<ReportDto>> GetAllReportsAsync(ReportStatus? status = null, int pageNumber = 1, int pageSize = 50);
        Task<ReportDto?> GetReportByIdAsync(Guid id);
        Task<ReportDto> ReviewReportAsync(Guid id, ReviewReportDto dto, Guid adminUserId);
        Task<int> GetPendingReportCountAsync();
    }
}