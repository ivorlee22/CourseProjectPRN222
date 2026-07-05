using System.ComponentModel.DataAnnotations;
using EduPlatform.BLL.DTOs.Documents;
using EduPlatform.BLL.Enums;

namespace EduPlatform.Web.ViewModels.Documents;

public sealed record DocumentIndexViewModel(
    Guid CourseId,
    string CourseTitle,
    IReadOnlyList<DocumentSummaryDto> Documents,
    bool CanManage);

public sealed record DocumentDetailsViewModel(
    DocumentDetailsDto Document,
    IReadOnlyList<DocumentChunkDto> Chunks,
    bool CanManage,
    bool CanViewChunks);

public sealed class UploadDocumentViewModel
{
    public Guid CourseId { get; init; }

    public string? CourseTitle { get; init; }

    [Required(ErrorMessage = "Vui lòng chọn tệp cần tải lên.")]
    [Display(Name = "Tệp tài liệu")]
    public IFormFile? File { get; init; }

    [Display(Name = "Tôi đã đọc và đồng ý với điều khoản sử dụng")]
    public bool AcceptTerms { get; init; }
}