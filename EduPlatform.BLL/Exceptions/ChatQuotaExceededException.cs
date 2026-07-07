using System;

namespace EduPlatform.BLL.Exceptions;

public sealed class ChatQuotaExceededException(string message) : Exception(message)
{
}
