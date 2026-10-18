import { Component, type ErrorInfo, type ReactNode } from "react";

export const ERROR_FALLBACK_MESSAGE = "Something went wrong. Reload the page to try again.";

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
}

// React has no hook for error boundaries, so this is the one class component (AD-10: KISS).
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false };

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error(error, info.componentStack);
  }

  render(): ReactNode {
    return this.state.hasError ? <p role="alert">{ERROR_FALLBACK_MESSAGE}</p> : this.props.children;
  }
}
