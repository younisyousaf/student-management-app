export interface ApiResponse<T> {
  message: string;
  data: T;
  errors?: { [field: string]: string[] };
}
