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
    Returns: (line_count, has_namespace)
    """
    line_count = 0
    has_namespace = False
    try:
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            for line in f:
                line_count += 1
                if 'namespace' in line:
                    has_namespace = True
        return line_count, has_namespace
    except PermissionError:
        return 0, False
    except Exception:
        return 0, False


def find_problematic_cs_files(root_dir='.', min_lines=500, verbose=False):
    """
    Find .cs files that are too large OR missing a namespace.
    """
    large_files = []
    missing_namespace_files = []
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
            
            line_count, has_namespace = analyze_file(cs_file)
            
            try:
                rel_path = str(cs_file.relative_to(root_path))
            except ValueError:
                rel_path = str(cs_file)

            # Criteria 1: Too many lines
            if line_count >= min_lines:
                large_files.append((rel_path, line_count))
            
            # Criteria 2: Missing namespace
            if not has_namespace:
                missing_namespace_files.append(rel_path)
        
        if verbose:
            print(f"Checked {files_checked} files total.    ")
            
    except KeyboardInterrupt:
        print("\nSearch interrupted by user.")
        sys.exit(1)
    
    return large_files, missing_namespace_files


def main():
    ROOT_DIR = '.' 
    MIN_LINES = 500
    VERBOSE = True 
    
    print(f"Searching '{ROOT_DIR}' for large files (>= {MIN_LINES}) or missing namespaces...")
    print("-" * 80)
    
    large_files, missing_ns = find_problematic_cs_files(ROOT_DIR, MIN_LINES, VERBOSE)
    
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

    print("-" * 80)


if __name__ == '__main__':
    main()