export enum PaymentStatus {
  Unpaid = 0,
  Partial = 1,
  Paid = 2
}

export interface Fee {
  id: number;
  studentId: number;
  courseId: number;
  amountDue: number;
  amountPaid: number;
  paymentDate: string | null;
  remarks: string | null;
  status: PaymentStatus;
  remainingBalance: number;
  isFullySettled: boolean;
}

export interface ProcessPaymentRequest {
  studentId: number;
  courseId: number;
  amountPaid: number;
  remarks?: string;
}
