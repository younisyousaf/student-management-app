import {
  Component,
  EventEmitter,
  Input,
  Output
} from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [],
  templateUrl: './pagination.html',
  styleUrl: './pagination.scss'
})
export class Pagination {

  @Input()
  pageNumber = 1;

  @Input()
  totalPages = 0;

  @Input()
  totalCount = 0;

  @Input()
  isLoading = false;

  @Output()
  pageChange =
    new EventEmitter<number>();

  previousPage(): void {

    if (
      this.isLoading ||
      this.pageNumber <= 1
    ) {
      return;
    }

    this.pageChange.emit(
      this.pageNumber - 1
    );
  }

  nextPage(): void {

    if (
      this.isLoading ||
      this.pageNumber >=
        this.totalPages
    ) {
      return;
    }

    this.pageChange.emit(
      this.pageNumber + 1
    );
  }
}
