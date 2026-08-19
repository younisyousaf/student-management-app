using Microsoft.Agents.AI.Workflows;

namespace StudentManagement.AI.Workflows.Enrollment;

public sealed class EnrollmentWorkflowCheckpointStore
{
    public CheckpointManager CheckpointManager { get; }

    public EnrollmentWorkflowCheckpointStore(
        CheckpointManager checkpointManager)
    {
        CheckpointManager = checkpointManager;
    }
}
