@echo off
cd /d "%~dp0"
python tools\launch_android_emulator_and_game.py %*
