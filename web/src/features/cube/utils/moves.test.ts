import { MOVE_TOKENS, moveForKey } from "./moves";

const EXPECTED_TOKENS = ["F", "F'", "R", "R'", "U", "U'", "B", "B'", "L", "L'", "D", "D'"];
const CLOCKWISE_KEYS = ["F", "R", "U", "B", "L", "D"];
const COUNTER_CLOCKWISE_KEYS = ["f", "r", "u", "b", "l", "d"];
const NOT_MOVE_KEYS = ["x", "Enter", " ", "Shift", "ArrowUp", "1"];

describe("MOVE_TOKENS", () => {
  it("lists the twelve tokens in button order", () => {
    expect(MOVE_TOKENS).toEqual(EXPECTED_TOKENS);
  });
});

describe("moveForKey", () => {
  it.each(CLOCKWISE_KEYS)("maps %s to the clockwise move", (key) => {
    expect(moveForKey(key)).toBe(key);
  });

  it.each(COUNTER_CLOCKWISE_KEYS)("maps %s to the counter-clockwise move", (key) => {
    expect(moveForKey(key)).toBe(`${key.toUpperCase()}'`);
  });

  it.each(NOT_MOVE_KEYS)("maps %s to no move", (key) => {
    expect(moveForKey(key)).toBeNull();
  });
});
