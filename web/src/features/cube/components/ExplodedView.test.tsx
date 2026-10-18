import { useState } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CLOSE_EXPLODED_VIEW_LABEL, EXPLODED_VIEW_HEADING, ExplodedView } from "./ExplodedView";
import { SOLVED_EXPLODED_VIEW } from "../fixtures/solvedCubeFixture";

const LINE_BREAK = "\n";
const OPENER_LABEL = "Opener";

// Stands in for the control that opens the dialog, so focus handling can be checked.
function Harness() {
  const [open, setOpen] = useState(false);
  return (
    <>
      <button
        type="button"
        onClick={() => {
          setOpen(true);
        }}
      >
        {OPENER_LABEL}
      </button>
      <ExplodedView
        lines={SOLVED_EXPLODED_VIEW}
        open={open}
        onClose={() => {
          setOpen(false);
        }}
      />
    </>
  );
}

async function openDialog() {
  await userEvent.click(screen.getByRole("button", { name: OPENER_LABEL }));
  return screen.getByRole("dialog", { name: EXPLODED_VIEW_HEADING });
}

describe("ExplodedView", () => {
  it("shows nothing while it is closed", () => {
    render(<ExplodedView lines={SOLVED_EXPLODED_VIEW} open={false} onClose={vi.fn()} />);

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("shows the lines unchanged in a dialog and focuses Close", async () => {
    render(<Harness />);

    const dialog = await openDialog();

    expect(dialog.querySelector("pre")?.textContent).toBe(SOLVED_EXPLODED_VIEW.join(LINE_BREAK));
    expect(screen.getByRole("button", { name: CLOSE_EXPLODED_VIEW_LABEL })).toHaveFocus();
  });

  it("closes with the Close button and gives the focus back to the opener", async () => {
    render(<Harness />);
    await openDialog();

    await userEvent.click(screen.getByRole("button", { name: CLOSE_EXPLODED_VIEW_LABEL }));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: OPENER_LABEL })).toHaveFocus();
  });

  it("closes with Escape", async () => {
    render(<Harness />);
    await openDialog();

    await userEvent.keyboard("{Escape}");

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("closes when the backdrop is clicked but not when the dialog itself is", async () => {
    render(<Harness />);
    const dialog = await openDialog();

    await userEvent.click(dialog);
    expect(screen.getByRole("dialog")).toBeInTheDocument();

    await userEvent.click(dialog.parentElement as HTMLElement);
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("keeps the focus on Close when Tab is pressed", async () => {
    render(<Harness />);
    await openDialog();

    await userEvent.tab();

    expect(screen.getByRole("button", { name: CLOSE_EXPLODED_VIEW_LABEL })).toHaveFocus();
  });
});
