using System.ComponentModel.DataAnnotations;

namespace EduPlatform.Web.ViewModels.AdminPackage;

public class CreatePackageViewModel
{
    [Required(ErrorMessage = "Tên gói cước là bắt buộc.")]
    [Display(Name = "Tên gói cước")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả là bắt buộc.")]
    [Display(Name = "Mô tả")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giá tiền là bắt buộc.")]
    [Display(Name = "Giá tiền")]
    [Range(0, 1000000000, ErrorMessage = "Giá tiền phải từ 0 đến 1,000,000,000")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Số khóa học tối đa là bắt buộc.")]
    [Display(Name = "Số khóa học tối đa")]
    [Range(1, 10000, ErrorMessage = "Số khóa học tối đa phải từ 1 đến 10,000")]
    public int MaxCourses { get; set; }

    [Required(ErrorMessage = "Số tin nhắn tối đa mỗi ngày là bắt buộc.")]
    [Display(Name = "Số tin nhắn chat/ngày")]
    [Range(1, 100000, ErrorMessage = "Số tin nhắn phải từ 1 đến 100,000")]
    public int DailyChats { get; set; }

    [Required(ErrorMessage = "Thời hạn (ngày) là bắt buộc.")]
    [Display(Name = "Thời hạn (Ngày)")]
    [Range(1, 3650, ErrorMessage = "Thời hạn phải từ 1 đến 3650 ngày")]
    public int DurationDays { get; set; }

    [Display(Name = "Trạng thái (Hiển thị)")]
    public bool IsActive { get; set; } = true;
}

public class EditPackageViewModel : CreatePackageViewModel
{
    public Guid Id { get; set; }
}
