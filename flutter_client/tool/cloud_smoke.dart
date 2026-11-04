// End-to-end check against the real Supabase project, run from a terminal:
//   set PIG_EMAIL=... & set PIG_PASSWORD=...
//   dart run tool/cloud_smoke.dart
// Signs in, uploads a small save, reads it back and prints what the server stored.
import 'dart:convert';
import 'dart:io';

import 'package:pig_tycoon_flutter_client/cloud/supabase_api.dart';

Future<void> main() async {
  final env = jsonDecode(File('supabase.env.json').readAsStringSync()) as Map;
  final api = SupabaseApi(SupabaseConfig(
      url: env['SUPABASE_URL'] as String,
      anonKey: env['SUPABASE_ANON_KEY'] as String));
  final email = Platform.environment['PIG_EMAIL'];
  final password = Platform.environment['PIG_PASSWORD'];
  if (email == null || password == null) {
    stderr.writeln('Đặt PIG_EMAIL và PIG_PASSWORD trước khi chạy.');
    exitCode = 2;
    return;
  }
  try {
    final s = await api.signIn(email, password);
    stdout.writeln('Đăng nhập: ${s.email} (${s.userId})');
    final save = jsonEncode(
        {'SaveVersion': 3, 'FarmLevel': 2, 'Gold': 2500, 'CurrentDay': 3});
    await api.uploadSave(
        saveJson: save, character: 'An', savedAt: DateTime.now(), device: 'smoke');
    final back = await api.fetchSave();
    stdout.writeln('Đọc lại: ${back?.saveJson} lúc ${back?.savedAt}');
    exitCode = back != null && jsonDecode(back.saveJson)['Gold'] == 2500 ? 0 : 1;
  } on SupabaseException catch (e) {
    stderr.writeln('Lỗi: $e');
    exitCode = 1;
  } finally {
    api.close();
  }
}
