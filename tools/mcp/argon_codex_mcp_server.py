#!/usr/bin/env python3
# Shim: Single Source of Truth is D:/Collab-Hub/mcp/argon_codex_mcp_server.py
import runpy, sys
sys.path.insert(0, r"D:\Collab-Hub\mcp")
if __name__ == "__main__":
    runpy.run_path(r"D:\Collab-Hub\mcp\argon_codex_mcp_server.py", run_name="__main__")
else:
    from argon_codex_mcp_server import *  # noqa: F401,F403
