using System.ComponentModel.DataAnnotations;
using EduPlatform.BLL.DTOs.Chats;

namespace EduPlatform.Web.ViewModels.Chats;

public sealed record ChatPageViewModel(
    Guid CourseId,
    string CourseTitle,
    IReadOnlyList<ChatSessionDto> Sessions,
    ChatSessionDto? ActiveSession,
    IReadOnlyList<ChatMessageDto> Messages,
    ChatMessageInputViewModel Input);

public sealed class ChatMessageInputViewModel
{
    [Required(ErrorMessage = "Hãy nhập câu hỏi của bạn.")]
    [StringLength(4000, ErrorMessage = "Câu hỏi không được quá 4000 ký tự.")]
    public string Question { get; set; } = string.Empty;
}
