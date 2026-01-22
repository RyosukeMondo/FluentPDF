#!/usr/bin/env python3
"""
Creates a test PDF with various annotations for annotation marshaling validation.
Requires: pip install reportlab
"""

from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import letter
from reportlab.lib.colors import red, yellow, green
import os

def create_annotations_pdf():
    output_path = os.path.join(os.path.dirname(__file__), "annotations.pdf")

    # Create PDF with annotations
    c = canvas.Canvas(output_path, pagesize=letter)
    width, height = letter

    # Page 1: Basic annotations
    c.setFont("Helvetica", 14)
    c.drawString(100, height - 100, "Annotation Test PDF")
    c.setFont("Helvetica", 10)
    c.drawString(100, height - 130, "This PDF contains various annotations for testing FS_QUADPOINTSF and FS_RECTF marshaling.")

    # Draw some rectangles that could be annotation targets
    c.rect(100, height - 200, 200, 50, stroke=1, fill=0)
    c.drawString(110, height - 180, "Text Highlight Area 1")

    c.rect(100, height - 300, 400, 80, stroke=1, fill=0)
    c.drawString(110, height - 270, "Large annotation area for quad points testing")
    c.drawString(110, height - 285, "Multiple lines of text for markup annotations")

    # Draw rectangles with various sizes for rect testing
    # Normal rectangle
    c.setStrokeColor(green)
    c.rect(100, height - 400, 100, 50, stroke=1, fill=0)
    c.drawString(110, height - 380, "Normal Rect")

    # Very small rectangle (near zero-size)
    c.setStrokeColor(red)
    c.rect(250, height - 400, 5, 5, stroke=1, fill=0)
    c.drawString(260, height - 400, "Tiny Rect (5x5)")

    # Large rectangle
    c.setStrokeColor(yellow)
    c.rect(100, height - 550, 400, 120, stroke=1, fill=0)
    c.drawString(110, height - 490, "Large Rectangle Area (400x120)")

    # Add text content
    c.setStrokeColor(red)
    c.setFont("Helvetica", 8)
    c.drawString(100, height - 600, "Edge Cases:")
    c.drawString(100, height - 615, "- Negative coordinates (handled by PDF coordinate system)")
    c.drawString(100, height - 630, "- Large coordinate values (near page boundaries)")
    c.drawString(100, height - 645, "- Zero-size rectangles (single point)")
    c.drawString(100, height - 660, "- NaN and infinity values (tested programmatically)")

    c.showPage()

    # Page 2: More complex annotation areas
    c.setFont("Helvetica", 12)
    c.drawString(100, height - 100, "Page 2: Complex Annotation Scenarios")

    # Rotated text area (for quad points)
    c.saveState()
    c.translate(300, height - 300)
    c.rotate(45)
    c.setStrokeColor(red)
    c.rect(-100, -20, 200, 40, stroke=1, fill=0)
    c.drawString(-90, -5, "Rotated annotation area (45 degrees)")
    c.restoreState()

    # Overlapping rectangles
    c.setStrokeColor(green)
    c.setFillColor(green)
    c.setFillAlpha(0.3)
    c.rect(100, height - 450, 150, 80, stroke=1, fill=1)
    c.setStrokeColor(yellow)
    c.setFillColor(yellow)
    c.rect(175, height - 420, 150, 80, stroke=1, fill=1)
    c.setFillAlpha(1.0)
    c.setFillColor(red)
    c.drawString(120, height - 410, "Overlapping annotations")

    # Edge of page annotations (testing large coordinates)
    c.setStrokeColor(red)
    c.rect(width - 110, 10, 100, 50, stroke=1, fill=0)
    c.drawString(width - 100, 25, "Near edge")

    c.rect(10, 10, 80, 40, stroke=1, fill=0)
    c.drawString(15, 25, "Corner")

    c.showPage()
    c.save()

    print(f"Created annotations test PDF: {output_path}")
    print(f"File size: {os.path.getsize(output_path)} bytes")
    print("Features:")
    print("- 2 pages with various annotation areas")
    print("- Different rectangle sizes (tiny, normal, large)")
    print("- Rotated annotation area (for quad points)")
    print("- Overlapping rectangles")
    print("- Edge and corner rectangles (large coordinate values)")
    print("- Text markup areas for highlight annotations")

if __name__ == "__main__":
    create_annotations_pdf()
