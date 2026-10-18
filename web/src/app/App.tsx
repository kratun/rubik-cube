import { CubePage } from "../features/cube/CubePage";
import { ErrorBoundary } from "./ErrorBoundary";

export const APP_TITLE = "Rubik's Cube Simulator";

export function App() {
  return (
    <main>
      <h1>{APP_TITLE}</h1>
      <ErrorBoundary>
        <CubePage />
      </ErrorBoundary>
    </main>
  );
}
