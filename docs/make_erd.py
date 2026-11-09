from PIL import Image, ImageDraw, ImageFont
from pathlib import Path

out = Path(__file__).with_name("erd.png")
W, H = 1400, 900
img = Image.new("RGB", (W, H), "#ffffff")
d = ImageDraw.Draw(img)
try:
    font = ImageFont.truetype("arial.ttf", 18)
    font_b = ImageFont.truetype("arialbd.ttf", 20)
    font_t = ImageFont.truetype("arialbd.ttf", 28)
except Exception:
    font = ImageFont.load_default()
    font_b = font
    font_t = font

d.text((40, 30), "Student Marks Management API - ER Diagram", fill="#111827", font=font_t)

boxes = {
    "students": (80, 140, 420, 420),
    "courses": (980, 140, 1320, 400),
    "enrollments": (480, 200, 900, 480),
    "users": (80, 520, 420, 820),
}
fields = {
    "students": ["id PK", "registration_no UK", "full_name", "email UK", "created_at_utc"],
    "courses": ["id PK", "code UK", "title", "credit_hours (1-4)", "created_at_utc"],
    "enrollments": [
        "id PK",
        "student_id FK",
        "course_id FK",
        "mark (null|0-100)",
        "enrolled_at_utc",
        "UK(student_id, course_id)",
    ],
    "users": [
        "id PK",
        "username UK",
        "password_hash",
        "role (Teacher|Student)",
        "student_id FK (nullable)",
    ],
}

for name, (x1, y1, x2, y2) in boxes.items():
    d.rounded_rectangle([x1, y1, x2, y2], radius=12, outline="#1f2937", width=3, fill="#f8fafc")
    d.rectangle([x1, y1, x2, y1 + 44], fill="#1e3a8a")
    d.text((x1 + 16, y1 + 10), name, fill="white", font=font_b)
    y = y1 + 60
    for f in fields[name]:
        d.text((x1 + 16, y), f, fill="#111827", font=font)
        y += 32


def line(a, b):
    d.line([a, b], fill="#334155", width=3)


line((420, 300), (480, 300))
line((900, 300), (980, 280))
d.text((520, 170), "students 1 -- N enrollments N -- 1 courses", fill="#111827", font=font)
line((250, 420), (250, 520))
d.text((270, 470), "optional 1--1 for Student role", fill="#111827", font=font)
d.text(
    (40, 860),
    "Delete on students/courses with enrollments is restricted (409).",
    fill="#374151",
    font=font,
)

img.save(out)
print("ERD_OK", out, out.stat().st_size)
