import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

// The conformance level of the audit: WCAG 2.1 A and AA (the 2.0 rules are part of the 2.1 tags).
const WCAG_TAGS = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"];
const NO_VIOLATIONS: unknown[] = [];

const ROOT_PATH = "/";
const UNKNOWN_CUBE_PATH = "/cubes/00000000-0000-0000-0000-000000000000";
const MOVES_ROUTE = "**/api/cubes/*/moves";
const MOVE_BUTTON_NAME = "F";
const MOVE_PAD_GROUP = "Moves";
const SHOW_EXPLODED_VIEW_BUTTON = "Show exploded view";
const CLOSE_BUTTON = "Close";
const EXPLODED_VIEW_DIALOG = "Exploded view";
const BUSY_MESSAGE = "Working…";
const NOT_FOUND_ALERT = /not found/i;

const REFLOW_WIDTHS_PX = [320, 640];
const REFLOW_HEIGHT_PX = 640;
const SCROLLBAR_TOLERANCE_PX = 1;

async function expectNoViolations(page: Page) {
  const results = await new AxeBuilder({ page }).withTags(WCAG_TAGS).analyze();
  expect(results.violations).toEqual(NO_VIOLATIONS);
}

test("a new cube has no WCAG 2.1 AA violations", async ({ page }) => {
  await page.goto(ROOT_PATH);
  await expect(page.getByRole("group", { name: MOVE_PAD_GROUP })).toBeVisible();

  await expectNoViolations(page);
});

test("a cube with moves has no WCAG 2.1 AA violations", async ({ page }) => {
  await page.goto(ROOT_PATH);
  await page.getByRole("button", { name: MOVE_BUTTON_NAME, exact: true }).click();
  await expect(page.getByRole("listitem")).toHaveCount(1);

  await expectNoViolations(page);
});
test("the busy state has no WCAG 2.1 AA violations", async ({ page }) => {
  await page.goto(ROOT_PATH);
  // The request is held until the scan is done, so the busy state is on screen for the whole scan.
  let releaseMoves: () => void = () => undefined;
  const movesHeld = new Promise<void>((resolve) => {
    releaseMoves = resolve;
  });
  await page.route(MOVES_ROUTE, async (route) => {
    await movesHeld;
    await route.continue();
  });
  await page.getByRole("button", { name: MOVE_BUTTON_NAME, exact: true }).click();
  await expect(page.getByRole("status").filter({ hasText: BUSY_MESSAGE })).toBeVisible();

  await expectNoViolations(page);

  releaseMoves();
  await expect(page.getByRole("status").filter({ hasText: BUSY_MESSAGE })).toHaveCount(0);
});

test("the Exploded view dialog has no violations and is keyboard operable", async ({ page }) => {
  await page.goto(ROOT_PATH);
  const opener = page.getByRole("button", { name: SHOW_EXPLODED_VIEW_BUTTON });
  await opener.focus();
  await page.keyboard.press("Enter");
  const dialog = page.getByRole("dialog", { name: EXPLODED_VIEW_DIALOG });
  await expect(dialog).toBeVisible();
  await expect(dialog.getByRole("button", { name: CLOSE_BUTTON })).toBeFocused();

  await expectNoViolations(page);
  await page.keyboard.press("Tab");
  await expect(dialog.getByRole("button", { name: CLOSE_BUTTON })).toBeFocused();
  await page.keyboard.press("Escape");

  await expect(dialog).toBeHidden();
  await expect(opener).toBeFocused();
});

test("an unknown cube shows an accessible error", async ({ page }) => {
  await page.goto(UNKNOWN_CUBE_PATH);
  await expect(page.getByRole("alert")).toHaveText(NOT_FOUND_ALERT);

  await expectNoViolations(page);
});

for (const width of REFLOW_WIDTHS_PX) {
  test(`the page reflows at ${String(width)} px without horizontal scrolling`, async ({ page }) => {
    await page.setViewportSize({ width, height: REFLOW_HEIGHT_PX });
    await page.goto(ROOT_PATH);
    await page.getByRole("button", { name: MOVE_BUTTON_NAME, exact: true }).click();
    await expect(page.getByRole("listitem")).toHaveCount(1);

    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );

    expect(overflow).toBeLessThanOrEqual(SCROLLBAR_TOLERANCE_PX);
  });
}
