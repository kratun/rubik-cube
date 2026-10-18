import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RESET_LABEL, SessionControls, UNDO_LABEL } from "./SessionControls";

const OTHER_LABEL = "Other";

describe("SessionControls", () => {
  it("calls onUndo and onReset for the matching buttons", async () => {
    const onUndo = vi.fn();
    const onReset = vi.fn();
    render(<SessionControls canUndo onUndo={onUndo} onReset={onReset} />);

    await userEvent.click(screen.getByRole("button", { name: UNDO_LABEL }));
    await userEvent.click(screen.getByRole("button", { name: RESET_LABEL }));

    expect(onUndo).toHaveBeenCalledOnce();
    expect(onReset).toHaveBeenCalledOnce();
  });

  it("disables Undo when there is nothing to undo and keeps Reset enabled", () => {
    render(<SessionControls canUndo={false} onUndo={vi.fn()} onReset={vi.fn()} />);

    expect(screen.getByRole("button", { name: UNDO_LABEL })).toBeDisabled();
    expect(screen.getByRole("button", { name: RESET_LABEL })).toBeEnabled();
  });

  it("does not take focus on first render", () => {
    render(<SessionControls canUndo={false} onUndo={vi.fn()} onReset={vi.fn()} />);

    expect(screen.getByRole("button", { name: RESET_LABEL })).not.toHaveFocus();
  });

  it("moves focus from Undo to Reset when Undo becomes disabled while focused", () => {
    const { rerender } = render(<SessionControls canUndo onUndo={vi.fn()} onReset={vi.fn()} />);
    screen.getByRole("button", { name: UNDO_LABEL }).focus();

    rerender(<SessionControls canUndo={false} onUndo={vi.fn()} onReset={vi.fn()} />);

    expect(screen.getByRole("button", { name: RESET_LABEL })).toHaveFocus();
  });

  it("leaves focus alone when Undo becomes disabled while another control has focus", () => {
    const { rerender } = render(
      <>
        <SessionControls canUndo onUndo={vi.fn()} onReset={vi.fn()} />
        <button type="button">{OTHER_LABEL}</button>
      </>,
    );
    const other = screen.getByRole("button", { name: OTHER_LABEL });
    other.focus();

    rerender(
      <>
        <SessionControls canUndo={false} onUndo={vi.fn()} onReset={vi.fn()} />
        <button type="button">{OTHER_LABEL}</button>
      </>,
    );

    expect(other).toHaveFocus();
  });
});
