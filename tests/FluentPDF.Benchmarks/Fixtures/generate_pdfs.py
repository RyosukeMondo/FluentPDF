#!/usr/bin/env python3
"""
Generate benchmark PDF fixtures for FluentPDF performance testing.
Creates 4 types of PDFs: text-heavy, image-heavy, vector-graphics, complex-layout.
Each PDF is 3-5 pages and < 5MB.
"""

from reportlab.lib.pagesizes import letter, A4
from reportlab.lib.units import inch
from reportlab.lib import colors
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, Image, PageBreak
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_JUSTIFY, TA_CENTER
from reportlab.pdfgen import canvas
from reportlab.lib.utils import ImageReader
from reportlab.graphics.shapes import Drawing, Rect, Circle, Line, Polygon, String
from reportlab.graphics import renderPDF
from PIL import Image as PILImage
import io
import os

# Sample text for text-heavy PDFs
LOREM_IPSUM = """Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation
ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit
in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat
non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."""

def generate_text_heavy_pdf():
    """Generate a text-heavy PDF (plain text document, 5 pages)."""
    filename = "text-heavy.pdf"
    doc = SimpleDocTemplate(filename, pagesize=letter)
    styles = getSampleStyleSheet()
    story = []

    # Title
    title_style = ParagraphStyle(
        'CustomTitle',
        parent=styles['Heading1'],
        fontSize=24,
        textColor=colors.HexColor('#1a1a1a'),
        spaceAfter=30,
        alignment=TA_CENTER
    )
    story.append(Paragraph("Performance Benchmarking Documentation", title_style))
    story.append(Spacer(1, 0.2*inch))

    # Body text style
    body_style = ParagraphStyle(
        'CustomBody',
        parent=styles['BodyText'],
        fontSize=11,
        alignment=TA_JUSTIFY,
        spaceAfter=12
    )

    # Generate 5 pages of text
    sections = [
        "Introduction to Performance Testing",
        "Methodology and Approach",
        "Benchmark Suite Overview",
        "Results and Analysis",
        "Conclusions and Recommendations"
    ]

    for i, section in enumerate(sections):
        # Section heading
        story.append(Paragraph(f"<b>{i+1}. {section}</b>", styles['Heading2']))
        story.append(Spacer(1, 0.1*inch))

        # Add multiple paragraphs per section
        for _ in range(8):
            story.append(Paragraph(LOREM_IPSUM, body_style))

        if i < len(sections) - 1:
            story.append(PageBreak())

    doc.build(story)
    print(f"✓ Generated {filename} ({os.path.getsize(filename)} bytes)")

def generate_image_heavy_pdf():
    """Generate an image-heavy PDF (photo gallery, 4 pages)."""
    filename = "image-heavy.pdf"
    c = canvas.Canvas(filename, pagesize=letter)
    width, height = letter

    # Generate synthetic images using PIL
    def create_gradient_image(color1, color2, size=(400, 300)):
        """Create a gradient image."""
        img = PILImage.new('RGB', size)
        pixels = img.load()
        for y in range(size[1]):
            for x in range(size[0]):
                r = int(color1[0] + (color2[0] - color1[0]) * (x / size[0]))
                g = int(color1[1] + (color2[1] - color1[1]) * (y / size[1]))
                b = int(color1[2] + (color2[2] - color1[2]) * (x / size[0]))
                pixels[x, y] = (r, g, b)
        return img

    # Create 4 pages with 4 images each
    colors_list = [
        ((255, 0, 0), (0, 0, 255)),    # Red to Blue
        ((0, 255, 0), (255, 255, 0)),  # Green to Yellow
        ((128, 0, 128), (255, 192, 203)),  # Purple to Pink
        ((255, 165, 0), (0, 255, 255))  # Orange to Cyan
    ]

    for page in range(4):
        c.setFont("Helvetica-Bold", 18)
        c.drawString(50, height - 50, f"Image Gallery - Page {page + 1}")

        # Place 4 images on each page (2x2 grid)
        positions = [
            (50, height - 350), (320, height - 350),
            (50, height - 650), (320, height - 650)
        ]

        for i, (x, y) in enumerate(positions):
            # Create gradient image
            color_idx = (page * 4 + i) % len(colors_list)
            img = create_gradient_image(colors_list[color_idx][0], colors_list[color_idx][1])

            # Save to bytes
            img_buffer = io.BytesIO()
            img.save(img_buffer, format='JPEG', quality=85)
            img_buffer.seek(0)

            # Draw image using ImageReader
            img_reader = ImageReader(img_buffer)
            c.drawImage(img_reader, x, y, width=250, height=200, preserveAspectRatio=True)

            # Add caption
            c.setFont("Helvetica", 10)
            c.drawString(x, y - 15, f"Image {page * 4 + i + 1}")

        c.showPage()

    c.save()
    print(f"✓ Generated {filename} ({os.path.getsize(filename)} bytes)")

def generate_vector_graphics_pdf():
    """Generate a vector graphics PDF (technical diagrams, 3 pages)."""
    filename = "vector-graphics.pdf"
    c = canvas.Canvas(filename, pagesize=letter)
    width, height = letter

    # Page 1: Flowchart
    c.setFont("Helvetica-Bold", 18)
    c.drawString(50, height - 50, "Flowchart Diagram")

    def draw_box(x, y, w, h, text):
        c.rect(x, y, w, h, stroke=1, fill=0)
        c.setFont("Helvetica", 10)
        text_width = c.stringWidth(text, "Helvetica", 10)
        c.drawString(x + (w - text_width) / 2, y + h / 2 - 5, text)

    def draw_arrow(x1, y1, x2, y2):
        c.line(x1, y1, x2, y2)
        # Simple arrow head
        c.line(x2, y2, x2 - 5, y2 + 5)
        c.line(x2, y2, x2 + 5, y2 + 5)

    # Draw flowchart
    box_y = height - 150
    draw_box(250, box_y, 100, 40, "Start")
    draw_arrow(300, box_y - 10, 300, box_y - 40)

    box_y -= 90
    draw_box(200, box_y, 200, 40, "Initialize System")
    draw_arrow(300, box_y - 10, 300, box_y - 40)

    box_y -= 90
    draw_box(200, box_y, 200, 40, "Load Document")
    draw_arrow(300, box_y - 10, 300, box_y - 40)

    box_y -= 90
    draw_box(200, box_y, 200, 40, "Render Page")
    draw_arrow(300, box_y - 10, 300, box_y - 40)

    box_y -= 90
    draw_box(250, box_y, 100, 40, "End")

    c.showPage()

    # Page 2: Architecture diagram with shapes
    c.setFont("Helvetica-Bold", 18)
    c.drawString(50, height - 50, "Architecture Diagram")

    # Draw layered architecture
    layers = [
        ("Presentation Layer", 150, colors.lightblue),
        ("Business Logic Layer", 250, colors.lightgreen),
        ("Data Access Layer", 350, colors.lightyellow),
        ("Database", 450, colors.lightcoral)
    ]

    for layer_name, y_offset, color in layers:
        c.setFillColor(color)
        c.rect(100, height - y_offset, 400, 60, stroke=1, fill=1)
        c.setFillColor(colors.black)
        c.setFont("Helvetica-Bold", 12)
        text_width = c.stringWidth(layer_name, "Helvetica-Bold", 12)
        c.drawString(300 - text_width / 2, height - y_offset + 25, layer_name)

    c.showPage()

    # Page 3: Component diagram with circles and connections
    c.setFont("Helvetica-Bold", 18)
    c.drawString(50, height - 50, "Component Diagram")

    def draw_circle_with_label(x, y, r, label):
        c.circle(x, y, r, stroke=1, fill=0)
        c.setFont("Helvetica", 9)
        text_width = c.stringWidth(label, "Helvetica", 9)
        c.drawString(x - text_width / 2, y - 4, label)

    # Draw components
    components = [
        (150, height - 200, "UI"),
        (350, height - 200, "Controller"),
        (550, height - 200, "Service"),
        (250, height - 400, "Repository"),
        (450, height - 400, "Cache")
    ]

    for x, y, label in components:
        draw_circle_with_label(x, y, 40, label)

    # Draw connections
    c.setDash(1, 2)
    c.line(190, height - 200, 310, height - 200)  # UI to Controller
    c.line(390, height - 200, 510, height - 200)  # Controller to Service
    c.line(330, height - 230, 270, height - 360)  # Controller to Repository
    c.line(530, height - 230, 470, height - 360)  # Service to Cache

    c.save()
    print(f"✓ Generated {filename} ({os.path.getsize(filename)} bytes)")

def generate_complex_layout_pdf():
    """Generate a complex layout PDF (magazine-style, 4 pages)."""
    filename = "complex-layout.pdf"
    doc = SimpleDocTemplate(filename, pagesize=A4)
    styles = getSampleStyleSheet()
    story = []

    # Custom styles
    title_style = ParagraphStyle(
        'MagazineTitle',
        parent=styles['Heading1'],
        fontSize=28,
        textColor=colors.HexColor('#2c3e50'),
        spaceAfter=20,
        alignment=TA_CENTER,
        fontName='Helvetica-Bold'
    )

    subtitle_style = ParagraphStyle(
        'MagazineSubtitle',
        parent=styles['Heading2'],
        fontSize=16,
        textColor=colors.HexColor('#7f8c8d'),
        spaceAfter=15,
        alignment=TA_CENTER
    )

    heading_style = ParagraphStyle(
        'ArticleHeading',
        parent=styles['Heading2'],
        fontSize=14,
        textColor=colors.HexColor('#e74c3c'),
        spaceAfter=10,
        fontName='Helvetica-Bold'
    )

    body_style = ParagraphStyle(
        'ArticleBody',
        parent=styles['BodyText'],
        fontSize=10,
        alignment=TA_JUSTIFY,
        spaceAfter=10
    )

    # Page 1: Cover page
    story.append(Spacer(1, 2*inch))
    story.append(Paragraph("FLUENTPDF MAGAZINE", title_style))
    story.append(Paragraph("Performance & Optimization Edition", subtitle_style))
    story.append(Spacer(1, 1*inch))

    # Table of contents
    toc_data = [
        ['Section', 'Page'],
        ['Rendering Performance', '2'],
        ['Memory Management', '3'],
        ['Benchmarking Best Practices', '4']
    ]

    toc_table = Table(toc_data, colWidths=[4*inch, 1*inch])
    toc_table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#34495e')),
        ('TEXTCOLOR', (0, 0), (-1, 0), colors.whitesmoke),
        ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
        ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
        ('FONTSIZE', (0, 0), (-1, 0), 12),
        ('BOTTOMPADDING', (0, 0), (-1, 0), 12),
        ('BACKGROUND', (0, 1), (-1, -1), colors.beige),
        ('GRID', (0, 0), (-1, -1), 1, colors.black)
    ]))
    story.append(toc_table)
    story.append(PageBreak())

    # Page 2-4: Articles with tables and mixed content
    articles = [
        ("Rendering Performance", "Understanding PDF rendering performance characteristics."),
        ("Memory Management", "Optimizing memory usage in document processing."),
        ("Benchmarking Best Practices", "Effective strategies for performance measurement.")
    ]

    for article_title, article_subtitle in articles:
        story.append(Paragraph(article_title, heading_style))
        story.append(Paragraph(f"<i>{article_subtitle}</i>", subtitle_style))
        story.append(Spacer(1, 0.2*inch))

        # Add paragraphs
        for _ in range(4):
            story.append(Paragraph(LOREM_IPSUM, body_style))

        # Add data table
        data = [
            ['Metric', 'Target', 'Actual', 'Status'],
            ['P99 Latency', '< 1s', '0.8s', 'PASS'],
            ['Memory Usage', '< 200MB', '150MB', 'PASS'],
            ['Cold Start', '< 2s', '1.5s', 'PASS']
        ]

        table = Table(data, colWidths=[1.5*inch, 1.2*inch, 1.2*inch, 1*inch])
        table.setStyle(TableStyle([
            ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#3498db')),
            ('TEXTCOLOR', (0, 0), (-1, 0), colors.whitesmoke),
            ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
            ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, 0), 10),
            ('BOTTOMPADDING', (0, 0), (-1, 0), 8),
            ('BACKGROUND', (0, 1), (-1, -1), colors.lightgrey),
            ('GRID', (0, 0), (-1, -1), 1, colors.black),
            ('FONTSIZE', (0, 1), (-1, -1), 9)
        ]))
        story.append(Spacer(1, 0.2*inch))
        story.append(table)
        story.append(Spacer(1, 0.2*inch))

        # Add more text
        for _ in range(3):
            story.append(Paragraph(LOREM_IPSUM, body_style))

        story.append(PageBreak())

    doc.build(story)
    print(f"✓ Generated {filename} ({os.path.getsize(filename)} bytes)")

if __name__ == "__main__":
    print("Generating benchmark PDF fixtures...")
    print()

    generate_text_heavy_pdf()
    generate_image_heavy_pdf()
    generate_vector_graphics_pdf()
    generate_complex_layout_pdf()

    print()
    print("All fixtures generated successfully!")

    # Print summary
    print("\nFile sizes:")
    for filename in ["text-heavy.pdf", "image-heavy.pdf", "vector-graphics.pdf", "complex-layout.pdf"]:
        if os.path.exists(filename):
            size_kb = os.path.getsize(filename) / 1024
            print(f"  {filename}: {size_kb:.1f} KB")
