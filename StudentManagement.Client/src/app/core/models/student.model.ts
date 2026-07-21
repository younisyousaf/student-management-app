export interface Student {
  id: number;
  rollNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  address: string | null;
  dateOfBirth: string;
  admissionDate: string;
  fullName: string;
}

export interface CreateStudentRequest {
  rollNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  dateOfBirth?: string;
  phone?: string;
  address?: string;
}

export interface UpdateStudentRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  address?: string;
}
