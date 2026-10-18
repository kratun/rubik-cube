import { expect, test, type Page } from "@playwright/test";

// What axe cannot measure: the contrast of graphics and borders (1.4.11) and of text while a change runs (1.4.3).
// The values are read from the computed styles, so a misspelt custom property or an edited colour fails here.
const MIN_GRAPHIC_CONTRAST = 3;
const MIN_TEXT_CONTRAST = 4.5;

const ROOT_PATH = "/";
const MOVES_ROUTE = "**/api/cubes/*/moves";
const MOVE_BUTTON_NAME = "F";
const BUSY_MESSAGE = "Working…";
const TILE_COLOURS = ["White", "Yellow", "Orange", "Red", "Green", "Blue"];

const TRANSPARENT = "rgba(0, 0, 0, 0)";
const NO_SHADOW = "none";
const RGB_PATTERN = /rgba?\((\d+),\s*(\d+),\s*(\d+)/;
const HEX_RADIX = 16;
const HEX_DIGITS_PER_CHANNEL = 2;
const MAX_CHANNEL = 255;
const SRGB_LINEAR_THRESHOLD = 0.03928;
const SRGB_LINEAR_DIVISOR = 12.92;
const SRGB_GAMMA_OFFSET = 0.055;
const SRGB_GAMMA_DIVISOR = 1.055;
const SRGB_GAMMA_EXPONENT = 2.4;
const LUMINANCE_WEIGHTS = [0.2126, 0.7152, 0.0722];
const CONTRAST_FLARE = 0.05;

type Rgb = [number, number, number];

function parseColour(value: string): Rgb {
  const rgb = RGB_PATTERN.exec(value);
  if (rgb !== null) {
    return [Number(rgb[1]), Number(rgb[2]), Number(rgb[3])];
  }
  const hex = value.trim().replace("#", "");
  return [0, 1, 2].map((channel) =>
    parseInt(
      hex.slice(channel * HEX_DIGITS_PER_CHANNEL, (channel + 1) * HEX_DIGITS_PER_CHANNEL),
      HEX_RADIX,
    ),
  ) as Rgb;
}

function luminance(colour: Rgb): number {
  const [red, green, blue] = colour.map((channel) => {
    const value = channel / MAX_CHANNEL;
    return value <= SRGB_LINEAR_THRESHOLD
      ? value / SRGB_LINEAR_DIVISOR
      : ((value + SRGB_GAMMA_OFFSET) / SRGB_GAMMA_DIVISOR) ** SRGB_GAMMA_EXPONENT;
  }) as Rgb;
  const [redWeight, greenWeight, blueWeight] = LUMINANCE_WEIGHTS as Rgb;
  return redWeight * red + greenWeight * green + blueWeight * blue;
}

function contrast(first: Rgb, second: Rgb): number {
  const [lighter, darker] = [luminance(first), luminance(second)].sort((a, b) => b - a) as [
    number,
    number,
  ];
  return (lighter + CONTRAST_FLARE) / (darker + CONTRAST_FLARE);
}

function blend(foreground: Rgb, background: Rgb, opacity: number): Rgb {
  return foreground.map((channel, index) =>
    Math.round(channel * opacity + (background[index] ?? 0) * (1 - opacity)),
  ) as Rgb;
}

async function styleOf(page: Page, selector: string, property: string): Promise<string> {
  return page
    .locator(selector)
    .first()
    .evaluate((element, name) => getComputedStyle(element).getPropertyValue(name), property);
}

async function customProperty(page: Page, name: string): Promise<Rgb> {
  const value = await page.evaluate(
    (property) => getComputedStyle(document.documentElement).getPropertyValue(property),
    name,
  );
  return parseColour(value);
}

test("every tile colour is painted and its edge stands out from the board", async ({ page }) => {
  await page.goto(ROOT_PATH);
  await page.locator(".tile").first().waitFor();
  const board = parseColour(await styleOf(page, ".cube-net", "background-color"));

  for (const colour of TILE_COLOURS) {
    const tile = `.tile[data-colour="${colour}"]`;
    expect(await styleOf(page, tile, "background-color"), `${colour} is painted`).not.toBe(
      TRANSPARENT,
    );
    const shadow = await styleOf(page, tile, "box-shadow");
    expect(shadow, `${colour} has a ring`).not.toBe(NO_SHADOW);
    expect(
      contrast(parseColour(shadow), board),
      `${colour} ring against the board`,
    ).toBeGreaterThanOrEqual(MIN_GRAPHIC_CONTRAST);
  }
});

test("the border of a control stands out from the page", async ({ page }) => {
  await page.goto(ROOT_PATH);
  const pageColour = parseColour(await styleOf(page, "body", "background-color"));
  const border = parseColour(
    await page
      .getByRole("button", { name: MOVE_BUTTON_NAME, exact: true })
      .evaluate((element) => getComputedStyle(element).borderTopColor),
  );

  expect(contrast(border, pageColour)).toBeGreaterThanOrEqual(MIN_GRAPHIC_CONTRAST);
});

test("while a change runs, muted text and the focus ring keep their contrast", async ({ page }) => {
  await page.goto(ROOT_PATH);
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

  const pageColour = parseColour(await styleOf(page, "body", "background-color"));
  const opacity = Number(await styleOf(page, '[aria-busy="true"]', "opacity"));
  const mutedText = parseColour(await styleOf(page, '[aria-busy="true"] .group-label', "color"));
  const focusRing = await customProperty(page, "--colour-focus");

  expect(contrast(blend(mutedText, pageColour, opacity), pageColour)).toBeGreaterThanOrEqual(
    MIN_TEXT_CONTRAST,
  );
  expect(contrast(blend(focusRing, pageColour, opacity), pageColour)).toBeGreaterThanOrEqual(
    MIN_GRAPHIC_CONTRAST,
  );
  releaseMoves();
});
