#!/usr/bin/env python3
"""
Find all .cs files >= 500 lines or files missing a namespace in subdirectories.
"""

import os
import sys
from pathlib import Path


def analyze_file(file_path):
    """
    Count lines and check for 'namespace' keyword.
    Also detect any individual method definitions exceeding 199 lines.
    Returns: (line_count, has_namespace, large_methods)
    where large_methods is a list of (method_name, length, start_line).
    """
    line_count = 0
    has_namespace = False
    large_methods = []

    # simple state for parsing methods
    in_method = False
    brace_depth = 0
    method_start = 0
    method_name = None

    import re
    method_decl = re.compile(r"\b(?:public|private|protected|internal|static|async|\s)+\s+[\w<>\[\]]+\s+(\w+)\s*\([^\)]*\)")

    try:
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            for idx, line in enumerate(f, start=1):
                line_count += 1
                if 'namespace' in line:
                    has_namespace = True

                # if not currently inside a method, look for declaration
                if not in_method:
                    m = method_decl.search(line)
                    if m:
                        in_method = True
                        brace_depth = line.count('{') - line.count('}')
                        method_start = idx
                        method_name = m.group(1)
                        # if brace_depth is zero, we may open on later lines
                else:
                    # already inside method body
                    brace_depth += line.count('{') - line.count('}')
                    if brace_depth <= 0:
                        # method ends at this line
                        length = idx - method_start + 1
                        if length > 199:
                            large_methods.append((method_name or '<unknown>', length, method_start))
                        in_method = False
                        method_name = None
                        brace_depth = 0
        return line_count, has_namespace, large_methods
    except PermissionError:
        return 0, False, []
    except Exception:
        return 0, False, []


def find_problematic_cs_files(root_dir='.', min_lines=500, verbose=False):
    """
    Find .cs files that are too large (by lines), missing a namespace, or contain
    individual methods longer than 199 lines.
    """
    large_files = []
    missing_namespace_files = []
    large_methods_report = []
    root_path = Path(root_dir).resolve()
    
    skip_dirs = {'Library', 'Temp', 'Obj', 'Build', 'Builds', '.git', 'node_modules'}
    
    try:
        files_checked = 0
        for cs_file in root_path.rglob('*.cs'):
            if any(skip_dir in cs_file.parts for skip_dir in skip_dirs):
                continue
            
            if not cs_file.is_file():
                continue
            
            files_checked += 1
            if verbose and files_checked % 100 == 0:
                print(f"Checked {files_checked} files...", end='\r')
            
            line_count, has_namespace, methods = analyze_file(cs_file)
            
            try:
                rel_path = str(cs_file.relative_to(root_path))
            except ValueError:
                rel_path = str(cs_file)

            # Criteria 1: Too many lines
            if line_count >= min_lines:
                if 'Editor' in rel_path:
                    continue
                large_files.append((rel_path, line_count))
            
            # Criteria 2: Missing namespace
            if not has_namespace:
                missing_namespace_files.append(rel_path)

            # Criteria 3: Large methods
            for name, length, start in methods:
                large_methods_report.append((rel_path, name, length, start))
        
        if verbose:
            print(f"Checked {files_checked} files total.    ")
            
    except KeyboardInterrupt:
        print("\nSearch interrupted by user.")
        sys.exit(1)
    
    return large_files, missing_namespace_files, large_methods_report


def main():
    ROOT_DIR = '.' 
    MIN_LINES = 400
    VERBOSE = True 
    
    print(f"Searching '{ROOT_DIR}' for large files (>= {MIN_LINES}) or missing namespaces...")
    print("-" * 80)
    
    large_files, missing_ns, large_methods = find_problematic_cs_files(ROOT_DIR, MIN_LINES, VERBOSE)
    
    # 1. Print files missing namespaces
    if missing_ns:
        print(f"\n[!] MISSING NAMESPACE ({len(missing_ns)} files):")
        for path in sorted(missing_ns):
            print(f"  MISSING: {path}")
    else:
        print("\n[✓] All files have a namespace.")

    # 2. Print large files
    if large_files:
        large_files.sort(key=lambda x: x[1], reverse=True)
        print(f"\n[!] LARGE FILES (>= {MIN_LINES} lines):")
        for file_path, line_count in large_files:
            print(f"  {line_count:5d} lines: {file_path}")
    else:
        print(f"\n[✓] No files exceed {MIN_LINES} lines.")

    # 3. Print long methods
    if large_methods:
        print(f"\n[!] LONG METHODS (>199 lines):")
        for file_path, name, length, start in sorted(large_methods, key=lambda x: (-x[2], x[0])):
            print(f"  {length:4d} lines starting at {start} in {file_path}: {name}()")
    else:
        print("\n[✓] No methods exceed 199 lines.")

    print("-" * 80)


if __name__ == '__main__':
    main()