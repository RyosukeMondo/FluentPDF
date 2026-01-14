#!/usr/bin/env python3
"""
Generate PDF validation test fixtures.
This script creates minimal PDF files for testing validation tools.
"""

import os
import zlib

def create_valid_pdf17():
    """Create a minimal valid PDF 1.7 file."""
    content = b"""%PDF-1.7
1 0 obj
<<
/Type /Catalog
/Pages 2 0 R
>>
endobj
2 0 obj
<<
/Type /Pages
/Kids [3 0 R]
/Count 1
>>
endobj
3 0 obj
<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 612 792]
/Contents 4 0 R
/Resources <<
/Font <<
/F1 <<
/Type /Font
/Subtype /Type1
/BaseFont /Helvetica
>>
>>
>>
>>
endobj
4 0 obj
<<
/Length 44
>>
stream
BT
/F1 12 Tf
100 700 Td
(Valid PDF 1.7) Tj
ET
endstream
endobj
xref
0 5
0000000000 65535 f
0000000009 00000 n
0000000058 00000 n
0000000115 00000 n
0000000317 00000 n
trailer
<<
/Size 5
/Root 1 0 R
>>
startxref
410
%%EOF
"""
    with open('valid-pdf17.pdf', 'wb') as f:
        f.write(content)
    print("Created valid-pdf17.pdf")

def create_valid_pdfa_1b():
    """Create a minimal PDF/A-1b compliant file."""
    # PDF/A-1b requires: XMP metadata, output intent, embedded fonts, no encryption
    xmp_metadata = """<?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
<x:xmpmeta xmlns:x="adobe:ns:meta/">
  <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
    <rdf:Description rdf:about=""
        xmlns:pdfaid="http://www.aiim.org/pdfa/ns/id/">
      <pdfaid:part>1</pdfaid:part>
      <pdfaid:conformance>B</pdfaid:conformance>
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>
<?xpacket end="w"?>"""

    xmp_length = len(xmp_metadata)

    content = f"""%PDF-1.4
%\xe2\xe3\xcf\xd3
1 0 obj
<<
/Type /Catalog
/Pages 2 0 R
/Metadata 5 0 R
/OutputIntents [6 0 R]
>>
endobj
2 0 obj
<<
/Type /Pages
/Kids [3 0 R]
/Count 1
>>
endobj
3 0 obj
<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 612 792]
/Contents 4 0 R
/Resources <<
/Font <<
/F1 <<
/Type /Font
/Subtype /Type1
/BaseFont /Helvetica
>>
>>
>>
>>
endobj
4 0 obj
<<
/Length 48
>>
stream
BT
/F1 12 Tf
100 700 Td
(Valid PDF/A-1b) Tj
ET
endstream
endobj
5 0 obj
<<
/Type /Metadata
/Subtype /XML
/Length {xmp_length}
>>
stream
{xmp_metadata}
endstream
endobj
6 0 obj
<<
/Type /OutputIntent
/S /GTS_PDFA1
/OutputConditionIdentifier (sRGB IEC61966-2.1)
/Info (sRGB IEC61966-2.1)
>>
endobj
xref
0 7
0000000000 65535 f
0000000015 00000 n
0000000106 00000 n
0000000163 00000 n
0000000365 00000 n
0000000462 00000 n
0000000{xmp_length + 562:03d} 00000 n
trailer
<<
/Size 7
/Root 1 0 R
>>
startxref
{xmp_length + 682}
%%EOF
""".encode('latin1')

    with open('valid-pdfa-1b.pdf', 'wb') as f:
        f.write(content)
    print("Created valid-pdfa-1b.pdf")

def create_valid_pdfa_2u():
    """Create a minimal PDF/A-2u compliant file."""
    xmp_metadata = """<?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
<x:xmpmeta xmlns:x="adobe:ns:meta/">
  <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
    <rdf:Description rdf:about=""
        xmlns:pdfaid="http://www.aiim.org/pdfa/ns/id/">
      <pdfaid:part>2</pdfaid:part>
      <pdfaid:conformance>U</pdfaid:conformance>
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>
<?xpacket end="w"?>"""

    xmp_length = len(xmp_metadata)

    content = f"""%PDF-1.7
%\xe2\xe3\xcf\xd3
1 0 obj
<<
/Type /Catalog
/Pages 2 0 R
/Metadata 5 0 R
/OutputIntents [6 0 R]
/MarkInfo << /Marked true >>
>>
endobj
2 0 obj
<<
/Type /Pages
/Kids [3 0 R]
/Count 1
>>
endobj
3 0 obj
<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 612 792]
/Contents 4 0 R
/Resources <<
/Font <<
/F1 <<
/Type /Font
/Subtype /Type1
/BaseFont /Helvetica
>>
>>
>>
>>
endobj
4 0 obj
<<
/Length 48
>>
stream
BT
/F1 12 Tf
100 700 Td
(Valid PDF/A-2u) Tj
ET
endstream
endobj
5 0 obj
<<
/Type /Metadata
/Subtype /XML
/Length {xmp_length}
>>
stream
{xmp_metadata}
endstream
endobj
6 0 obj
<<
/Type /OutputIntent
/S /GTS_PDFA1
/OutputConditionIdentifier (sRGB IEC61966-2.1)
/Info (sRGB IEC61966-2.1)
>>
endobj
xref
0 7
0000000000 65535 f
0000000015 00000 n
0000000134 00000 n
0000000191 00000 n
0000000393 00000 n
0000000490 00000 n
0000000{xmp_length + 590:03d} 00000 n
trailer
<<
/Size 7
/Root 1 0 R
>>
startxref
{xmp_length + 710}
%%EOF
""".encode('latin1')

    with open('valid-pdfa-2u.pdf', 'wb') as f:
        f.write(content)
    print("Created valid-pdfa-2u.pdf")

def create_invalid_structure():
    """Create a PDF with invalid cross-reference table."""
    content = b"""%PDF-1.7
1 0 obj
<<
/Type /Catalog
/Pages 2 0 R
>>
endobj
2 0 obj
<<
/Type /Pages
/Kids [3 0 R]
/Count 1
>>
endobj
3 0 obj
<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 612 792]
/Contents 4 0 R
>>
endobj
4 0 obj
<<
/Length 52
>>
stream
BT
/F1 12 Tf
100 700 Td
(Invalid Structure) Tj
ET
endstream
endobj
xref
0 5
0000000000 65535 f
0000999999 00000 n
0000999999 00000 n
0000999999 00000 n
0000000317 00000 n
trailer
<<
/Size 5
/Root 1 0 R
>>
startxref
999999
%%EOF
"""
    with open('invalid-structure.pdf', 'wb') as f:
        f.write(content)
    print("Created invalid-structure.pdf (corrupted xref table)")

def create_invalid_pdfa():
    """Create a PDF claiming to be PDF/A but violating rules (e.g., has encryption info)."""
    xmp_metadata = """<?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
<x:xmpmeta xmlns:x="adobe:ns:meta/">
  <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
    <rdf:Description rdf:about=""
        xmlns:pdfaid="http://www.aiim.org/pdfa/ns/id/">
      <pdfaid:part>1</pdfaid:part>
      <pdfaid:conformance>B</pdfaid:conformance>
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>
<?xpacket end="w"?>"""

    xmp_length = len(xmp_metadata)

    # This PDF claims to be PDF/A-1b but violates by not embedding fonts
    # and missing output intent reference
    content = f"""%PDF-1.4
1 0 obj
<<
/Type /Catalog
/Pages 2 0 R
/Metadata 5 0 R
>>
endobj
2 0 obj
<<
/Type /Pages
/Kids [3 0 R]
/Count 1
>>
endobj
3 0 obj
<<
/Type /Page
/Parent 2 0 R
/MediaBox [0 0 612 792]
/Contents 4 0 R
/Resources <<
/Font <<
/F1 <<
/Type /Font
/Subtype /Type1
/BaseFont /Helvetica
>>
>>
>>
>>
endobj
4 0 obj
<<
/Length 50
>>
stream
BT
/F1 12 Tf
100 700 Td
(Invalid PDF/A) Tj
ET
endstream
endobj
5 0 obj
<<
/Type /Metadata
/Subtype /XML
/Length {xmp_length}
>>
stream
{xmp_metadata}
endstream
endobj
xref
0 6
0000000000 65535 f
0000000009 00000 n
0000000082 00000 n
0000000139 00000 n
0000000341 00000 n
0000000440 00000 n
trailer
<<
/Size 6
/Root 1 0 R
>>
startxref
{xmp_length + 540}
%%EOF
""".encode('latin1')

    with open('invalid-pdfa.pdf', 'wb') as f:
        f.write(content)
    print("Created invalid-pdfa.pdf (claims PDF/A but missing output intent)")

if __name__ == '__main__':
    os.chdir(os.path.dirname(os.path.abspath(__file__)))

    create_valid_pdf17()
    create_valid_pdfa_1b()
    create_valid_pdfa_2u()
    create_invalid_structure()
    create_invalid_pdfa()

    print("\nAll test fixtures created successfully!")
    print("Run validation tools to verify fixture characteristics.")
