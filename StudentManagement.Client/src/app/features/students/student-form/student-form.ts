import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { StudentsService } from '../students.service';
import { Student } from '../../../core/models/student.model';

@Component({
  selector: 'app-student-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './student-form.html'
})
export class StudentForm implements OnInit {
  isEditMode = signal(false);
  studentId = signal<number | null>(null);

  rollNumber = signal('');
  firstName = signal('');
  lastName = signal('');
  email = signal('');
  dateOfBirth = signal('');
  phone = signal('');
  address = signal('');

  errorMessage = signal<string | null>(null);
  isSaving = signal(false);

  constructor(
    private studentsService: StudentsService,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) return;

    this.isEditMode.set(true);
    const id = Number(idParam);
    this.studentId.set(id);

    this.studentsService.getById(id).subscribe({
      next: (res) => {
        const s = res.data;
        this.rollNumber.set(s.rollNumber);
        this.firstName.set(s.firstName);
        this.lastName.set(s.lastName);
        this.email.set(s.email);
        this.dateOfBirth.set(s.dateOfBirth.substring(0, 10));
        this.phone.set(s.phone ?? '');
        this.address.set(s.address ?? '');
      },
      error: () => this.errorMessage.set('Could not load student.')
    });
  }

  onSubmit(): void {
    this.errorMessage.set(null);
    this.isSaving.set(true);

    if (this.isEditMode()) {
      this.studentsService.update(this.studentId()!, {
        firstName: this.firstName(),
        lastName: this.lastName(),
        email: this.email(),
        phone: this.phone() || undefined,
        address: this.address() || undefined
      }).subscribe({
        next: () => this.router.navigate(['/students']),
        error: (err: unknown) => {
          this.errorMessage.set(this.extractErrorMessage(err));
          this.isSaving.set(false);
        }
      });
    } else {
      this.studentsService.create({
        rollNumber: this.rollNumber(),
        firstName: this.firstName(),
        lastName: this.lastName(),
        email: this.email(),
        dateOfBirth: this.dateOfBirth(),
        phone: this.phone() || undefined,
        address: this.address() || undefined
      }).subscribe({
        next: () => this.router.navigate(['/students']),
        error: (err: unknown) => {
          this.errorMessage.set(this.extractErrorMessage(err));
          this.isSaving.set(false);
        }
      });
    }
  }

  private extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const httpError = (err as { error?: { message?: string } }).error;
      if (httpError?.message) return httpError.message;
    }
    return 'Save failed.';
  }
}
