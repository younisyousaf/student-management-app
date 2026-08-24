using Microsoft.Agents.AI;

namespace StudentManagement.AI.Agents;

public static class StudentManagementSkillsFactory
{
    public static AgentSkillsProvider Create()
    {
        var skillsPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Skills");

        return new AgentSkillsProvider(
            skillPath: skillsPath,
            options:
                new AgentSkillsProviderOptions
                {
                    DisableLoadSkillApproval = true
                });
    }
}
