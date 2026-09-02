namespace StudentManagement.Core.Security;

public static class Permissions
{
    public static class Students
    {
        public const string Read = "students.read";
        public const string Create = "students.create";
        public const string Update = "students.update";
        public const string Delete = "students.delete";
    }

    public static class Courses
    {
        public const string Read = "courses.read";
        public const string Create = "courses.create";
        public const string Update = "courses.update";
        public const string Delete = "courses.delete";
    }

    public static class Enrollments
    {
        public const string Read = "enrollments.read";
        public const string Create = "enrollments.create";
        public const string Complete = "enrollments.complete";
        public const string Drop = "enrollments.drop";
    }

    public static class Attendance
    {
        public const string Read = "attendance.read";
        public const string Mark = "attendance.mark";
        public const string Update = "attendance.update";
    }

    public static class Fees
    {
        public const string Read = "fees.read";
        public const string RecordPayment = "fees.payment.record";
    }

    public static class Users
    {
        public const string Read = "users.read";
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Deactivate = "users.deactivate";
        public const string AssignRoles = "users.roles.assign";
    }

    public static class Schools
    {
        public const string Read = "schools.read";
        public const string Create = "schools.create";
        public const string Update = "schools.update";
        public const string Deactivate = "schools.deactivate";
    }

    public static class Settings
    {
        public const string Read = "settings.read";
        public const string Update = "settings.update";
    }

    public static class Copilot
    {
        public const string Use = "copilot.use";
    }

    public static class Knowledge
    {
        public const string Read = "knowledge.read";
        public const string Manage = "knowledge.manage";
    }

    public static class Dashboard
    {
        public const string Read = "dashboard.read";
    }

    public static readonly string[] All =
    [
        Students.Read,
        Students.Create,
        Students.Update,
        Students.Delete,

        Courses.Read,
        Courses.Create,
        Courses.Update,
        Courses.Delete,

        Enrollments.Read,
        Enrollments.Create,
        Enrollments.Complete,
        Enrollments.Drop,

        Attendance.Read,
        Attendance.Mark,
        Attendance.Update,

        Fees.Read,
        Fees.RecordPayment,

        Users.Read,
        Users.Create,
        Users.Update,
        Users.Deactivate,
        Users.AssignRoles,

        Schools.Read,
        Schools.Create,
        Schools.Update,
        Schools.Deactivate,

        Settings.Read,
        Settings.Update,

        Copilot.Use,

        Knowledge.Read,
        Knowledge.Manage,

        Dashboard.Read
    ];
}