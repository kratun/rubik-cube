import { render, screen } from "@testing-library/react";
import { jsonResponse } from "../features/cube/testing/apiResponseFixture";
import { faceLabel } from "../features/cube/components/CubeView";
import { SOLVED_CUBE } from "../features/cube/testing/solvedCubeFixture";
import { APP_TITLE, App } from "./App";
import { ERROR_FALLBACK_MESSAGE } from "./ErrorBoundary";

const FIRST_FACE = "up";

afterEach(() => {
  vi.restoreAllMocks();
});

describe("App", () => {
  it("shows the application title as the main heading", () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(SOLVED_CUBE));

    render(<App />);

    expect(screen.getByRole("heading", { level: 1, name: APP_TITLE })).toBeInTheDocument();
  });

  it("shows the cube under the title", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(SOLVED_CUBE));

    render(<App />);

    expect(await screen.findByRole("group", { name: faceLabel(FIRST_FACE) })).toBeInTheDocument();
  });

  it("keeps the title and shows the fallback when the page crashes", async () => {
    const brokenCube = { ...SOLVED_CUBE, explodedView: undefined };
    vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(brokenCube));
    vi.spyOn(console, "error").mockImplementation(() => undefined);

    render(<App />);

    expect(await screen.findByRole("alert")).toHaveTextContent(ERROR_FALLBACK_MESSAGE);
    expect(screen.getByRole("heading", { level: 1, name: APP_TITLE })).toBeInTheDocument();
  });
});
