namespace StudentManagement.Core.Enums;

public enum EnrollmentWorkflowStatus
{
    WaitingForApproval,
    Processing,
    Completed,
    Rejected,
    Failed,
    Interrupted,
    ReadyForRetry
}
