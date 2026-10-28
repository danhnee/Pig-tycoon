#!/usr/bin/env python3
# Shim: Single Source of Truth is D:/Collab-Hub/mcp/collab_cli.py
import runpy, sys
sys.path.insert(0, r"D:\Collab-Hub\mcp")
if __name__ == "__main__":
    runpy.run_path(r"D:\Collab-Hub\mcp\collab_cli.py", run_name="__main__")
