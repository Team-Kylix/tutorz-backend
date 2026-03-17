namespace Tutorz.Domain.Enums
{
    public enum AssignmentStatus
    {
        Pending = 0,
        Active = 1,
        Declined = 2,
        Inactive = 3
    }

    public enum RequestInitiator
    {
        Institute = 0,
        User = 1 // Tutor or Student
    }
}
