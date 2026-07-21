export interface Course {
  id: number;
  code: string;
  name: string;
  description: string | null;
  durationMonths: number;
  feeAmount: number;
}

export interface CourseRequest {
  code: string;
  name: string;
  description?: string;
  durationMonths: number;
  feeAmount: number;
}
