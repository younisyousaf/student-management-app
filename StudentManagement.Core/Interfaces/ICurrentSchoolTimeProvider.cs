namespace StudentManagement.Core.Interfaces;

public interface ICurrentSchoolTimeProvider
{
    DateTime Today { get; }
}