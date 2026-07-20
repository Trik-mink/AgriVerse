import { AsyncLocalStorage } from 'node:async_hooks';

export const JUDGE_SESSION_HEADER = 'x-agriverse-session';

export type SafeRequestContext = {
  requestId: string;
  route: string;
  sessionId?: string;
};

const requestContext = new AsyncLocalStorage<SafeRequestContext>();

export function runWithRequestContext<Result>(context: SafeRequestContext, operation: () => Result): Result {
  return requestContext.run(context, operation);
}

export function getRequestContext(): SafeRequestContext | undefined {
  return requestContext.getStore();
}

export function setRequestSession(sessionId: string): void {
  const context = requestContext.getStore();
  if (context) {
    context.sessionId = sessionId;
  }
}

export function getModelSessionId(): string {
  const sessionId = requestContext.getStore()?.sessionId;
  if (sessionId) {
    return sessionId;
  }

  if (process.env.NODE_ENV === 'production') {
    throw new Error('A production model call was attempted without a validated judge session.');
  }

  return 'local-development';
}
