from pathlib import Path

from PIL import Image, ImageEnhance
from reportlab.lib.colors import HexColor
from reportlab.pdfbase.pdfmetrics import stringWidth
from reportlab.pdfgen import canvas


ROOT = Path(__file__).parent
SCREENSHOTS = Path(r"C:\Users\mkles\Pictures\Screenshots")
ASSETS = ROOT / "tmp" / "sherlock_presentation_assets"
OUTPUT = ROOT / "output" / "pdf" / "Sherlock_Holmes_English_Presentation.pdf"

PAGE_W, PAGE_H = 13.333 * 72, 7.5 * 72

INK = HexColor("#101617")
PANEL = HexColor("#182123")
PAPER = HexColor("#F3F0E6")
MUTED = HexColor("#B8C2BC")
GOLD = HexColor("#D3A94D")
MOSS = HexColor("#7FA488")
RED = HexColor("#A65344")


IMAGE_SPECS = {
    "house": ("Zrzut ekranu 2026-07-26 200244.png", 76, 26, 0.92),
    "wide_house": ("Zrzut ekranu 2026-07-26 193420.png", 76, 26, 0.96),
    "table": ("Zrzut ekranu 2026-07-26 201725.png", 76, 26, 0.98),
    "detective": ("Zrzut ekranu 2026-07-26 141715.png", 70, 12, 0.95),
    "characters": ("Zrzut ekranu 2026-07-26 135528.png", 70, 12, 0.98),
    "overview": ("Zrzut ekranu 2026-07-26 131739.png", 70, 12, 0.96),
    "basement": ("Zrzut ekranu 2026-08-05 023851.png", 0, 0, 1.02),
}


def make_assets():
    ASSETS.mkdir(parents=True, exist_ok=True)
    output = {}
    for key, (name, top, bottom, brightness) in IMAGE_SPECS.items():
        image = Image.open(SCREENSHOTS / name).convert("RGB")
        width, height = image.size
        image = image.crop((0, top, width, height - bottom))
        image = ImageEnhance.Brightness(image).enhance(brightness)
        path = ASSETS / f"{key}.jpg"
        image.save(path, quality=91, optimize=True)
        output[key] = path
    return output


def cover_crop(image_path, box_ratio, focus_y=0.5):
    image = Image.open(image_path)
    width, height = image.size
    image_ratio = width / height
    if image_ratio > box_ratio:
        crop_width = int(height * box_ratio)
        left = (width - crop_width) // 2
        return image.crop((left, 0, left + crop_width, height))
    crop_height = int(width / box_ratio)
    top = int((height - crop_height) * focus_y)
    top = max(0, min(top, height - crop_height))
    return image.crop((0, top, width, top + crop_height))


def draw_cover_image(c, image_path, x, y, width, height, focus_y=0.5):
    crop = cover_crop(image_path, width / height, focus_y)
    temp_path = ASSETS / f"crop_{Path(image_path).stem}_{int(width)}_{int(height)}_{int(focus_y * 100)}.jpg"
    crop.save(temp_path, quality=90, optimize=True)
    c.drawImage(str(temp_path), x, y, width=width, height=height, mask="auto")


def text(c, value, x, y, size, color=PAPER, font="Helvetica", leading=None):
    c.setFont(font, size)
    c.setFillColor(color)
    c.drawString(x, y, value)


def wrap_lines(c, value, font, size, max_width):
    words = value.split()
    lines, current = [], ""
    for word in words:
        trial = word if not current else f"{current} {word}"
        if stringWidth(trial, font, size) <= max_width:
            current = trial
        else:
            lines.append(current)
            current = word
    if current:
        lines.append(current)
    return lines


def paragraph(c, value, x, y, width, size=16, color=MUTED, leading=22, font="Helvetica"):
    c.setFont(font, size)
    c.setFillColor(color)
    for line in wrap_lines(c, value, font, size, width):
        c.drawString(x, y, line)
        y -= leading
    return y


def bullets(c, items, x, y, width, size=15.5, color=PAPER, accent=GOLD, leading=25):
    for item in items:
        c.setFillColor(accent)
        c.circle(x + 4, y + 4, 3, fill=1, stroke=0)
        next_y = paragraph(c, item, x + 20, y, width - 20, size=size, color=color, leading=leading)
        y = next_y - 8
    return y


def slide_frame(c, number, section):
    c.setFillColor(INK)
    c.rect(0, 0, PAGE_W, PAGE_H, fill=1, stroke=0)
    c.setFillColor(GOLD)
    c.rect(0, PAGE_H - 9, PAGE_W, 9, fill=1, stroke=0)
    text(c, section.upper(), 48, PAGE_H - 38, 9, color=MOSS, font="Helvetica-Bold")
    text(c, f"0{number}", PAGE_W - 72, 26, 10, color=MUTED, font="Helvetica-Bold")
    text(c, "WARSAW FILM SCHOOL  /  MULTIMEDIA STUDIES", 48, 26, 8.5, color=MUTED, font="Helvetica")


def title(c, number, section, heading, subheading=None):
    slide_frame(c, number, section)
    text(c, heading, 48, PAGE_H - 88, 30, color=PAPER, font="Helvetica-Bold")
    if subheading:
        paragraph(c, subheading, 48, PAGE_H - 118, 440, size=14.5, color=MUTED, leading=19)


def create_presentation(images):
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    c = canvas.Canvas(str(OUTPUT), pagesize=(PAGE_W, PAGE_H))
    c.setTitle("Sherlock Holmes - A 3D Point-and-Click Adventure")
    c.setAuthor("Warsaw Film School - Multimedia Studies")

    # 1. Title
    draw_cover_image(c, images["house"], 0, 0, PAGE_W, PAGE_H, focus_y=0.28)
    c.setFillColor(HexColor("#081010"))
    c.setFillAlpha(0.72)
    c.rect(0, 0, PAGE_W, PAGE_H, fill=1, stroke=0)
    c.setFillAlpha(1)
    c.setFillColor(GOLD)
    c.rect(48, 82, 112, 5, fill=1, stroke=0)
    text(c, "SHERLOCK HOLMES", 48, 336, 15, color=GOLD, font="Helvetica-Bold")
    text(c, "A 3D Point-and-Click", 48, 286, 37, color=PAPER, font="Helvetica-Bold")
    text(c, "Adventure Game", 48, 240, 37, color=PAPER, font="Helvetica-Bold")
    text(c, "Warsaw Film School", 48, 55, 10, color=PAPER, font="Helvetica-Bold")
    c.showPage()

    # 2. The project
    title(c, 2, "My Role", "The Project and My Role", "I focus on level design, puzzles, 3D characters and the player's feeling of immersion.")
    bullets(c, [
        "We are a four-person team, and each person is responsible for a different part of development.",
        "My work connects level design, environmental puzzles and 3D character work.",
        "My main goal is to make the player feel like a detective, not only like someone clicking on objects.",
    ], 48, 327, 425)
    draw_cover_image(c, images["overview"], 520, 98, 390, 330, focus_y=0.28)
    c.setFillColor(PANEL)
    c.roundRect(520, 62, 390, 30, 4, fill=1, stroke=0)
    text(c, "I design the house as both a setting and a playable puzzle.", 538, 73, 10.5, color=MUTED, font="Helvetica")
    c.showPage()

    # 3. Responsibilities
    title(c, 3, "Responsibilities", "What I Do", "My work combines creative decisions with technical implementation.")
    responsibilities = [
        ("Level Design", "I plan rooms, routes, pacing and the order in which players discover information."),
        ("Environment Design", "I use props, lighting and architecture to make each location communicate a story."),
        ("3D Character Design", "I create and adapt characters that fit the stylised visual direction of the game."),
        ("Locomotion & Animation", "I set up movement, navigation and animation feedback so characters feel present in the scene."),
        ("Story & Environmental Puzzles", "I connect narrative events with mechanics, clues and physical puzzles in the environment."),
        ("Camera, Light & Visual Effects", "I use camera work, light and effects to direct attention and build detective atmosphere."),
    ]
    for index, (heading, body) in enumerate(responsibilities):
        col = index % 2
        row = index // 2
        x = 48 + col * 435
        y = 337 - row * 102
        text(c, f"0{index + 1}", x, y + 35, 10, color=GOLD, font="Helvetica-Bold")
        text(c, heading, x + 38, y + 34, 15, color=PAPER, font="Helvetica-Bold")
        paragraph(c, body, x + 38, y + 13, 345, size=10.8, color=MUTED, leading=14)
    c.showPage()

    # 4. Story
    title(c, 4, "Narrative Design", "Building a Mystery Inside the House")
    draw_cover_image(c, images["characters"], 48, 88, 415, 330, focus_y=0.24)
    c.setFillColor(PANEL)
    c.roundRect(505, 93, 405, 325, 5, fill=1, stroke=0)
    text(c, "THE PREMISE", 531, 376, 10, color=GOLD, font="Helvetica-Bold")
    paragraph(c, "I use Lady Edith's murder and the search for young Ethel to give every puzzle a narrative purpose.", 531, 340, 330, size=17, color=PAPER, leading=24)
    paragraph(c, "When I design a room, I try to make it reveal one new piece of the story: a witness, an object, a hidden mechanism or a trace.", 531, 248, 330, size=14.5, color=MUTED, leading=20)
    c.showPage()

    # 5. Art direction
    title(c, 5, "Level Design", "Making the House Tell Its Own Story")
    draw_cover_image(c, images["wide_house"], 48, 175, 865, 248, focus_y=0.24)
    bullets(c, [
        "I use warm domestic rooms in contrast with colder, darker spaces below ground.",
        "I place furniture, lighting and props so they can become clues before the player reads dialogue.",
        "In the basement, brick walls, narrow routes and darkness create pressure and curiosity.",
    ], 48, 141, 840, size=13.5, leading=18)
    c.showPage()

    # 6. Mechanics
    title(c, 6, "Puzzle Design", "How I Let the Player Investigate")
    c.setFillColor(PANEL)
    cards = [
        ("01", "Detective Vision", "I use it to reveal traces, interactive objects and hidden evidence."),
        ("02", "Magnifying Glass", "It lets the player examine small details and visual patterns."),
        ("03", "Objects & Locks", "I build simple collection, key and lockpicking interactions."),
        ("04", "Environmental Puzzles", "The player reads the room, operates mechanisms and opens secret routes."),
    ]
    for index, (num, heading, body) in enumerate(cards):
        x = 48 + (index % 2) * 430
        y = 300 - (index // 2) * 155
        c.setFillColor(PANEL)
        c.roundRect(x, y, 385, 125, 5, fill=1, stroke=0)
        text(c, num, x + 20, y + 87, 11, color=GOLD, font="Helvetica-Bold")
        text(c, heading, x + 60, y + 85, 17, color=PAPER, font="Helvetica-Bold")
        paragraph(c, body, x + 20, y + 53, 340, size=12.5, color=MUTED, leading=17)
    draw_cover_image(c, images["detective"], 699, 95, 214, 120, focus_y=0.35)
    c.showPage()

    # 7. Deduction
    title(c, 7, "Deduction", "Designing the Connection of Evidence")
    draw_cover_image(c, images["table"], 48, 93, 420, 325, focus_y=0.24)
    c.setFillColor(GOLD)
    c.setLineWidth(2)
    c.line(536, 332, 626, 332)
    c.line(626, 332, 721, 278)
    c.line(721, 278, 820, 332)
    for x, y, label in [(536, 332, "CLUE"), (626, 332, "FACT"), (721, 278, "EVENT"), (820, 332, "DISCOVERY")]:
        c.setFillColor(MOSS)
        c.circle(x, y, 10, fill=1, stroke=0)
        text(c, label, x - 24, y - 29, 8.5, color=MUTED, font="Helvetica-Bold")
    paragraph(c, "I designed Idea Points so the player discovers evidence and connects it in a logical order.", 505, 198, 365, size=17, color=PAPER, leading=24)
    paragraph(c, "For example, the body, fireplace, window and hidden wall reconstruct the murder scene and reveal a secret mechanism.", 505, 134, 365, size=13.5, color=MUTED, leading=19)
    c.showPage()

    # 8. Watson
    title(c, 8, "Character Switching", "Making Watson Part of the Puzzle")
    draw_cover_image(c, images["characters"], 48, 92, 420, 326, focus_y=0.18)
    bullets(c, [
        "I designed character switching so the player can control both Sherlock and Dr Watson.",
        "Watson can hold mechanisms, move heavy objects and help Sherlock reach a solution.",
        "This turns switching into a meaningful puzzle decision, rather than a cosmetic feature.",
    ], 508, 337, 365, size=14.5, leading=21)
    c.setFillColor(RED)
    c.roundRect(508, 82, 352, 40, 4, fill=1, stroke=0)
    text(c, "Example: two bricks open a secret wall only when both characters cooperate.", 525, 96, 10.5, color=PAPER, font="Helvetica-Bold")
    c.showPage()

    # 9. Production
    title(c, 9, "Development", "Turning My Design Into Playable Systems")
    c.setFillColor(PANEL)
    c.roundRect(48, 105, 390, 305, 5, fill=1, stroke=0)
    text(c, "UNITY WORKFLOW", 75, 365, 11, color=GOLD, font="Helvetica-Bold")
    bullets(c, [
        "I use Unity to build 3D scenes, navigation and interaction logic.",
        "I write and adapt C# scripts for puzzles, character switching and clue progression.",
        "I use Cinemachine, Animator and Visual Effects to improve camera feedback and atmosphere.",
    ], 75, 330, 320, size=14, leading=20)
    draw_cover_image(c, images["house"], 480, 105, 430, 305, focus_y=0.3)
    c.showPage()

    # 10. Conclusion
    title(c, 10, "Conclusion", "What I Have Learned")
    draw_cover_image(c, images["basement"], 520, 82, 390, 345, focus_y=0.28)
    bullets(c, [
        "Game development has two connected parts: gameplay and story. The player experience is strongest when they support each other.",
        "The most important and difficult part is teamwork: keeping the team engaged, communicating clearly and connecting everyone's work.",
        "A strong puzzle needs a clear rule, visual feedback and a story reason to exist.",
    ], 48, 328, 415, size=15, leading=22)
    text(c, "Thank you for your attention.", 48, 77, 21, color=GOLD, font="Helvetica-Bold")
    text(c, "I will be happy to answer any questions.", 48, 51, 12, color=MUTED, font="Helvetica")
    c.showPage()

    c.save()


if __name__ == "__main__":
    create_presentation(make_assets())
    print(OUTPUT)
