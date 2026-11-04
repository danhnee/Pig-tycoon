import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'cloud/save_sync.dart';
import 'cloud/supabase_api.dart';

/// Launches the real Unity player. Gameplay and saves stay owned by Unity.
class UnityGameHost {
  const UnityGameHost();
  static const _channel = MethodChannel('pigtycoon/unity');

  Future<void> launch(String player) {
    if (player != 'An' && player != 'Khoa') {
      throw ArgumentError.value(player, 'player', 'Chỉ có An hoặc Khoa');
    }
    return _channel.invokeMethod<void>('launchGame', {'player': player});
  }
}

class PigTycoonGameApp extends StatelessWidget {
  const PigTycoonGameApp(
      {super.key, this.config = SupabaseConfig.fromEnvironment});
  final SupabaseConfig config;
  @override
  Widget build(BuildContext context) => MaterialApp(
        title: 'Pig Tycoon',
        debugShowCheckedModeBanner: false,
        theme: ThemeData(
          colorScheme: ColorScheme.fromSeed(
            seedColor: const Color(0xff9b7951),
            brightness: Brightness.dark,
          ),
        ),
        // Without a Supabase project the game still runs with local saves only.
        home: config.isConfigured
            ? CloudLoginScreen(api: SupabaseApi(config))
            : const UnityGameMenu(),
      );
}

class CloudLoginScreen extends StatefulWidget {
  const CloudLoginScreen({super.key, required this.api});
  final SupabaseApi api;
  @override
  State<CloudLoginScreen> createState() => _CloudLoginScreenState();
}

class _CloudLoginScreenState extends State<CloudLoginScreen> {
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _busy = false;
  String? _message;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit({required bool register}) async {
    final email = _email.text.trim();
    final password = _password.text;
    if (!email.contains('@') || password.length < 6) {
      setState(() => _message = 'Nhập email hợp lệ và mật khẩu từ 6 ký tự.');
      return;
    }
    setState(() {
      _busy = true;
      _message = null;
    });
    try {
      if (register) {
        final s = await widget.api.signUp(email, password);
        if (s == null) {
          if (mounted) {
            setState(() => _message =
                'Đã tạo tài khoản. Mở email để xác nhận rồi đăng nhập.');
          }
          return;
        }
      } else {
        await widget.api.signIn(email, password);
      }
      if (!mounted) return;
      await Navigator.of(context).pushReplacement(MaterialPageRoute<void>(
          builder: (_) => UnityGameMenu(sync: SaveSync(widget.api))));
    } on SupabaseException catch (e) {
      if (mounted) setState(() => _message = e.message);
    } on Exception {
      if (mounted) {
        setState(() => _message = 'Không kết nối được máy chủ. Kiểm tra mạng.');
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        body: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 420),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text('PIG TYCOON',
                        textAlign: TextAlign.center,
                        style: Theme.of(context).textTheme.headlineLarge),
                    const SizedBox(height: 24),
                    TextField(
                      key: const Key('login_email'),
                      controller: _email,
                      keyboardType: TextInputType.emailAddress,
                      decoration: const InputDecoration(
                          labelText: 'Email', prefixIcon: Icon(Icons.mail)),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      key: const Key('login_password'),
                      controller: _password,
                      obscureText: true,
                      decoration: const InputDecoration(
                          labelText: 'Mật khẩu', prefixIcon: Icon(Icons.lock)),
                    ),
                    const SizedBox(height: 24),
                    FilledButton(
                      key: const Key('login_submit'),
                      onPressed:
                          _busy ? null : () => _submit(register: false),
                      child: const Text('Đăng nhập'),
                    ),
                    const SizedBox(height: 8),
                    OutlinedButton(
                      key: const Key('login_register'),
                      onPressed: _busy ? null : () => _submit(register: true),
                      child: const Text('Tạo tài khoản'),
                    ),
                    if (_message != null)
                      Padding(
                        padding: const EdgeInsets.only(top: 16),
                        child: Text(_message!, textAlign: TextAlign.center),
                      ),
                  ],
                ),
              ),
            ),
          ),
        ),
      );
}

class UnityGameMenu extends StatefulWidget {
  const UnityGameMenu({super.key, this.sync});

  /// Null when playing offline (no Supabase project configured).
  final SaveSync? sync;
  @override
  State<UnityGameMenu> createState() => _UnityGameMenuState();
}

class _UnityGameMenuState extends State<UnityGameMenu> {
  String _player = 'Khoa';
  bool _launching = false;
  String? _error;
  String? _cloudStatus;

  Future<void> _launch() async {
    if (_launching) return;
    setState(() {
      _launching = true;
      _error = null;
    });
    final sync = widget.sync;
    try {
      if (sync != null) await _pull(sync);
      // This completes only after the native activity returns to this menu.
      await const UnityGameHost().launch(_player);
      if (sync != null) await _push(sync);
    } on PlatformException catch (e) {
      if (mounted) setState(() => _error = e.message ?? e.code);
    } on MissingPluginException {
      if (mounted) {
        setState(() =>
            _error = 'Mở bản Android để chơi. Bản này chưa có Unity Player.');
      }
    } finally {
      if (mounted) setState(() => _launching = false);
    }
  }

  // A failed sync never blocks play; the local save stays authoritative.
  Future<void> _pull(SaveSync sync) async {
    try {
      final r = await sync.pullBeforeLaunch();
      _setCloud(r == SyncResult.downloaded
          ? 'Đã tải bản lưu từ máy chủ.'
          : 'Bản lưu trên máy là mới nhất.');
    } on Exception catch (e) {
      _setCloud('Chưa đồng bộ được: $e. Chơi tiếp bằng bản lưu trên máy.');
    }
  }

  Future<void> _push(SaveSync sync) async {
    try {
      final r = await sync.pushAfterReturn(_player);
      _setCloud(r == SyncResult.uploaded
          ? 'Đã lưu tiến trình lên máy chủ.'
          : 'Chưa có bản lưu để gửi lên.');
    } on Exception catch (e) {
      _setCloud('Chưa gửi được bản lưu: $e.');
    }
  }

  void _setCloud(String text) {
    if (mounted) setState(() => _cloudStatus = text);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        body: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 480),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text('PIG TYCOON',
                        style: Theme.of(context).textTheme.headlineLarge),
                    const SizedBox(height: 12),
                    const Text('Sáu với một'),
                    const SizedBox(height: 32),
                    SegmentedButton<String>(
                      segments: const [
                        ButtonSegment(value: 'An', label: Text('An')),
                        ButtonSegment(value: 'Khoa', label: Text('Khoa')),
                      ],
                      selected: {_player},
                      onSelectionChanged: _launching
                          ? null
                          : (value) => setState(() => _player = value.single),
                    ),
                    const SizedBox(height: 24),
                    FilledButton(
                      key: const Key('launch_unity'),
                      onPressed: _launching ? null : _launch,
                      child: Text(_launching ? 'Đang chơi…' : 'Vào trang trại'),
                    ),
                    if (widget.sync != null)
                      Padding(
                        padding: const EdgeInsets.only(top: 16),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(Icons.cloud_done, size: 18),
                            const SizedBox(width: 8),
                            Flexible(
                              child: Text(
                                  key: const Key('cloud_status'),
                                  _cloudStatus ??
                                      widget.sync!.api.session?.email ??
                                      ''),
                            ),
                          ],
                        ),
                      ),
                    if (_error != null)
                      Padding(
                        padding: const EdgeInsets.only(top: 20),
                        child: Text(_error!, semanticsLabel: _error),
                      ),
                  ],
                ),
              ),
            ),
          ),
        ),
      );
}
