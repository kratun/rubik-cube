import { render, screen, within } from "@testing-library/react";
import { HISTORY_HEADING, MoveHistory, NO_MOVES_MESSAGE } from "./MoveHistory";

const MOVES = ["F", "R'", "U"];
const REPEATED_MOVES = ["F", "F"];

describe("MoveHistory", () => {
  it("lists the moves in the order given", () => {
    render(<MoveHistory moves={MOVES} />);

    const items = within(screen.getByRole("list")).getAllByRole("listitem");
    expect(items.map((item) => item.textContent)).toEqual(MOVES);
    expect(screen.queryByText(NO_MOVES_MESSAGE)).not.toBeInTheDocument();
  });

  it("lists a repeated move once per occurrence", () => {
    render(<MoveHistory moves={REPEATED_MOVES} />);

    expect(screen.getAllByRole("listitem")).toHaveLength(REPEATED_MOVES.length);
  });

  it("says there are no moves yet for an empty session", () => {
    render(<MoveHistory moves={[]} />);

    expect(screen.getByText(NO_MOVES_MESSAGE)).toBeInTheDocument();
    expect(screen.queryByRole("list")).not.toBeInTheDocument();
  });

  it("can be focused so a long list can be scrolled with the keyboard", () => {
    render(<MoveHistory moves={MOVES} />);

    expect(screen.getByRole("list", { name: HISTORY_HEADING })).toHaveAttribute("tabindex", "0");
  });

  it("is a region named by its heading", () => {
    render(<MoveHistory moves={[]} />);

    expect(screen.getByRole("region", { name: HISTORY_HEADING })).toBeInTheDocument();
  });
});
