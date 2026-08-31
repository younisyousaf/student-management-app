import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideArrowLeft, LucideBookOpen, LucideSave } from '@lucide/angular';
import { ToastService } from '../../../core/services/toast.service';
import { CoursesService } from '../course.service';

@Component({
  selector: 'app-course-form',
  standalone: true,
  imports: [FormsModule, RouterLink, LucideArrowLeft, LucideBookOpen, LucideSave],
  templateUrl: './course-form.html',
  styleUrl: './course-form.scss'
})
export class CourseForm implements OnInit {
  private readonly coursesService = inject(CoursesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  readonly isEditMode = signal(false);
  readonly courseId = signal<number | null>(null);
  readonly code = signal('');
  readonly name = signal('');
  readonly description = signal('');
  readonly durationMonths = signal(1);
  readonly feeAmount = signal(0);
  readonly fieldErrors = signal<{ [field: string]: string[] }>({});
  readonly isSaving = signal(false);

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) return;

    const id = Number(idParam);
    this.isEditMode.set(true);
    this.courseId.set(id);

    this.coursesService.getById(id).subscribe({
      next: res => {
        const course = res.data;
        this.code.set(course.code);
        this.name.set(course.name);
        this.description.set(course.description ?? '');
        this.durationMonths.set(course.durationMonths);
        this.feeAmount.set(course.feeAmount);
      },
      error: () => this.toastService.error('Unable to load course', 'The course record could not be loaded.')
    });
  }

  onSubmit(): void {
    this.fieldErrors.set({});
    this.isSaving.set(true);

    const payload = {
      code: this.code(),
      name: this.name(),
      description: this.description() || undefined,
      durationMonths: this.durationMonths(),
      feeAmount: this.feeAmount()
    };

    const success = () => {
      this.toastService.success(
        this.isEditMode() ? 'Course updated' : 'Course created',
        this.isEditMode() ? 'Course details were saved successfully.' : 'The new course was added successfully.'
      );
      this.router.navigate(['/courses']);
    };

    const failure = (err: unknown) => {
      this.handleError(err);
      this.isSaving.set(false);
    };

    if (this.isEditMode()) {
      this.coursesService.update(this.courseId()!, payload).subscribe({ next: success, error: failure });
      return;
    }

    this.coursesService.create(payload).subscribe({ next: success, error: failure });
  }

  fieldError(name: string): string | null {
    const errors = this.fieldErrors();
    const key = Object.keys(errors).find(key => key.toLowerCase() === name.toLowerCase());
    return key ? errors[key].join(' ') : null;
  }

  private handleError(err: unknown): void {
    if (err && typeof err === 'object' && 'error' in err) {
      const httpError = (err as { error?: { message?: string; errors?: { [field: string]: string[] } } }).error;
      if (httpError?.errors) this.fieldErrors.set(httpError.errors);
      this.toastService.error('Save failed', httpError?.message ?? 'The course could not be saved.');
      return;
    }

    this.toastService.error('Save failed', 'The course could not be saved.');
  }
}
