#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Empirical verification suite for D:/Collab-Hub v5.0 (P0/P1/P2 + Opus R1-R8 & N1-N7)."""

from __future__ import annotations

import hashlib
import json
import os
import shutil
import subprocess
import sys
import tomllib
from pathlib import Path

HUB_ROOT = Path(r"D:\Collab-Hub")
sys.path.insert(0, str(HUB_ROOT / "mcp"))
import argon_codex_mcp_server as srv  # noqa: E402


def sha256_of(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def run_empirical_verification() -> dict:
    # 1. Verify TOOLS_SCHEMA has compartment support on all 9 tools and no wake_command
    schema_by_name = {t["name"]: t["inputSchema"]["properties"] for t in srv.TOOLS_SCHEMA}
    compartment_tools = [
        "collab_send_message",
        "collab_read_messages",
        "collab_delegate_task_to_argon",
        "collab_list_delegated_tasks",
        "collab_update_delegated_task",
        "collab_wake_agent",
        "collab_ack_wakeup",
        "collab_publish_direct_event",
        "collab_wait_for_event",
    ]
    missing_comp = [n for n in compartment_tools if "compartment" not in schema_by_name.get(n, {})]
    if missing_comp:
        raise AssertionError(f"Missing compartment in inputSchema for: {missing_comp}")
    if "wake_command" in schema_by_name.get("collab_wake_agent", {}):
        raise AssertionError("wake_command should be removed from collab_wake_agent inputSchema")

    # 2. Verify Single-Instance TCP Hub endpoint, live PID reconciliation, and R4 hub_token protection
    peers_snap = srv.tool_collab_peer_registry({})
    ep_path = HUB_ROOT / "hub_endpoint.json"
    ep = json.loads(ep_path.read_text(encoding="utf-8"))
    hub_pid = int(ep.get("pid", 0))
    if not ep.get("hub_token") or not ep.get("last_heartbeat_utc"):
        raise AssertionError(f"hub_endpoint.json missing hub_token or last_heartbeat_utc: {ep}")
    netstat_proc = subprocess.run(
        ["cmd.exe", "/C", "netstat -ano | findstr :47821 | findstr LISTENING"],
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if str(hub_pid) not in netstat_proc.stdout:
        raise AssertionError(f"hub_endpoint.json PID {hub_pid} does not match netstat listener:\n{netstat_proc.stdout}")

    # R4: Verify unauthenticated collab_hub_shutdown is rejected and hub stays alive
    unauth_shut = srv._call_native_go_broker("collab_hub_shutdown", {"hub_token": "invalid_token"})
    if not isinstance(unauth_shut, dict) or "UNAUTHORIZED_HUB_SHUTDOWN" not in str(unauth_shut.get("error", "")):
        raise AssertionError(f"Expected UNAUTHORIZED_HUB_SHUTDOWN, got: {unauth_shut}")

    # 3. Verify Antigravity LS Bridge auto-discovery
    ls_bridge = peers_snap.get("antigravity_ls_bridge", {})
    if not ls_bridge.get("discovered") or not ls_bridge.get("ls_address"):
        raise AssertionError(f"Antigravity LS Bridge not discovered: {ls_bridge}")

    # 4. Verify read-only invariance & R5 strict compartment rejection on unknown compartments
    ws_path = HUB_ROOT / "agent_wakeup_state.json"
    reg_path = HUB_ROOT / "compartments" / "registry.json"
    ws_sha_before = sha256_of(ws_path) if ws_path.exists() else ""
    reg_sha_before = sha256_of(reg_path) if reg_path.exists() else ""
    _ = srv.tool_collab_peer_registry({})
    _ = srv.tool_collab_compartment_manager({"action": "list"})
    ghost_read = srv.tool_collab_read_messages({"compartment": "ghost/room"})
    ghost_send = srv.tool_collab_send_message({"compartment": "ghost/room", "sender": "codex", "subject": "x", "body": "y"})
    if "UNKNOWN_COMPARTMENT" not in str(ghost_read.get("error", "")) or "UNKNOWN_COMPARTMENT" not in str(ghost_send.get("error", "")):
        raise AssertionError(f"R5 failure: ghost/room was not rejected: read={ghost_read}, send={ghost_send}")
    if (HUB_ROOT / "compartments" / "ghost").exists():
        raise AssertionError("R5 failure: compartments/ghost directory was created on disk!")
    ws_sha_after = sha256_of(ws_path) if ws_path.exists() else ""
    reg_sha_after = sha256_of(reg_path) if reg_path.exists() else ""
    if ws_sha_before != ws_sha_after:
        raise AssertionError("tool_collab_peer_registry mutated agent_wakeup_state.json on read!")
    if reg_sha_before != reg_sha_after:
        raise AssertionError("read/ghost operations mutated compartments/registry.json!")

    # 5. Verify N1 (MSIX claude_desktop_config.json) & N2 (D:\CodexHome\config.toml)
    msix_cfg = Path(r"C:\Users\Admin\AppData\Local\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\Claude\claude_desktop_config.json")
    msix_data = json.loads(msix_cfg.read_text(encoding="utf-8"))
    if "coworkUserFilesPath" not in msix_data or "preferences" not in msix_data or "argon_codex_collab" not in msix_data.get("mcpServers", {}):
        raise AssertionError(f"N1 failure in MSIX claude_desktop_config.json: keys={list(msix_data.keys())}")
    codex_toml = tomllib.loads(Path(r"D:\CodexHome\config.toml").read_text(encoding="utf-8"))
    codex_env = codex_toml.get("mcp_servers", {}).get("argon_codex_collab", {}).get("env", {})
    if codex_env.get("COLLAB_HUB_DIR") != r"D:\Collab-Hub":
        raise AssertionError(f"N2 failure in D:\\CodexHome\\config.toml env: {codex_env}")

    # 6. Verify compartment `elunio/classroom` binding to conversation `8f84ab6c-9674-4125-a261-0290c50b0633`
    elunio_ws = r"D:\DO_AN_LAP_TRINH_WEB\.worktrees\learning-workspace"
    elunio_conv = "8f84ab6c-9674-4125-a261-0290c50b0633"
    bind_res = srv.tool_collab_compartment_manager(
        {
            "action": "bind_session",
            "compartment": "elunio/classroom",
            "agent_id": "gemini",
            "session_id": elunio_conv,
        }
    )
    comp_elunio = bind_res.get("compartment", {})
    if comp_elunio.get("gemini_conversation_id") != elunio_conv or comp_elunio.get("workspace_path") != elunio_ws:
        raise AssertionError(f"Unexpected elunio/classroom profile: {comp_elunio}")

    # 7. Verify R1, R3, N5, and multi-process concurrency in temporary compartment `collab-hub/selftest-v5`
    test_comp = "collab-hub/selftest-v5"
    test_comp_dir = HUB_ROOT / "compartments" / "collab-hub" / "selftest-v5"
    if test_comp_dir.exists():
        shutil.rmtree(test_comp_dir, ignore_errors=True)

    root_msg_sha_before = sha256_of(HUB_ROOT / "messages.jsonl")
    root_tsk_sha_before = sha256_of(HUB_ROOT / "delegated_tasks.jsonl")

    try:
        srv.tool_collab_compartment_manager(
            {
                "action": "create",
                "compartment": test_comp,
                "title": "Selftest v5 Compartment",
                "workspace_path": str(HUB_ROOT),
            }
        )

        # R1: Verify >1 MiB body without allow_large_body is rejected with PAYLOAD_TOO_LARGE
        too_big = srv.tool_collab_send_message(
            {
                "compartment": test_comp,
                "sender": "claude_desktop",
                "recipient": "gemini",
                "subject": "Oversized",
                "body": "X" * (1100 * 1024),
                "auto_wake_recipient": False,
            }
        )
        if too_big.get("ok") or "PAYLOAD_TOO_LARGE" not in str(too_big.get("error", "")):
            raise AssertionError(f"R1 failure: 1.1 MiB message was not rejected: {too_big}")

        # R3: Verify 600s idempotency deduplication on identical send
        s_res1 = srv.tool_collab_send_message(
            {
                "compartment": test_comp,
                "sender": "claude_cli",
                "recipient": "gemini",
                "subject": "Selftest Compartment Isolation",
                "body": "Testing isolated send/read/delegate/update",
                "auto_wake_recipient": False,
            }
        )
        s_res2 = srv.tool_collab_send_message(
            {
                "compartment": test_comp,
                "sender": "claude_cli",
                "recipient": "gemini",
                "subject": "Selftest Compartment Isolation",
                "body": "Testing isolated send/read/delegate/update",
                "auto_wake_recipient": False,
            }
        )
        if not s_res2.get("deduplicated") or s_res1["message"]["id"] != s_res2["message"]["id"]:
            raise AssertionError(f"R3 failure: duplicate send was not deduplicated: first={s_res1}, second={s_res2}")

        r_res = srv.tool_collab_read_messages({"compartment": test_comp, "recipient": "gemini", "limit": 5})
        if r_res["count"] != 1 or r_res["messages"][0]["subject"] != "Selftest Compartment Isolation":
            raise AssertionError(f"Compartment read failed: {r_res}")

        # Verify claude_desktop vs claude_cli alias separation
        srv.tool_collab_send_message(
            {
                "compartment": test_comp,
                "sender": "gemini",
                "recipient": "claude_desktop",
                "subject": "Desktop Only Message",
                "body": "Should not leak to claude_cli filter",
                "auto_wake_recipient": False,
            }
        )
        r_cli_only = srv.tool_collab_read_messages({"compartment": test_comp, "recipient": "claude_cli", "limit": 10})
        if any(m["subject"] == "Desktop Only Message" for m in r_cli_only["messages"]):
            raise AssertionError("claude_desktop message leaked into claude_cli recipient filter!")

        d_res = srv.tool_collab_delegate_task_to_argon(
            {
                "compartment": test_comp,
                "requester": "claude_cli",
                "assignee": "gemini",
                "subagent_type": "self",
                "title": "Selftest Task #1",
                "instructions": "Verify compartment task update",
            }
        )
        tid = d_res["delegated_task"]["task_id"]
        u_res = srv.tool_collab_update_delegated_task(
            {
                "compartment": test_comp,
                "task_id": tid,
                "status": "DONE",
                "result_summary": "Verified isolated task update",
                "result_artifacts": ["D:/Collab-Hub/bin/argon_mcp_native.exe"],
            }
        )
        if not u_res.get("ok") or u_res.get("task", {}).get("status") != "DONE":
            raise AssertionError(f"Compartment task update failed: {u_res}")

        # N5: Verify Rust C-ABI rust_fast_delta_sync_compartment
        rust_cd = srv._rust_ffi_fast_delta_sync_compartment(test_comp, 0, 0)
        if not rust_cd or rust_cd.get("compartment_key") != test_comp or rust_cd.get("relative_storage_dir") != f"compartments/{test_comp}":
            raise AssertionError(f"N5 failure: rust_fast_delta_sync_compartment returned {rust_cd}")

        # Multi-process concurrent Python append test (4 OS processes x 4 unique messages = 16 messages)
        worker_code = (
            "import sys; sys.path.insert(0, r'D:\\Collab-Hub\\mcp'); import argon_codex_mcp_server as s; "
            "[s.tool_collab_send_message({'compartment':'collab-hub/selftest-v5',"
            "'sender':'codex','recipient':'gemini','subject':f'MP-{sys.argv[1]}-{i}','body':f'concurrent-{sys.argv[1]}-{i}','auto_wake_recipient':False}) "
            "for i in range(4)]"
        )
        procs = [subprocess.Popen([sys.executable, "-c", worker_code, str(w)]) for w in range(4)]
        for p in procs:
            if p.wait(timeout=20) != 0:
                raise AssertionError("Multi-process concurrent Python worker failed!")

        mp_read = srv.tool_collab_read_messages({"compartment": test_comp, "limit": 50})
        mp_ids = [int(m["id"]) for m in mp_read["messages"]]
        if mp_ids != list(range(1, 20)):
            raise AssertionError(f"Multi-process concurrent IDs not contiguous 1..19: {mp_ids}")

        if sha256_of(HUB_ROOT / "messages.jsonl") != root_msg_sha_before:
            raise AssertionError("Compartment send mutated root messages.jsonl!")
        if sha256_of(HUB_ROOT / "delegated_tasks.jsonl") != root_tsk_sha_before:
            raise AssertionError("Compartment delegate/update mutated root delegated_tasks.jsonl!")
    finally:
        if test_comp_dir.exists():
            shutil.rmtree(test_comp_dir, ignore_errors=True)
        parent_c = HUB_ROOT / "compartments" / "collab-hub"
        if parent_c.exists() and not any(parent_c.iterdir()):
            parent_c.rmdir()
        reg_obj = json.loads(reg_path.read_text(encoding="utf-8"))
        if test_comp in reg_obj.get("compartments", {}):
            del reg_obj["compartments"][test_comp]
            tmp_r = reg_path.with_suffix(".json.tmp")
            tmp_r.write_text(json.dumps(reg_obj, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
            os.replace(tmp_r, reg_path)

    # 8. Run CLI verify-v5 and verify all dynamic checks pass without leaving temporary compartments behind
    v5_proc = subprocess.run(
        [sys.executable, str(HUB_ROOT / "mcp" / "collab_cli.py"), "verify-v5"],
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if v5_proc.returncode != 0:
        raise AssertionError(f"collab_cli.py verify-v5 failed:\n{v5_proc.stdout}\n{v5_proc.stderr}")
    v5_data = json.loads(v5_proc.stdout)
    if not v5_data.get("v5_verification_passed") or not all(v5_data.get("checks", {}).values()):
        raise AssertionError(f"verify-v5 dynamic checks failed: {v5_data}")

    return {
        "all_r1_to_r8_and_n1_to_n7_checks_passed": True,
        "hub_endpoint": {
            "pid": ep.get("pid"),
            "addr": ep.get("addr"),
            "has_hub_token": bool(ep.get("hub_token")),
            "last_heartbeat_utc": ep.get("last_heartbeat_utc"),
        },
        "netstat_47821_listening": netstat_proc.stdout.strip(),
        "antigravity_ls_bridge": ls_bridge,
        "elunio_classroom_compartment": comp_elunio,
        "verify_v5_checks": v5_data["checks"],
        "verify_v5_summary": {
            "wakeup_roundtrip_ms": v5_data["cross_process_reactive_wakeup"]["wakeup_roundtrip_ms"],
            "tcp_hub_roundtrip_avg_us": v5_data["benchmarks"]["tcp_hub_roundtrip_avg_us"],
        },
    }


if __name__ == "__main__":
    res = run_empirical_verification()
    print(json.dumps(res, ensure_ascii=False, indent=2))
