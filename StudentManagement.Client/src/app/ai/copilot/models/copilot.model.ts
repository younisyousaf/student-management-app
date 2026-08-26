export interface CopilotChatRequest {
  message: string;
  sessionId?: string | null;
}
export interface CopilotChatResponse {
  response: string;
  sessionId: string;
}
export type CopilotMessageRole =
  | 'user'
  | 'assistant';
export interface CopilotMessage {
  id: string;
  role: CopilotMessageRole;
  content: string;
  createdAt: Date | null;
}
export interface CopilotHistoryMessage {
  id: string;
  role: CopilotMessageRole;
  content: string;
  createdAt: string | null;
}
export interface CopilotApprovalRequest {
  interruptId: string;
  toolCallId: string;
  toolName: string;
  arguments: string;
  message?: string;
}
export interface CopilotConversation {
  threadId: string;
  title: string;
  lastRunId: string | null;
  createdAt: string;
  updatedAt: string;
}
