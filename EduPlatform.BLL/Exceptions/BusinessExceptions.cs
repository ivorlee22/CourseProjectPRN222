namespace EduPlatform.BLL.Exceptions;

public sealed class ResourceNotFoundException(string message) : Exception(message);

public sealed class ForbiddenOperationException(string message) : Exception(message);

public sealed class ResourceConflictException(string message) : Exception(message);

public sealed class BusinessValidationException(string message) : Exception(message);

public sealed class CourseQuotaExceededException(string message) : Exception(message);

public sealed class DocumentProcessingException(string message, Exception? innerException = null)
    : Exception(message, innerException);
