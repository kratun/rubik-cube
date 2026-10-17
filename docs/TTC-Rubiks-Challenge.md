# TTC Software Developer – Rubik's Cube Challenge

> Source: `docs/TTC Rubiks Challenge - 2026_.pdf` (2 pages) plus follow-up email notes.
> Page images: [initial-orientation.png](initial-orientation.png) (page 1), [exploded-view.png](exploded-view.png) (page 2).

## Task

Develop a program that simulates a Rubik's cube. The aim is **not to solve** the cube, just to build a
programmatic Rubik's cube that can **correctly rotate any face**.

## Constraints

- Any programming language, but it must be **buildable and runnable on a Windows PC** and must **not require paid software**.
- The submission must include a **README with detailed instructions on how to build and run** the program.
- A UI is **not required**; console output is perfectly acceptable.
- Ideally published to an easily accessible platform such as GitHub (or otherwise easily shareable with the Lead Developers).

## Initial state

- The cube starts **solved**.
- Set up the cube the same way as https://rubiks-cube-solver.com/ :
  - **Green** tiles on the **front**
  - **Red** tiles on the **right-hand side**
  - **White** tiles on the **up** face
  - (By implication: Blue = back, Orange = left, Yellow = down.)

## Test sequence

Starting from the solved position, apply these rotations in order, then print the colours on each face:

| # | Face  | Direction         | Standard notation |
|---|-------|-------------------|-------------------|
| 1 | Front | clockwise 90°     | `F`  |
| 2 | Right | anti-clockwise 90°| `R'` |
| 3 | Up    | clockwise 90°    | `U`  |
| 4 | Back  | anti-clockwise 90°| `B'` |
| 5 | Left  | clockwise 90°    | `L`  |
| 6 | Down  | anti-clockwise 90°| `D'` |

Sequence: `F R' U B' L D'`

The same sequence can be set up on rubiks-cube-solver.com using their UI buttons, in the order above,
which can be used to verify the expected result.

## Output

Print the faces using the **exploded view** shown on PDF page 2 ([exploded-view.png](exploded-view.png)).
The image shows the **expected state after the test sequence** `F R' U B' L D'` (not the solved cube).

### Layout

```
        U U U
        U U U
        U U U
L L L   F F F   R R R   B B B
L L L   F F F   R R R   B B B
L L L   F F F   R R R   B B B
        D D D
        D D D
        D D D
```

Colour key: W white, Y yellow, G green, B blue, R red, O orange.

### Expected output (verified against an independent simulation of the sequence)

```
      R O G
      B W W
      B B B
G Y Y O R R Y B O Y B W
O O G O G W R R W O B Y
B G O W W W O Y R Y Y W
      G G B
      R Y R
      R G G
```

Face orientations in the net follow the standard unfolding: U is seen from above with B at the top edge,
F/R/B/L are seen from outside the cube, and D is seen from below with F at its top edge.

## Evaluation criteria

- Working solution (correct output).
- Code is close to **"production quality"**: readable, maintainable, testable, etc.

## Additional notes from email

### Code quality and organisation
- Separate frontend and backend code clearly.
- Remove all demo code and unnecessary files before submission.

### Functionality and output
- Verify the output matches the requirements exactly.
- Test the solution manually end-to-end before submitting (write tests).

## Personal technology preferences

- Backend: **.NET 10**
- Frontend: **React (latest stable)**

