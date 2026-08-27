import { Pipe, PipeTransform } from '@angular/core';
import { marked } from 'marked';

@Pipe({
  name: 'markdown',
  standalone: true
})
export class MarkdownPipe implements PipeTransform {
  transform(value: string): string {
    if (!value) {
      return '';
    }

    const html = marked.parse(value, {
      gfm: true,
      breaks: true
    }) as string;

    return html
      .replace(
        /<table>/g,
        '<div class="markdown-table-wrapper"><table>'
      )
      .replace(
        /<\/table>/g,
        '</table></div>'
      );
  }
}
