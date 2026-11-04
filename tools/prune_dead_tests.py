"""Remove test methods in Tests/Program.cs that only exercise the Core subsystems deleted on 3 Nov 2026
(CameraAndWorldLayoutSystem, CameraAndMapPhysicsEngine, CausalImpactChainEngine, PolyglotApiBridge,
NativePolyglotAccelerator, AnDayScript, FenceCourse, TutorialTrack) and their RunTest registrations.

A method is removed only if its body references one of those subsystems' types or GameEngine members; the
removed method bodies are written to .agent_scratch/removed_dead_core/removed_tests.cs for the record.

    python tools/prune_dead_tests.py            # dry run
    python tools/prune_dead_tests.py --apply
"""
import re
import sys

DEAD = [
    "CameraAndWorldLayoutSystem", "CameraAndMapPhysicsEngine", "CausalImpactChainEngine", "PolyglotApiBridge",
    "NativePolyglotAccelerator", "AnDayScript", "FenceCourse", "TutorialTrack",
    "CameraMode3D", "CharacterCameraFacingView", "WorldMapZoneId", "CameraPrimaryState3D", "CameraPlatformInputScheme",
    "CameraYawRotationPolicy", ".CameraAndWorld", ".CausalChains", ".ApiBridge", "LastUsedNativeBackend",
]
PATH = "Tests/Program.cs"


def methods(src):
    """Yield (name, start, end) for every `static void Name()` method, end exclusive (after the closing brace line)."""
    for m in re.finditer(r"^[ \t]*(?:private |public |internal )?static void (\w+)\(\)\s*\n[ \t]*\{", src, re.M):
        i = src.index("{", m.end() - 1)
        depth = 0
        j = i
        while j < len(src):
            c = src[j]
            if c == "{": depth += 1
            elif c == "}":
                depth -= 1
                if depth == 0:
                    break
            j += 1
        end = src.index("\n", j) + 1
        yield m.group(1), m.start(), end


def main(apply):
    src = open(PATH, encoding="utf-8").read()
    keep = {"Main", "TestLoadInvalidJson", "TestDualProtagonistAnPerspectiveParts11To12PolyglotApiBridgeAndNativeFlowField"}  # mixed: edit by hand
    dead = [(n, a, b) for n, a, b in methods(src) if n not in keep and any(d in src[a:b] for d in DEAD)]
    for n, a, b in dead:
        print(f"remove {n} ({src[a:b].count(chr(10))} lines)")
    if not apply:
        return
    removed = []
    for n, a, b in sorted(dead, key=lambda t: -t[1]):
        removed.append(src[a:b])
        src = src[:a] + src[b:]
    names = {n for n, _, _ in dead}
    src = "\n".join(l for l in src.split("\n") if not re.search(r"RunTest\([^;]*,\s*(" + "|".join(names) + r")\);", l)) if names else src
    open(PATH, "w", encoding="utf-8").write(src)
    open(".agent_scratch/removed_dead_core/removed_tests.cs", "w", encoding="utf-8").write("\n".join(reversed(removed)))
    print(f"--- removed {len(dead)} methods")


if __name__ == "__main__":
    main("--apply" in sys.argv)
