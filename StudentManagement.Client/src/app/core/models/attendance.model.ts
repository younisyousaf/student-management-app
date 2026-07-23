export enum AttendanceStatus {
  Present = 0,
  Absent = 1,
  Late = 2,
  Excused = 3
}

export interface Attendance {
  id: number;
  studentId: number;
  courseId: number;
  date: string;
  status: AttendanceStatus;
  remarks: string | null;
}

export interface MarkAttendanceRequest {
  studentId: number;
  courseId: number;
  date: string;
  status: AttendanceStatus;
  remarks?: string;
}

export interface UpdateAttendanceRequest {
  status: AttendanceStatus;
  remarks?: string;
}
