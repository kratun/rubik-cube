import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MovePad } from "./MovePad";

const EXPECTED_LABELS = ["F", "F'", "R", "R'", "U", "U'", "B", "B'", "L", "L'", "D", "D'"];
const CLOCKWISE_FRONT = "F";
const COUNTER_CLOCKWISE_UP = "U'";

describe("MovePad", () => {
  it("shows twelve buttons in the order F F' R R' U U' B B' L L' D D'", () => {
    render(<MovePad onMove={vi.fn()} />);

    const labels = screen.getAllByRole("button").map((button) => button.textContent);
    expect(labels).toEqual(EXPECTED_LABELS);
  });

  it("passes the token of a clicked button", async () => {
    const onMove = vi.fn();
    render(<MovePad onMove={onMove} />);

    await userEvent.click(screen.getByRole("button", { name: COUNTER_CLOCKWISE_UP }));

    expect(onMove).toHaveBeenCalledExactlyOnceWith(COUNTER_CLOCKWISE_UP);
  });

  it("can be reached with Tab and pressed with Enter", async () => {
    const onMove = vi.fn();
    render(<MovePad onMove={onMove} />);

    await userEvent.tab();
    await userEvent.keyboard("{Enter}");

    expect(screen.getByRole("button", { name: CLOCKWISE_FRONT })).toHaveFocus();
    expect(onMove).toHaveBeenCalledExactlyOnceWith(CLOCKWISE_FRONT);
  });
});
