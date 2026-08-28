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
  activities?: CopilotActivity[];
  turnStopped?: boolean;
  activityExpanded?: boolean;
  turnUserMessageId?: string;
}

export interface CopilotHistoryMessage {
  id: string;
  role: CopilotMessageRole;
  content: string;
  createdAt: string | null;
}
export interface CopilotApprovalDisplayItem {
  label: string;
  value: string;
}
export interface CopilotApprovalRequest {
  interruptId: string;
  toolCallId: string;
  toolName: string;
  arguments: string;
  message?: string;
  displayTitle?: string;
  displayDetails?: CopilotApprovalDisplayItem[];
  warning?: string | null;
}
export interface CopilotConversation {
  threadId: string;
  title: string;
  lastRunId: string | null;
  createdAt: string;
  updatedAt: string;
}
export type CopilotActivityStatus =
  | 'running'
  | 'completed'
  | 'waiting'
  | 'rejected'
  | 'stopped'
  | 'failed';
export interface CopilotActivity {
  id: string;
  toolName: string;
  status: CopilotActivityStatus;
}

export interface CopilotTurn {
  userMessageId: string;
  status: 'Prepared' | 'Completed' | 'Stopped' | 'Failed';
  activities: CopilotActivity[];
  createdAt: string;
  updatedAt: string;
}
