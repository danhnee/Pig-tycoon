#!/bin/bash
# BuildWindows (scripts only) -> run the player with -pigtycoon-scenario=uiprobe -> report .agent_scratch/uiprobe.md
set -e
export UNITY_IL2CPP_PATH='D:\Pig-tycoon\.toolchain\compatible-apphosts\il2cpp'
cd "$(dirname "$0")/.."
OUT='D:\Pig-tycoon\.agent_scratch\uiprobe.md'
rm -f .agent_scratch/uiprobe.md
[ "${BUILDALL:-0}" = 1 ] && python tools/headless_runner.py unity --no-cache --min-free-mb 150 --max-mem-mb 6144 --runtime-min-free-mb 64 --runtime-low-free-sec 30 --wait-sec 3600 --method PigTycoon.EditorTools.PixelMyth.PixelMythWorldBuilder.BuildAll
[ "${SKIP_WINBUILD:-0}" = 1 ] || python tools/headless_runner.py unity --no-cache --min-free-mb 150 --max-mem-mb ${WIN_MEM_MB:-10240} --runtime-min-free-mb 64 --runtime-low-free-sec 30 --wait-sec 3600 --method PigTycoon.EditorTools.PixelMyth.PixelMythWorldBuilder.BuildWindows
PIGTYCOON_SCENARIO=uiprobe PIGTYCOON_EXTRA_ARGS="-pigtycoon-uiprobe-out=$OUT" python tools/record_video.py --no-encode --timeout-min 25
ls -la .agent_scratch/uiprobe.md && head -3 .agent_scratch/uiprobe.md
echo UIPROBE_DONE
