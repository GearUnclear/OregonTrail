#!/usr/bin/env python3
"""
80-column text checker for the Oregon Trail game.

The game's TUI (src/Program.cs) redraws a fixed-width console grid every
frame: each rendered line gets PadRight(Console.WindowWidth) and is written
raw, with no wrapping applied by WolfCurses or the renderer. A string literal
whose *rendered* line is longer than the console width doesn't get truncated
-- it wraps at the terminal level and desyncs the next frame's cursor-based
redraw. Some files hand-wrap prose at ~58-76 chars (see GameIntro.cs,
TomHollandEncounter.cs); most (esp. src/Event/**) don't wrap at all.

This script statically scans C# string literals (regular, verbatim, and
interpolated) for lines exceeding a column width and reports them so they can
be manually re-wrapped. It does not modify any files.

Usage:
    python3 tools/check_text_width.py [--dir src] [--width 80] [--ext .cs]
                                       [--preview 90] [--limit N]
"""
import argparse
import os
import sys

ESCAPE_MAP = {
    'n': '\n', 't': '\t', 'r': '\r', '"': '"', '\\': '\\',
    '0': '\0', "'": "'", 'a': '\a', 'b': '\b', 'f': '\f', 'v': '\v',
}


def scan_file(path, width):
    """Yield (line_no, length, text, approximate) for over-width rendered lines."""
    try:
        text = path.read_text(encoding='utf-8', errors='replace')
    except OSError:
        return
    n = len(text)
    i = 0
    line = 1

    while i < n:
        c = text[i]

        # -- comments --
        if c == '/' and i + 1 < n and text[i + 1] == '/':
            j = text.find('\n', i)
            if j == -1:
                break
            i = j  # leave the '\n' itself to the newline handler below
            continue
        if c == '/' and i + 1 < n and text[i + 1] == '*':
            j = text.find('*/', i + 2)
            if j == -1:
                i = n
                break
            line += text.count('\n', i, j + 2)
            i = j + 2
            continue

        if c == '\n':
            line += 1
            i += 1
            continue

        # -- char literal, e.g. 'x', '\n', '\'' --
        if c == "'":
            i += 1
            if i < n and text[i] == '\\':
                i += 2
            else:
                i += 1
            if i < n and text[i] == "'":
                i += 1
            continue

        # -- string literal prefixes --
        verbatim = False
        interp = False
        if text[i:i + 3] in ('$@"', '@$"'):
            verbatim, interp = True, True
            start_line = line
            i += 3
        elif text[i:i + 2] == '@"':
            verbatim = True
            start_line = line
            i += 2
        elif text[i:i + 2] == '$"':
            interp = True
            start_line = line
            i += 2
        elif c == '"':
            start_line = line
            i += 1
        else:
            i += 1
            continue

        decoded = []
        approximate = False
        closed = False
        while i < n:
            ch = text[i]

            if verbatim and ch == '"':
                if i + 1 < n and text[i + 1] == '"':
                    decoded.append('"')
                    i += 2
                    continue
                i += 1
                closed = True
                break

            if not verbatim and ch == '\\':
                nxt = text[i + 1] if i + 1 < n else ''
                if nxt in ESCAPE_MAP:
                    decoded.append(ESCAPE_MAP[nxt])
                    i += 2
                    continue
                if nxt in ('u', 'x', 'U'):
                    # variable-length hex escape; approximate as one rendered char
                    j = i + 2
                    hexdigits = 8 if nxt == 'U' else 4 if nxt == 'u' else 4
                    k = j
                    while k < n and k < j + hexdigits and text[k] in '0123456789abcdefABCDEF':
                        k += 1
                    decoded.append('?')
                    i = k
                    continue
                decoded.append(nxt)
                i += 2
                continue

            if not verbatim and ch == '"':
                i += 1
                closed = True
                break

            if not verbatim and ch == '\n':
                # unterminated on this line; bail out of the literal
                break

            if interp and ch == '{':
                if i + 1 < n and text[i + 1] == '{':
                    decoded.append('{')
                    i += 2
                    continue
                # interpolation hole -- copy through verbatim, track brace depth
                depth = 1
                hole_start = i
                i += 1
                while i < n and depth > 0:
                    hc = text[i]
                    if hc == '{':
                        depth += 1
                    elif hc == '}':
                        depth -= 1
                        if depth == 0:
                            break
                    i += 1
                hole = text[hole_start:i + 1] if i < n else text[hole_start:i]
                i += 1
                inner = hole[1:-1].strip() if len(hole) >= 2 else hole
                inner = inner.split(':', 1)[0].split(',', 1)[0].strip()
                if inner == 'Environment.NewLine':
                    # this hole evaluates to an actual newline at runtime --
                    # treat it as one so line-splitting below stays accurate.
                    decoded.append('\n')
                else:
                    decoded.append(hole)
                    approximate = True
                continue

            if interp and ch == '}':
                if i + 1 < n and text[i + 1] == '}':
                    decoded.append('}')
                    i += 2
                    continue
                i += 1
                continue

            decoded.append(ch)
            i += 1

        content = ''.join(decoded)
        line += content.count('\n')
        if not closed:
            continue

        for offset, seg in enumerate(content.split('\n')):
            if len(seg) > width:
                yield (start_line + offset, len(seg), seg, approximate)


def iter_source_files(root, ext):
    for dirpath, _dirnames, filenames in os.walk(root):
        for name in filenames:
            if name.endswith(ext):
                yield os.path.join(dirpath, name)


def main():
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument('--dir', default='src', help='directory to scan (default: src)')
    parser.add_argument('--width', type=int, default=80, help='max column width (default: 80)')
    parser.add_argument('--ext', default='.cs', help='file extension to scan (default: .cs)')
    parser.add_argument('--preview', type=int, default=90, help='preview chars to print per finding')
    parser.add_argument('--limit', type=int, default=0, help='max findings to print, 0 = all (default: 0)')
    args = parser.parse_args()

    findings = []
    for path in sorted(iter_source_files(args.dir, args.ext)):
        for line_no, length, seg, approx in scan_file(__import__('pathlib').Path(path), args.width):
            findings.append((length, path, line_no, seg, approx))

    findings.sort(key=lambda f: f[0], reverse=True)

    shown = findings if args.limit == 0 else findings[:args.limit]
    for length, path, line_no, seg, approx in shown:
        preview = seg.strip()
        if len(preview) > args.preview:
            preview = preview[:args.preview] + '...'
        tag = ' (~interpolated, length approximate)' if approx else ''
        print(f'[{length:>4}] {path}:{line_no}{tag}')
        print(f'        {preview}')

    files_affected = len({p for _, p, *_ in findings})
    print()
    if findings:
        print(f'{len(findings)} line(s) over {args.width} columns across {files_affected} file(s) '
              f'(longest: {findings[0][0]}).')
    else:
        print(f'No string literals over {args.width} columns found under {args.dir}.')

    return 1 if findings else 0


if __name__ == '__main__':
    sys.exit(main())
