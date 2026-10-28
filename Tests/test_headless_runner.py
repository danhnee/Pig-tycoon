"""Windows enforcement tests with tiny Python processes only; no Blender/Unity."""
import ctypes
import ctypes.wintypes as wt
import importlib.util
import os
from pathlib import Path
import sys
import tempfile
import time
from types import SimpleNamespace
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('headless_runner', Path(__file__).resolve().parents[1] / 'tools/headless_runner.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class UnityPlayerCommandTests(unittest.TestCase):
    def test_graphics_player_retains_job_runner_but_enables_rendering(self):
        with tempfile.TemporaryDirectory(prefix='pig-player-graphics-') as temporary:
            root = Path(temporary).resolve()
            player = root / 'Demo.exe'
            player.touch()
            (root / 'Demo_Data').mkdir()
            (root / 'Demo_Data' / 'globalgamemanagers').touch()
            (root / 'UnityPlayer.dll').touch()
            args = SimpleNamespace(player=str(player), method=None, graphics=True,
                                   script_args=['-screen-width', '960', '-screen-height', '540'])
            with patch.object(runner, 'ROOT', root):
                command = runner.unity_cmd(args, root / 'graphics.log')
            self.assertNotIn('-nographics', command)
            self.assertNotIn('-batchmode', command)
            self.assertIn('-silent-crashes', command)
            self.assertIn('-logFile', command)

    def test_editor_does_not_accept_graphics_capture_mode(self):
        args = SimpleNamespace(player=None, graphics=True, method=None, project='unused', script_args=[])
        with self.assertRaises(SystemExit):
            runner.unity_cmd(args, Path('unused.log'))

    def test_player_runs_without_editor_or_immediate_quit(self):
        with tempfile.TemporaryDirectory(prefix='pig-player-command-') as temporary:
            root = Path(temporary).resolve()
            player = root / 'Demo.exe'
            player.touch()
            (root / 'Demo_Data').mkdir()
            (root / 'Demo_Data' / 'globalgamemanagers').touch()
            (root / 'UnityPlayer.dll').touch()
            args = SimpleNamespace(player=str(player), method=None, project='unused',
                                   script_args=['-pigTycoonSmoke'])
            with patch.object(runner, 'ROOT', root):
                command = runner.unity_cmd(args, root / 'smoke.log')
            self.assertEqual(command[0], str(player))
            self.assertIn('-pigTycoonSmoke', command)
            self.assertNotIn('-quit', command)
            self.assertNotIn('-projectPath', command)

    def test_player_rejects_missing_unity_data(self):
        with tempfile.TemporaryDirectory(prefix='pig-player-command-') as temporary:
            root = Path(temporary).resolve()
            player = root / 'Demo.exe'
            player.touch()
            args = SimpleNamespace(player=str(player), method=None, project='unused', script_args=[])
            with patch.object(runner, 'ROOT', root), self.assertRaises(SystemExit):
                runner.unity_cmd(args, root / 'smoke.log')

    def test_player_smoke_never_reuses_editor_cache(self):
        with patch.object(runner, 'unity_cmd', return_value=['Demo.exe']), \
                patch.object(runner, 'cache_key', return_value=('test-key', {'inputs': {}})) as cache_key:
            self.assertEqual(runner.main(['unity', '--player', 'Demo.exe', '--dry-run']), 0)
            cache_key.assert_not_called()


@unittest.skipUnless(os.name == 'nt', 'requires actual Windows Job Objects')
class HeadlessMemoryTests(unittest.TestCase):
    def setUp(self):
        self.scratch = tempfile.TemporaryDirectory(prefix='pig-headless-test-')
        self.folder = Path(self.scratch.name).resolve()
        self.assertTrue(self.folder.is_relative_to(Path(tempfile.gettempdir()).resolve()))
        self.addCleanup(self.scratch.cleanup)

    def run_python(self, code, cap=128, runtime_free=1, grace=1):
        args = SimpleNamespace(max_mem_mb=cap, tail=0, runtime_min_free_mb=runtime_free,
                               runtime_low_free_sec=grace)
        return runner.run([sys.executable, '-c', code], args, self.folder/'child.log', self.folder/'report.json', 'blender')

    def test_job_limit_is_aggregate_and_handles_are_pointer_sized(self):
        job = runner.make_job(96)
        try:
            k32 = ctypes.WinDLL('kernel32', use_last_error=True)
            k32.QueryInformationJobObject.argtypes = [wt.HANDLE, ctypes.c_int, wt.LPVOID, wt.DWORD, ctypes.POINTER(wt.DWORD)]
            k32.QueryInformationJobObject.restype = wt.BOOL
            info = runner.JOBOBJECT_EXTENDED_LIMIT_INFORMATION()
            self.assertTrue(k32.QueryInformationJobObject(job, 9, ctypes.byref(info), ctypes.sizeof(info), None))
            self.assertTrue(info.BasicLimitInformation.LimitFlags & 0x200)  # JOB_OBJECT_LIMIT_JOB_MEMORY
            self.assertTrue(info.BasicLimitInformation.LimitFlags & 0x2000)  # KILL_ON_JOB_CLOSE
            self.assertEqual(info.JobMemoryLimit, 96 * 2**20)
            self.assertIs(runner.kernel32().CreateJobObjectW.restype, wt.HANDLE)
            self.assertEqual(runner.kernel32().AssignProcessToJobObject.argtypes, [wt.HANDLE, wt.HANDLE])
        finally:
            k32.CloseHandle.argtypes = [wt.HANDLE]
            k32.CloseHandle.restype = wt.BOOL
            k32.CloseHandle(job)

    def test_assignment_failure_never_executes_user_code(self):
        marker = self.folder/'executed.txt'
        with patch.object(runner, 'assign_job', return_value=False):
            code, report = self.run_python(f'from pathlib import Path; Path({str(marker)!r}).write_text("executed")')
        self.assertNotEqual(code, 0)
        self.assertFalse(marker.exists(), 'uncontained child executed before job attachment')
        self.assertFalse(report['job_assigned'])
        self.assertEqual(report['termination_reason'], 'JOB_ENFORCEMENT_FAILED')

    def test_peak_is_simultaneous_parent_plus_child_not_largest_member(self):
        child = 'import time; payload=bytearray(20*2**20); time.sleep(1)'
        parent = f'import subprocess,sys,time; payload=bytearray(20*2**20); child=subprocess.Popen([sys.executable,"-c",{child!r}]); child.wait()'
        code, report = self.run_python(parent)
        self.assertEqual(code, 0)
        self.assertGreaterEqual(report['peak_private_mb'], 48)
        self.assertGreaterEqual(report['peak_member_count'], 2)
        self.assertEqual(report['peak_private_source'], 'Win32 PeakJobMemoryUsed')
        self.assertTrue(report['job_assigned'])

    def test_two_children_cannot_each_consume_the_whole_cap(self):
        markers = [self.folder/'allocated-0.txt', self.folder/'allocated-1.txt']
        child = 'import sys,time; from pathlib import Path; payload=bytearray(28*2**20); Path(sys.argv[1]).write_text("allocated"); time.sleep(1)'
        parent = f'import subprocess,sys; payload=bytearray(16*2**20); children=[subprocess.Popen([sys.executable,"-c",{child!r},path]) for path in {[str(p) for p in markers]!r}]; [p.wait() for p in children]'
        code, report = self.run_python(parent, cap=96)
        self.assertEqual(code, 137)
        self.assertTrue(report['killed_by_memory_cap'])
        self.assertFalse(all(marker.exists() for marker in markers), 'both allocations completed despite aggregate cap')
        # Windows reported a kernel high-water mark above the configured limit
        # during this denied-allocation control. Report it honestly, without clipping.
        self.assertGreater(report['peak_private_bytes'], 0)
        self.assertEqual(report['max_mem_mb'], 96, 'runner must not raise the requested cap')

    def test_runtime_low_free_ram_guard_terminates_owned_job(self):
        import psutil
        impossible_free = int(psutil.virtual_memory().total / 2**20) + 1
        started = time.monotonic()
        code, report = self.run_python('import time; time.sleep(2)', runtime_free=impossible_free, grace=.05)
        self.assertEqual(code, 137)
        self.assertEqual(report['termination_reason'], 'LOW_FREE_RAM')
        self.assertTrue(report['killed_by_free_ram_guard'])
        self.assertFalse(report['killed_by_memory_cap'])
        self.assertLess(time.monotonic()-started, 2)

    def test_job_monitors_descendant_after_root_exits(self):
        marker = self.folder/'descendant-finished.txt'
        child = f'import time; from pathlib import Path; payload=bytearray(16*2**20); time.sleep(.6); Path({str(marker)!r}).write_text("done")'
        parent = f'import subprocess,sys; subprocess.Popen([sys.executable,"-c",{child!r}])'
        code, report = self.run_python(parent)
        self.assertEqual(code, 0)
        self.assertTrue(marker.exists(), 'runner returned before its remaining job members finished')
        self.assertGreaterEqual(report['peak_private_mb'], 20)


if __name__ == '__main__':
    unittest.main(verbosity=2)
