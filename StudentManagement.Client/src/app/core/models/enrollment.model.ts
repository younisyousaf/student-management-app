export interface Enrollment {
  id: number;
  studentId: number;
  courseId: number;
  enrollDate: string;
  status: 'Active' | 'Dropped' | 'Completed';
}

export interface EnrollStudentRequest {
  studentId: number;
  courseId: number;
}
