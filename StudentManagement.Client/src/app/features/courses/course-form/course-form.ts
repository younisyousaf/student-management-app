import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CoursesService } from '../course.service';

@Component({
  selector: 'app-course-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './course-form.html'
})
export class CourseForm implements OnInit {
  isEditMode = signal(false);
  courseId = signal<number | null>(null);

  code = signal('');
  name = signal('');
  description = signal('');
  durationMonths = signal<number>(1);
  feeAmount = signal<number>(0);

  errorMessage = signal<string | null>(null);
  isSaving = signal(false);

  constructor(
    private coursesService: CoursesService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) return;

    this.isEditMode.set(true);
    const id = Number(idParam);
    this.courseId.set(id);

    this.coursesService.getById(id).subscribe({
      next: (res) => {
        const c = res.data;
        this.code.set(c.code);
        this.name.set(c.name);
        this.description.set(c.description ?? '');
        this.durationMonths.set(c.durationMonths);
        this.feeAmount.set(c.feeAmount);
      },
      error: () => this.errorMessage.set('Could not load course.')
    });
  }

  onSubmit(): void {
  this.errorMessage.set(null);
  this.isSaving.set(true);

  const payload = {
    code: this.code(),
    name: this.name(),
    description: this.description() || undefined,
    durationMonths: this.durationMonths(),
    feeAmount: this.feeAmount()
  };

  if (this.isEditMode()) {
    this.coursesService.update(this.courseId()!, payload).subscribe({
      next: () => this.router.navigate(['/courses']),
      error: (err: unknown) => {
        this.errorMessage.set(this.extractErrorMessage(err));
        this.isSaving.set(false);
      }
    });
  } else {
    this.coursesService.create(payload).subscribe({
      next: () => this.router.navigate(['/courses']),
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
