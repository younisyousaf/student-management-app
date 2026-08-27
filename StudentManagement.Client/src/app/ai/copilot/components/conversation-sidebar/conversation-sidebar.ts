import { Component, input, output } from '@angular/core';
import { CopilotConversation } from '../../models/copilot.model';
@Component({
  selector: 'app-conversation-sidebar',
  standalone: true,
  templateUrl: './conversation-sidebar.html',
  styleUrl: './conversation-sidebar.scss'
})
export class ConversationSidebar {
  readonly conversations = input.required<CopilotConversation[]>();
  readonly currentThreadId = input.required<string>();
  readonly isSending = input.required<boolean>();
  readonly isLoadingHistory = input.required<boolean>();
  readonly isLoadingConversations = input.required<boolean>();
  readonly pageNumber = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly hasPreviousPage = input.required<boolean>();
  readonly hasNextPage = input.required<boolean>();
  readonly openMenuThreadId = input.required<string | null>();
  readonly renamingThreadId = input.required<string | null>();
  readonly renameTitle = input.required<string>();
  readonly managingThreadId = input.required<string | null>();
  readonly newConversation = output<void>();
  readonly openConversation = output<CopilotConversation>();
  readonly toggleMenu = output<{
    threadId: string;
    event: Event;
  }>();
  readonly beginRename = output<{
    conversation: CopilotConversation;
    event: Event;
  }>();
  readonly renameInput = output<Event>();
  readonly saveRename = output<{
    conversation: CopilotConversation;
    event: Event;
  }>();
  readonly cancelRename = output<Event | undefined>();
  readonly deleteRequest = output<{
    conversation: CopilotConversation;
    event: Event;
  }>();
  readonly previousPage = output<void>();
  readonly nextPage = output<void>();
}
