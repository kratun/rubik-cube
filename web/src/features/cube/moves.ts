// The six face letters of the move notation; the API accepts only the standard tokens (AD-9).
export const FACE_LETTERS: readonly string[] = ["F", "R", "U", "B", "L", "D"];
export const COUNTER_CLOCKWISE_SUFFIX = "'";

/** The twelve move tokens in button order: F F' R R' U U' B B' L L' D D'. */
export const MOVE_TOKENS: readonly string[] = FACE_LETTERS.flatMap((letter) => [
  letter,
  `${letter}${COUNTER_CLOCKWISE_SUFFIX}`,
]);

/** An upper-case face letter is the clockwise move, a lower-case one the counter-clockwise move. */
export function moveForKey(key: string): string | null {
  const letter = key.toUpperCase();
  if (!FACE_LETTERS.includes(letter)) {
    return null;
  }
  return key === letter ? letter : `${letter}${COUNTER_CLOCKWISE_SUFFIX}`;
}
