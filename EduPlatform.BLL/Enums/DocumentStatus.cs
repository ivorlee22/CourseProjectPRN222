namespace EduPlatform.BLL.Enums;

/// <summary>
/// Lifecycle of a document uploaded to the platform. Mirrored in
/// <c>EduPlatform.DAL.Entities.DocumentStatus</c>; the BLL keeps its own enum
/// so it does not need to reference the persistence layer.
/// </summary>
public enum DocumentStatus
{
    Pending = 1,
    Processing = 2,
    Ready = 3,
    Failed = 4
}