#!/usr/bin/env python3
"""Makes the two room markers (ticket 3.2) and the A4 print sheet. Our own design, nothing reused.

Run:  python3 hardware/room-markers/make_markers.py
Needs: Pillow (pip install pillow).

Output (all committed, so nobody has to run this):
  hardware/room-markers/marker-a.png, marker-b.png   1024 x 1024 px, printed 150 mm wide
  hardware/room-markers/room-markers-a4.pdf          two A4 pages at 300 dpi, one marker + a 10 cm scale bar each
  hardware/room-markers/room-markers-a4-page1.png, -page2.png   the same pages as pictures
  app/Assets/Scripts/Room/RoomFrame/Resources/RoomFrame/marker_a.bytes, marker_b.bytes
      the same PNG bytes; the app loads these (a .bytes file skips Unity's texture compression, which ARCore cannot read)

The pattern is fixed by the seeds below: running the script again gives the same pattern.
If you change the pattern, print and stick new markers and calibrate again.
"""
import os
import random
import shutil
from PIL import Image, ImageDraw, ImageFont

SIZE = 1024            # marker picture, pixels
MARKER_MM = 150        # printed width, black edge to black edge (RoomMarkers.WidthMeters in the app)
DPI = 300
HERE = os.path.dirname(os.path.abspath(__file__))
APP_RES = os.path.join(HERE, "..", "..", "app", "Assets", "Scripts", "Room", "RoomFrame", "Resources", "RoomFrame")


def font(px):
    for path in ("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
                 "/usr/share/fonts/dejavu/DejaVuSans-Bold.ttf",
                 "C:/Windows/Fonts/arialbd.ttf"):
        if os.path.exists(path):
            return ImageFont.truetype(path, px)
    return ImageFont.load_default()


def make_marker(letter, seed):
    """Many overlapping shapes of different sizes and greys: lots of corners, no repeats, strong contrast."""
    rnd = random.Random(seed)
    img = Image.new("L", (SIZE, SIZE), 255)
    d = ImageDraw.Draw(img)
    greys = [0, 0, 0, 40, 90, 150, 200, 255, 255]
    # Big shapes first, small ones on top.
    for count, lo, hi in ((14, 220, 420), (40, 110, 220), (110, 45, 110), (160, 22, 45)):
        for _ in range(count):
            w, h = rnd.randint(lo, hi), rnd.randint(lo, hi)
            x, y = rnd.randint(-w // 3, SIZE - 2 * w // 3), rnd.randint(-h // 3, SIZE - 2 * h // 3)
            g = rnd.choice(greys)
            kind = rnd.random()
            if kind < 0.55:
                d.rectangle([x, y, x + w, y + h], fill=g)
            elif kind < 0.80:
                pts = [(x + rnd.randint(0, w), y + rnd.randint(0, h)) for _ in range(3)]
                d.polygon(pts, fill=g)
            elif kind < 0.92:
                d.ellipse([x, y, x + w, y + h], fill=g)
            else:
                d.line([x, y, x + w, y + h], fill=g, width=rnd.randint(8, 20))
    # Big letter so a person can tell the markers apart (white box, black letter).
    box = 250
    bx, by = (60, 60) if letter == "A" else (SIZE - 60 - box, SIZE - 60 - box)
    d.rectangle([bx, by, bx + box, by + box], fill=255, outline=0, width=10)
    f = font(210)
    tb = d.textbbox((0, 0), letter, font=f)
    d.text((bx + (box - (tb[2] - tb[0])) // 2 - tb[0], by + (box - (tb[3] - tb[1])) // 2 - tb[1]), letter, fill=0, font=f)
    # Black frame: the 150 mm is measured from its outer edge to its outer edge.
    d.rectangle([0, 0, SIZE - 1, SIZE - 1], outline=0, width=14)
    return img


def mm(v):
    return int(round(v * DPI / 25.4))


def make_page(marker, letter):
    page = Image.new("L", (mm(210), mm(297)), 255)
    d = ImageDraw.Draw(page)
    small, tiny = font(mm(5)), font(mm(3.5))
    d.text((mm(30), mm(16)), "Room marker " + letter + "  -  DIY VR headset, ticket 3.2", fill=0, font=small)
    d.text((mm(30), mm(25)), "Print at 100 % (\"actual size\", NOT \"fit to page\").", fill=0, font=tiny)
    d.text((mm(30), mm(31)), "The square must be 150 mm wide. This title is above the marker's top edge.", fill=0, font=tiny)
    side = mm(MARKER_MM)
    page.paste(marker.resize((side, side), Image.NEAREST), (mm(30), mm(40)))
    d.text((mm(30), mm(193)), "Stick it flat on the wall, letter upright, not behind glass.", fill=0, font=tiny)
    # 10 cm scale bar with 1 cm ticks.
    x0, y0 = mm(30), mm(215)
    d.rectangle([x0, y0, x0 + mm(100), y0 + mm(1)], fill=0)
    for i in range(11):
        h = mm(6) if i in (0, 5, 10) else mm(3.5)
        d.rectangle([x0 + mm(10 * i) - 2, y0 - h, x0 + mm(10 * i) + 2, y0 + mm(1)], fill=0)
    d.text((x0, y0 + mm(4)), "0", fill=0, font=tiny)
    d.text((x0 + mm(46), y0 + mm(4)), "5 cm", fill=0, font=tiny)
    d.text((x0 + mm(93), y0 + mm(4)), "10 cm", fill=0, font=tiny)
    d.text((x0, y0 + mm(12)), "Check with a ruler: this bar must be exactly 10 cm long (100 mm).", fill=0, font=tiny)
    d.text((x0, y0 + mm(18)), "If it is shorter or longer, print again at 100 % scale.", fill=0, font=tiny)
    return page


def main():
    os.makedirs(APP_RES, exist_ok=True)
    pages = []
    for letter, seed in (("A", 320001), ("B", 320002)):
        marker = make_marker(letter, seed)
        png = os.path.join(HERE, "marker-%s.png" % letter.lower())
        marker.save(png, optimize=True)
        shutil.copyfile(png, os.path.join(APP_RES, "marker_%s.bytes" % letter.lower()))
        page = make_page(marker, letter)
        page.save(os.path.join(HERE, "room-markers-a4-page%d.png" % (len(pages) + 1)), optimize=True, dpi=(DPI, DPI))
        pages.append(page)
    pages[0].save(os.path.join(HERE, "room-markers-a4.pdf"), save_all=True, append_images=pages[1:], resolution=DPI)
    # Small previews for checking by eye (not committed if run with PREVIEW_DIR unset).
    prev = os.environ.get("PREVIEW_DIR")
    if prev:
        pages[0].resize((620, 877)).save(os.path.join(prev, "p1.png"))
        Image.open(os.path.join(HERE, "marker-b.png")).resize((512, 512)).save(os.path.join(prev, "b.png"))


if __name__ == "__main__":
    main()
