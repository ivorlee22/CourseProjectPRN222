using System.ComponentModel.DataAnnotations;

namespace EduPlatform.Web.ViewModels.Admin;

public sealed class ChunkingConfigViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Chunk Size.")]
    [Range(100, 10000, ErrorMessage = "Chunk Size phải từ 100 đến 10.000 ký tự.")]
    [Display(Name = "Chunk Size (ký tự)")]
    public int ChunkSize { get; set; } = 1500;

    [Required(ErrorMessage = "Vui lòng nhập Chunk Overlap.")]
    [Range(0, 9999, ErrorMessage = "Chunk Overlap phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Chunk Overlap (ký tự)")]
    public int ChunkOverlap { get; set; } = 200;

    [Required(ErrorMessage = "Vui lòng nhập kích thước file tối đa.")]
    [Range(1, 100, ErrorMessage = "Kích thước file phải từ 1 MB đến 100 MB.")]
    [Display(Name = "Kích thước file tối đa (MB)")]
    public long MaxFileSizeMb { get; set; } = 25;

    public DateTimeOffset? LastUpdatedUtc { get; set; }
}
