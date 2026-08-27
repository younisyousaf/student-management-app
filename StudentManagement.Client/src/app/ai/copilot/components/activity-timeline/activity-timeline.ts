import { Component, input, output } from '@angular/core';
import {
  CopilotActivity,
  CopilotActivityStatus
} from '../../models/copilot.model';
@Component({
  selector: 'app-activity-timeline',
  standalone: true,
  templateUrl: './activity-timeline.html',
  styleUrl: './activity-timeline.scss'
})
export class ActivityTimeline {
  readonly activities = input.required<CopilotActivity[]>();
  readonly expanded = input.required<boolean>();
  readonly isSending = input.required<boolean>();
  readonly toggle = output<void>();
  readonly stopped = input.required<boolean>();
  activityText(activity: CopilotActivity): string {
    const labels: Record<string, { running: string; completed: string }> = {
      GetStudentById: {
        running: 'Looking up student...',
        completed: 'Student lookup completed'
      },
      GetStudentByRollNumber: {
        running: 'Looking up student...',
        completed: 'Student lookup completed'
      },
      SearchStudentsByName: {
        running: 'Searching student records...',
        completed: 'Student search completed'
      },
      GetCourseById: {
        running: 'Looking up course...',
        completed: 'Course lookup completed'
      },
      GetCourseByCode: {
        running: 'Looking up course...',
        completed: 'Course lookup completed'
      },
      SearchCoursesByName: {
        running: 'Searching courses...',
        completed: 'Course search completed'
      },
      GetAllCourses: {
        running: 'Retrieving courses...',
        completed: 'Courses retrieved'
      },
      GetEnrollmentById: {
        running: 'Looking up enrollment...',
        completed: 'Enrollment lookup completed'
      },
      GetEnrollmentsByStudent: {
        running: 'Checking student enrollments...',
        completed: 'Student enrollments checked'
      },
      GetEnrollmentForStudentCourse: {
        running: 'Checking existing enrollment...',
        completed: 'Existing enrollment checked'
      },
      GetEnrollmentsByCourse: {
        running: 'Retrieving course enrollments...',
        completed: 'Course enrollments retrieved'
      },
      GetAttendanceById: {
        running: 'Looking up attendance record...',
        completed: 'Attendance record retrieved'
      },
      GetAttendanceForStudent: {
        running: 'Reviewing student attendance...',
        completed: 'Student attendance retrieved'
      },
      GetAttendanceForCourseOnDate: {
        running: 'Checking course attendance...',
        completed: 'Course attendance retrieved'
      },
      GetAttendanceSummaryForStudent: {
        running: 'Calculating attendance summary...',
        completed: 'Attendance summary calculated'
      },
      GetFeeById: {
        running: 'Looking up fee record...',
        completed: 'Fee record retrieved'
      },
      GetFeeStatement: {
        running: 'Checking fee statement...',
        completed: 'Fee statement retrieved'
      },
      GetFeesForStudent: {
        running: 'Reviewing student fees...',
        completed: 'Student fees retrieved'
      },
      SearchInstitutionalKnowledge: {
        running: 'Searching institutional knowledge...',
        completed: 'Institutional knowledge searched'
      },
      GetStudentsBelowAttendanceThreshold: {
        running: 'Finding students with low attendance...',
        completed: 'Low-attendance report completed'
      },
      GetStudentsWithOutstandingFees: {
        running: 'Checking outstanding student fees...',
        completed: 'Outstanding-fee report completed'
      },
      GetCourseAttendanceSummary: {
        running: 'Calculating course attendance...',
        completed: 'Course attendance summary calculated'
      },
      GetStudentsWithNoAttendanceRecords: {
        running: 'Finding students without attendance records...',
        completed: 'No-attendance report completed'
      },
      GetStudentsWithNoActiveEnrollment: {
        running: 'Checking active student enrollments...',
        completed: 'Enrollment-status report completed'
      },
      GetInstitutionFeeSummary: {
        running: 'Calculating institution fee summary...',
        completed: 'Institution fee summary calculated'
      },
      load_skill: {
        running: 'Loading task guidance...',
        completed: 'Task guidance loaded'
      },
      read_skill_resource: {
        running: 'Reading skill resource...',
        completed: 'Skill resource read'
      },
      run_skill_script: {
        running: 'Running skill...',
        completed: 'Skill completed'
      },
      create_student: {
        running: 'Creating student...',
        completed: 'Student created'
      },
      create_course: {
        running: 'Creating course...',
        completed: 'Course created'
      },
      enroll_student: {
        running: 'Enrolling student...',
        completed: 'Student enrolled'
      },
      drop_course: {
        running: 'Dropping enrollment...',
        completed: 'Enrollment dropped'
      },
      complete_course: {
        running: 'Completing enrollment...',
        completed: 'Enrollment completed'
      },
      mark_attendance: {
        running: 'Recording attendance...',
        completed: 'Attendance recorded'
      },
      mark_attendance_today: {
        running: 'Recording today\'s attendance...',
        completed: 'Today\'s attendance recorded'
      },
      update_attendance: {
        running: 'Updating attendance...',
        completed: 'Attendance updated'
      },
      process_student_payment: {
        running: 'Processing payment...',
        completed: 'Payment recorded'
      },
      update_student_profile: {
        running: 'Updating student profile...',
        completed: 'Student profile updated'
      },
      remove_student: {
        running: 'Removing student...',
        completed: 'Student removed'
      },
      update_course_details: {
        running: 'Updating course...',
        completed: 'Course updated'
      },
      update_course_pricing: {
        running: 'Updating course pricing...',
        completed: 'Course pricing updated'
      },
      remove_course: {
        running: 'Removing course...',
        completed: 'Course removed'
      }
    };
    const label = labels[activity.toolName];
    if (activity.status === 'running') {
      return label?.running ?? `Running ${this.humanizeToolName(activity.toolName)}...`;
    }
    if (activity.status === 'completed') {
      return label?.completed ?? `${this.humanizeToolName(activity.toolName)} completed`;
    }
    if (activity.status === 'waiting') {
      return `Waiting for approval: ${this.humanizeToolName(activity.toolName)}`;
    }
    if (activity.status === 'rejected') {
      return `${this.humanizeToolName(activity.toolName)} was rejected`;
    }
    if (activity.status === 'stopped') {
      return `${this.humanizeToolName(activity.toolName)} stopped`;
    }
    return `${this.humanizeToolName(activity.toolName)} failed`;
  }
  activityStatusSymbol(status: CopilotActivityStatus): string {
    switch (status) {
      case 'completed':
        return '✓';
      case 'waiting':
        return '○';
      case 'rejected':
        return '×';
      case 'stopped':
        return '■';
      case 'failed':
        return '!';
      default:
        return '●';
    }
  }
  private humanizeToolName(toolName: string): string {
    const value = toolName
      .replace(/_/g, ' ')
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .toLowerCase();
    return value.charAt(0).toUpperCase() + value.slice(1);
  }

  // hasStoppedActivity(): boolean {
  // return this.activities().some(
  //   activity =>
  //     activity.status === 'stopped'
  // );
// }
}
