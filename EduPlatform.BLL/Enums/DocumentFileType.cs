namespace EduPlatform.BLL.Enums;

/// <summary>
/// File types supported by the Document upload pipeline.
/// Values are persisted as strings and matched against the upload content type.
/// </summary>
public enum DocumentFileType
{
    Unknown = 0,
    Pdf = 1,
    Docx = 2,
    Txt = 3,
    Md = 4
}