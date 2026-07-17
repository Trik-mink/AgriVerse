import { Component, type ErrorInfo, type ReactNode } from 'react';

type ImmersiveErrorBoundaryProps = { children: ReactNode; contextLost: boolean; onCanvasFailure: () => void };
type ImmersiveErrorBoundaryState = { hasError: boolean };

export class ImmersiveErrorBoundary extends Component<ImmersiveErrorBoundaryProps, ImmersiveErrorBoundaryState> {
  state: ImmersiveErrorBoundaryState = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  static getDerivedStateFromProps(props: ImmersiveErrorBoundaryProps) {
    return props.contextLost ? { hasError: true } : null;
  }

  componentDidCatch(_error: Error, _info: ErrorInfo) {
    this.props.onCanvasFailure();
  }

  componentDidUpdate(previousProps: ImmersiveErrorBoundaryProps) {
    if (!previousProps.contextLost && this.props.contextLost) this.props.onCanvasFailure();
  }

  render() {
    if (this.state.hasError) {
      return <div className="immersive-crash-notice" role="status">Opening the classic investigation. Your work is intact.</div>;
    }

    return this.props.children;
  }
}
