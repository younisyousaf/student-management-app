import {
  Component,
  computed,
  input,
  output
} from '@angular/core';

@Component({
  selector: 'app-turn-version-navigator',
  standalone: true,
  templateUrl: './turn-version-navigator.html',
  styleUrl: './turn-version-navigator.scss'
})
export class TurnVersionNavigator {
  readonly currentVersion =
    input.required<number>();

  readonly totalVersions =
    input.required<number>();

  readonly loading = input(false);

  readonly previous = output<void>();
  readonly next = output<void>();

  readonly canGoPrevious = computed(
    () =>
      !this.loading() &&
      this.currentVersion() > 1
  );

  readonly canGoNext = computed(
    () =>
      !this.loading() &&
      this.currentVersion() <
        this.totalVersions()
  );
}
