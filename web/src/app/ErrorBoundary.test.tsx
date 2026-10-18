import { render, screen } from "@testing-library/react";
import { ERROR_FALLBACK_MESSAGE, ErrorBoundary } from "./ErrorBoundary";

const HEALTHY_TEXT = "All fine";
const CRASH_MESSAGE = "boom";

function Crash(): never {
  throw new Error(CRASH_MESSAGE);
}

afterEach(() => {
  vi.restoreAllMocks();
});

describe("ErrorBoundary", () => {
  it("shows its children when nothing fails", () => {
    render(
      <ErrorBoundary>
        <p>{HEALTHY_TEXT}</p>
      </ErrorBoundary>,
    );

    expect(screen.getByText(HEALTHY_TEXT)).toBeInTheDocument();
  });

  it("shows a fallback instead of a blank page when a child throws", () => {
    const logged = vi.spyOn(console, "error").mockImplementation(() => undefined);

    render(
      <ErrorBoundary>
        <Crash />
      </ErrorBoundary>,
    );

    expect(screen.getByRole("alert")).toHaveTextContent(ERROR_FALLBACK_MESSAGE);
    expect(logged).toHaveBeenCalled();
  });
});
