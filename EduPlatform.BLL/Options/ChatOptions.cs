namespace EduPlatform.BLL.Options;

public sealed class ChatOptions
{
    public const string SectionName = "Chat";

    public int RetrievalLimit { get; init; } = 5;

    public double MinimumSimilarityScore { get; init; } = 0.5;

    public int MaxQuestionLength { get; init; } = 4000;

    public int MaxContextCharacters { get; init; } = 12000;

    public string EmptyContextMessage { get; init; }
        = "Không tìm thấy nội dung phù hợp trong tài liệu của khóa học để trả lời câu hỏi này.";
}
