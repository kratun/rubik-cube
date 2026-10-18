import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
  BUSY_LABEL,
  RESET_LABEL,
  SESSION_CONTROLS_LABEL,
  SHOW_EXPLODED_VIEW_LABEL,
  SessionControls,
  TTC_LABEL,
  UNDO_LABEL,
} from "./SessionControls";

const OTHER_LABEL = "Other";
const BUSY_ATTRIBUTE = "aria-busy";
const BUSY_ON = "true";
const BUSY_OFF = "false";

describe("SessionControls", () => {
  it("calls onUndo and onReset for the matching buttons", async () => {
    const onUndo = vi.fn();
    const onReset = vi.fn();
    render(
      <SessionControls
        canUndo
        busy={false}
        onUndo={onUndo}
        onReset={onReset}
        onApplyTtc={vi.fn()}
        onShowExplodedView={vi.fn()}
      />,
    );

    await userEvent.click(screen.getByRole("button", { name: UNDO_LABEL }));
    await userEvent.click(screen.getByRole("button", { name: RESET_LABEL }));

    expect(onUndo).toHaveBeenCalledOnce();
    expect(onReset).toHaveBeenCalledOnce();
  });

  it("calls onApplyTtc for the TTC button, which stays enabled without moves", async () => {
    const onApplyTtc = vi.fn();
    render(
      <SessionControls
        canUndo={false}
        busy={false}
        onUndo={vi.fn()}
        onReset={vi.fn()}
        onApplyTtc={onApplyTtc}
        onShowExplodedView={vi.fn()}
      />,
    );

    await userEvent.click(screen.getByRole("button", { name: TTC_LABEL }));

    expect(onApplyTtc).toHaveBeenCalledOnce();
  });

  it("disables Undo when there is nothing to undo and keeps Reset enabled", () => {
    render(
      <SessionControls
        canUndo={false}
        busy={false}
        onUndo={vi.fn()}
        onReset={vi.fn()}
        onApplyTtc={vi.fn()}
        onShowExplodedView={vi.fn()}
      />,
    );

    expect(screen.getByRole("button", { name: UNDO_LABEL })).toBeDisabled();
    expect(screen.getByRole("button", { name: RESET_LABEL })).toBeEnabled();
  });

  it("does not take focus on first render", () => {
    render(
      <SessionControls
        canUndo={false}
        busy={false}
        onUndo={vi.fn()}
        onReset={vi.fn()}
        onApplyTtc={vi.fn()}
        onShowExplodedView={vi.fn()}
      />,
    );

    expect(screen.getByRole("button", { name: RESET_LABEL })).not.toHaveFocus();
  });

  it("moves focus from Undo to Reset when Undo becomes disabled while focused", () => {
    const { rerender } = render(
      <SessionControls
        canUndo
        busy={false}
        onUndo={vi.fn()}
        onReset={vi.fn()}
        onApplyTtc={vi.fn()}
        onShowExplodedView={vi.fn()}
      />,
    );
    screen.getByRole("button", { name: UNDO_LABEL }).focus();

    rerender(
      <SessionControls
        canUndo={false}
        busy={false}
        onUndo={vi.fn()}
        onReset={vi.fn()}
        onApplyTtc={vi.fn()}
        onShowExplodedView={vi.fn()}
      />,
    );

    expect(screen.getByRole("button", { name: RESET_LABEL })).toHaveFocus();
  });

  it("leaves focus alone when Undo becomes disabled while another control has focus", () => {
    const { rerender } = render(
      <>
        <SessionControls
          canUndo
          busy={false}
          onUndo={vi.fn()}
          onReset={vi.fn()}
          onApplyTtc={vi.fn()}
          onShowExplodedView={vi.fn()}
        />
        <button type="button">{OTHER_LABEL}</button>
      </>,
    );
    const other = screen.getByRole("button", { name: OTHER_LABEL });
    other.focus();

    rerender(
      <>
        <SessionControls
          canUndo={false}
          busy={false}
          onUndo={vi.fn()}
          onReset={vi.fn()}
          onApplyTtc={vi.fn()}
          onShowExplodedView={vi.fn()}
        />
        <button type="button">{OTHER_LABEL}</button>
      </>,
    );

    expect(other).toHaveFocus();
  });

  it("is a group named Session", () => {
    render(
      <SessionControls
        canUndo
        busy={false}
        onUndo={vi.fn()}
        onReset={vi.fn()}
        onApplyTtc={vi.fn()}
        onShowExplodedView={vi.fn()}
      />,
    );

    expect(screen.getByRole("group", { name: SESSION_CONTROLS_LABEL })).toBeInTheDocument();
  });

  it("calls onShowExplodedView for the exploded view button", async () => {
    const onShowExplodedView = vi.fn();
    render(
      <SessionControls
        canUndo={false}
        busy={false}
        onUndo={vi.fn()}
        onReset={vi.fn()}
        onApplyTtc={vi.fn()}
        onShowExplodedView={onShowExplodedView}
      />,
    );

    await userEvent.click(screen.getByRole("button", { name: SHOW_EXPLODED_VIEW_LABEL }));

    expect(onShowExplodedView).toHaveBeenCalledOnce();
  });

  it("shows no busy cue while idle", () => {
    render(
      <SessionControls
        canUndo
        busy={false}
        onUndo={vi.fn()}
        onReset={vi.fn()}
        onApplyTtc={vi.fn()}
        onShowExplodedView={vi.fn()}
      />,
    );

    expect(screen.getByRole("group", { name: SESSION_CONTROLS_LABEL })).toHaveAttribute(
      BUSY_ATTRIBUTE,
      BUSY_OFF,
    );
    expect(screen.queryByText(BUSY_LABEL)).not.toBeInTheDocument();
  });

  it("shows a busy cue and keeps the buttons enabled while a change is running", () => {
    render(
      <SessionControls
        canUndo
        busy
        onUndo={vi.fn()}
        onReset={vi.fn()}
        onApplyTtc={vi.fn()}
        onShowExplodedView={vi.fn()}
      />,
    );

    expect(screen.getByRole("group", { name: SESSION_CONTROLS_LABEL })).toHaveAttribute(
      BUSY_ATTRIBUTE,
      BUSY_ON,
    );
    expect(screen.getByText(BUSY_LABEL)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: UNDO_LABEL })).toBeEnabled();
    expect(screen.getByRole("button", { name: RESET_LABEL })).toBeEnabled();
    expect(screen.getByRole("button", { name: TTC_LABEL })).toBeEnabled();
  });
});
