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
  createdAt: Date;
}

export interface CopilotApprovalRequest {
  interruptId: string;
  toolCallId: string;
  toolName: string;
  arguments: string;
  message?: string;
}
