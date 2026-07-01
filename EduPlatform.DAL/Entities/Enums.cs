namespace EduPlatform.DAL.Entities;

public enum UserRole
{
    Student = 1,
    Teacher = 2,
    Admin = 3
}

public enum CourseType
{
    Public = 1,
    Private = 2
}

public enum EnrollmentStatus
{
    Pending = 1,
    Active = 2,
    Rejected = 3
}

public enum DocumentStatus
{
    Pending = 1,
    Processing = 2,
    Ready = 3,
    Failed = 4
}

public enum MessageRole
{
    User = 1,
    Assistant = 2,
    System = 3
}

public enum SubscriptionStatus
{
    Pending = 1,
    Active = 2,
    Cancelled = 3,
    Expired = 4
}

public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4,
    Refunded = 5
}

public enum PaymentMethod
{
    VNPay = 1,
    MoMo = 2
}
