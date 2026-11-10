# Room markers (ticket 3.2)

Two printed pictures that the phone recognises. Stuck to the wall, they tell the app where the room is, the same way on every start.

| File | What it is |
|---|---|
| `room-markers-a4.pdf` | Print this. Two A4 pages: marker A and marker B, each with a 10 cm check bar |
| `room-markers-a4-page1.png`, `-page2.png` | The same two pages as pictures, if the PDF will not print |
| `marker-a.png`, `marker-b.png` | The marker pictures alone (1024 x 1024 px) |
| `make_markers.py` | The script that made all of the above. You do not need to run it |

Each marker must be **150 mm wide** (black edge to black edge). The app is built for exactly that size.

## Print and stick

1. Open `room-markers-a4.pdf` and print both pages on plain white matt A4 paper. In the print window choose **Actual size** or **100 %** (not "Fit to page").
2. Put a ruler on the bar under each marker. It must be exactly 10 cm long. If not, print again at 100 %.
3. Cut out each square along the outside of its black frame (a small white border is fine).
4. Stick both flat on a wall at about chest to eye height, letter upright, 1 to 2 m apart. Two walls of the same corner work too. Not behind glass, not on a door, not where a lamp shines straight on them.
5. Do not move them afterwards. If one moves, calibrate again in the app (hold D-pad LEFT for 1 second while looking at both).

How you know it worked: the bar measures 10 cm, each square measures 15 cm, and neither marker is wrinkled or curled.

Where the markers hang is private room data: do not put photos or measurements of them into this repository.
