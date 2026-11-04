import 'dart:convert';
import 'dart:io';

/// Project URL and public (anon) key, passed at build time:
/// `flutter run --dart-define-from-file=supabase.env.json`.
class SupabaseConfig {
  const SupabaseConfig({required this.url, required this.anonKey});
  final String url;
  final String anonKey;

  static const SupabaseConfig fromEnvironment = SupabaseConfig(
    url: String.fromEnvironment('SUPABASE_URL'),
    anonKey: String.fromEnvironment('SUPABASE_ANON_KEY'),
  );

  bool get isConfigured => url.isNotEmpty && anonKey.isNotEmpty;
}

class SupabaseException implements Exception {
  SupabaseException(this.status, this.message);
  final int status;
  final String message;
  @override
  String toString() => message;
}

class SupabaseSession {
  const SupabaseSession(
      {required this.accessToken, required this.userId, required this.email});
  final String accessToken;
  final String userId;
  final String email;
}

class CloudSave {
  const CloudSave(
      {required this.saveJson,
      required this.saveVersion,
      required this.savedAt});
  final String saveJson;
  final int saveVersion;
  final DateTime savedAt;
}

/// Talks to Supabase Auth (GoTrue) and the auto-generated REST API (PostgREST).
/// Uses dart:io only, so the app needs no extra package.
class SupabaseApi {
  SupabaseApi(this.config, {HttpClient? client})
      : _client = client ?? HttpClient();
  final SupabaseConfig config;
  final HttpClient _client;
  SupabaseSession? session;

  Future<SupabaseSession> signIn(String email, String password) =>
      _auth('token?grant_type=password', email, password);

  /// Creates an account via the project's `signup` Edge Function so the player
  /// can sign in immediately without waiting on confirmation mail, falling back
  /// to `/auth/v1/signup` when the Edge Function is not deployed.
  Future<SupabaseSession?> signUp(String email, String password) async {
    Object? body;
    try {
      body = await _send('POST', '/functions/v1/signup',
          body: {'email': email, 'password': password}, authed: false);
    } on SupabaseException catch (e) {
      if (e.status != 404) rethrow;
      body = await _send('POST', '/auth/v1/signup',
          body: {'email': email, 'password': password}, authed: false);
    }
    if (body is Map && body['access_token'] is String) {
      return session = _sessionFrom(body);
    }
    return null;
  }

  Future<CloudSave?> fetchSave() async {
    final s = _requireSession();
    final rows = await _send('GET',
        '/rest/v1/save_slots?user_id=eq.${s.userId}&select=save_json,save_version,saved_at');
    if (rows is! List || rows.isEmpty) return null;
    final row = rows.first as Map;
    return CloudSave(
      saveJson: jsonEncode(row['save_json']),
      saveVersion: (row['save_version'] as num).toInt(),
      savedAt: DateTime.parse(row['saved_at'] as String),
    );
  }

  Future<void> uploadSave(
      {required String saveJson,
      required String character,
      required DateTime savedAt,
      String device = 'android'}) async {
    final s = _requireSession();
    final save = jsonDecode(saveJson);
    if (save is! Map) throw const FormatException('Bản lưu không hợp lệ.');
    int intField(String key, int fallback) =>
        (save[key] is num) ? (save[key] as num).toInt() : fallback;
    await _send('POST', '/rest/v1/save_slots?on_conflict=user_id',
        prefer: 'resolution=merge-duplicates,return=minimal',
        body: {
          'user_id': s.userId,
          'save_json': save,
          'save_version': intField('SaveVersion', 0),
          'saved_at': savedAt.toUtc().toIso8601String(),
          'device': device,
        });
    await _send('POST', '/rest/v1/profiles?on_conflict=id',
        prefer: 'resolution=merge-duplicates,return=minimal',
        body: {
          'id': s.userId,
          'display_name': s.email.split('@').first,
          'character': character,
          'farm_level': intField('FarmLevel', 1),
          'gold': intField('Gold', 0),
          'current_day': intField('CurrentDay', 1),
          'updated_at': DateTime.now().toUtc().toIso8601String(),
        });
  }

  void close() => _client.close(force: true);

  Future<SupabaseSession> _auth(
      String path, String email, String password) async {
    final body = await _send('POST', '/auth/v1/$path',
        body: {'email': email, 'password': password}, authed: false);
    return session = _sessionFrom(body as Map);
  }

  SupabaseSession _sessionFrom(Map body) {
    final user = body['user'] as Map;
    return SupabaseSession(
      accessToken: body['access_token'] as String,
      userId: user['id'] as String,
      email: (user['email'] as String?) ?? '',
    );
  }

  SupabaseSession _requireSession() {
    final s = session;
    if (s == null) throw SupabaseException(401, 'Chưa đăng nhập.');
    return s;
  }

  Future<Object?> _send(String method, String path,
      {Object? body, bool authed = true, String? prefer}) async {
    final uri = Uri.parse(config.url.replaceAll(RegExp(r'/+$'), '') + path);
    final req = await _client.openUrl(method, uri);
    req.headers.set('apikey', config.anonKey);
    req.headers.set(HttpHeaders.authorizationHeader,
        'Bearer ${authed ? _requireSession().accessToken : config.anonKey}');
    if (prefer != null) req.headers.set('Prefer', prefer);
    if (body != null) {
      req.headers.contentType = ContentType.json;
      req.add(utf8.encode(jsonEncode(body)));
    }
    final res = await req.close();
    final text = await res.transform(utf8.decoder).join();
    final decoded = text.isEmpty ? null : jsonDecode(text);
    if (res.statusCode >= 400) {
      String msg = 'Lỗi máy chủ (${res.statusCode}).';
      if (decoded is Map) {
        msg = (decoded['msg'] ??
                decoded['error_description'] ??
                decoded['message'] ??
                msg)
            .toString();
      }
      throw SupabaseException(res.statusCode, msg);
    }
    return decoded;
  }
}
