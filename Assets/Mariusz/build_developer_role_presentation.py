from pathlib import Path

import build_sherlock_presentation as base
from reportlab.pdfgen import canvas


OUTPUT = base.ROOT / "output" / "pdf" / "Sherlock_Holmes_My_Developer_Role.pdf"


def create_presentation(images):
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    c = canvas.Canvas(str(OUTPUT), pagesize=(base.PAGE_W, base.PAGE_H))
    c.setTitle("My Role in Sherlock Holmes - Diploma Game Project")
    c.setAuthor("Warsaw Film School")

    # 1. Title
    base.draw_cover_image(c, images["house"], 0, 0, base.PAGE_W, base.PAGE_H, focus_y=0.28)
    c.setFillColor(base.HexColor("#081010"))
    c.setFillAlpha(0.72)
    c.rect(0, 0, base.PAGE_W, base.PAGE_H, fill=1, stroke=0)
    c.setFillAlpha(1)
    c.setFillColor(base.GOLD)
    c.rect(48, 82, 112, 5, fill=1, stroke=0)
    base.text(c, "MY ROLE IN", 48, 336, 15, color=base.GOLD, font="Helvetica-Bold")
    base.text(c, "SHERLOCK HOLMES", 48, 286, 37, color=base.PAPER, font="Helvetica-Bold")
    base.text(c, "Game Development", 48, 240, 37, color=base.PAPER, font="Helvetica-Bold")
    base.text(c, "Warsaw Film School", 48, 55, 10, color=base.PAPER, font="Helvetica-Bold")
    c.showPage()

    # 2. Team and project
    base.title(c, 2, "Context", "The Project and the Team", "A four-person diploma project with shared creative and technical responsibilities.")
    base.bullets(c, [
        "We are creating a stylised 3D point-and-click adventure inspired by Sherlock Holmes.",
        "Each member of our four-person team develops a different part of the game.",
        "My work connects level design, puzzles, 3D characters and the player's feeling of immersion.",
    ], 48, 327, 425)
    base.draw_cover_image(c, images["overview"], 520, 98, 390, 330, focus_y=0.28)
    c.setFillColor(base.PANEL)
    c.roundRect(520, 62, 390, 30, 4, fill=1, stroke=0)
    base.text(c, "My goal: make the player feel like a detective.", 538, 73, 10.5, color=base.MUTED, font="Helvetica")
    c.showPage()

    # 3. Creative responsibilities
    base.title(c, 3, "Creative Work", "How I Build the World")
    cards = [
        ("01", "Level Design", "I plan rooms, routes, pacing and the order in which players discover information."),
        ("02", "Environment Design", "I use props, lighting and architecture to make every location communicate a story."),
        ("03", "3D Character Design", "I create and adapt characters that match the game's stylised visual direction."),
    ]
    for index, (number, heading, body) in enumerate(cards):
        y = 323 - index * 92
        c.setFillColor(base.PANEL)
        c.roundRect(48, y, 420, 73, 5, fill=1, stroke=0)
        base.text(c, number, 68, y + 43, 10, color=base.GOLD, font="Helvetica-Bold")
        base.text(c, heading, 108, y + 41, 16, color=base.PAPER, font="Helvetica-Bold")
        base.paragraph(c, body, 108, y + 20, 330, size=10.8, color=base.MUTED, leading=14)
    base.draw_cover_image(c, images["characters"], 510, 112, 400, 305, focus_y=0.2)
    c.showPage()

    # 4. Technical and systemic responsibilities
    base.title(c, 4, "Playable Systems", "Making the World Respond")
    cards = [
        ("Locomotion & Animation", "I set up movement, navigation and animation feedback so characters feel present in the scene."),
        ("Story & Environmental Puzzles", "I connect narrative events with mechanics, clues and physical puzzles in the environment."),
        ("Camera, Light & Visual Effects", "I use camera work, light and effects to direct attention and build detective atmosphere."),
    ]
    for index, (heading, body) in enumerate(cards):
        x = 48 + (index % 2) * 430
        y = 285 - (index // 2) * 157
        width = 385 if index < 2 else 815
        c.setFillColor(base.PANEL)
        c.roundRect(x, y, width, 125, 5, fill=1, stroke=0)
        base.text(c, heading, x + 22, y + 84, 16, color=base.PAPER, font="Helvetica-Bold")
        base.paragraph(c, body, x + 22, y + 52, width - 44, size=12.5, color=base.MUTED, leading=17)
    c.showPage()

    # 5. Design approach
    base.title(c, 5, "Design Goal", "Making the Player Feel Like a Detective")
    base.draw_cover_image(c, images["detective"], 48, 94, 420, 324, focus_y=0.34)
    base.bullets(c, [
        "I use Detective Vision and the magnifying glass to turn observation into an active part of gameplay.",
        "I design clues as part of the environment, so the player can notice, inspect and connect them.",
        "I make each puzzle serve the story: it should explain a character, a place or an event.",
    ], 508, 335, 360, size=14.5, leading=21)
    c.setFillColor(base.RED)
    c.roundRect(508, 82, 352, 40, 4, fill=1, stroke=0)
    base.text(c, "The player should investigate, not simply click through the game.", 525, 96, 10.5, color=base.PAPER, font="Helvetica-Bold")
    c.showPage()

    # 6. Conclusion
    base.title(c, 6, "Conclusion", "What I Have Learned")
    base.draw_cover_image(c, images["basement"], 520, 82, 390, 345, focus_y=0.28)
    base.bullets(c, [
        "Game development has two connected parts: gameplay and story. The player experience is strongest when they support each other.",
        "The most important and difficult part is teamwork: keeping the team engaged, communicating clearly and connecting everyone's work.",
        "A strong puzzle needs a clear rule, visual feedback and a story reason to exist.",
    ], 48, 328, 415, size=15, leading=22)
    base.text(c, "Thank you for your attention.", 48, 77, 21, color=base.GOLD, font="Helvetica-Bold")
    base.text(c, "I will be happy to answer any questions.", 48, 51, 12, color=base.MUTED, font="Helvetica")
    c.showPage()

    c.save()


if __name__ == "__main__":
    create_presentation(base.make_assets())
    print(OUTPUT)
