import 'dart:convert';
import 'dart:io';

import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:pig_tycoon_flutter_client/cloud/save_sync.dart';
import 'package:pig_tycoon_flutter_client/cloud/supabase_api.dart';

/// Minimal stand-in for Supabase Auth + REST, enough to check what the app sends.
class FakeSupabase {
  late HttpServer server;
  final requests = <({String method, String path, Map<String, String> headers, Object? body})>[];
  final saveRows = <Map<String, Object?>>[];
  final profileRows = <Map<String, Object?>>[];
  bool enableSignupEdgeFunction = true;

  Future<String> start() async {
    server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    server.listen(_handle);
    return 'http://127.0.0.1:${server.port}';
  }

  Future<void> _handle(HttpRequest req) async {
    final text = await utf8.decoder.bind(req).join();
    final body = text.isEmpty ? null : jsonDecode(text);
    final headers = <String, String>{};
    req.headers.forEach((k, v) => headers[k] = v.join(','));
    requests.add((method: req.method, path: req.uri.toString(), headers: headers, body: body));
    Object? out;
    var status = 200;
    final path = req.uri.path;
    if (path == '/auth/v1/token') {
      final b = body as Map;
      if (b['password'] != 'secret123') {
        status = 400;
        out = {'error_description': 'Invalid login credentials'};
      } else {
        out = {'access_token': 'jwt-1', 'user': {'id': 'u-1', 'email': b['email']}};
      }
    } else if (path == '/functions/v1/signup' && enableSignupEdgeFunction) {
      final b = body as Map;
      out = {
        'access_token': 'jwt-signup',
        'user': {'id': 'u-new', 'email': b['email']},
      };
    } else if (path == '/auth/v1/signup') {
      final b = body as Map;
      out = {'id': 'u-pending', 'email': b['email']};
    } else if (path == '/rest/v1/save_slots' && req.method == 'GET') {
      out = saveRows;
    } else if (path == '/rest/v1/save_slots') {
      saveRows
        ..clear()
        ..add(Map<String, Object?>.from(body as Map));
      status = 201;
    } else if (path == '/rest/v1/profiles') {
      profileRows
        ..clear()
        ..add(Map<String, Object?>.from(body as Map));
      status = 201;
    } else {
      status = 404;
    }
    req.response.statusCode = status;
    if (out != null) req.response.write(jsonEncode(out));
    await req.response.close();
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  // flutter_test answers every HttpClient request with 400; talk to the local fake instead.
  HttpOverrides.global = null;
  const saveChannel = MethodChannel('pigtycoon/save');
  late FakeSupabase fake;
  late SupabaseApi api;
  Map<String, Object?>? localFile;

  setUp(() async {
    fake = FakeSupabase();
    final url = await fake.start();
    api = SupabaseApi(SupabaseConfig(url: url, anonKey: 'anon-key'));
    localFile = null;
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(saveChannel, (call) async {
      switch (call.method) {
        case 'readSave':
          return localFile;
        case 'writeSave':
          localFile = {
            'json': (call.arguments as Map)['json'],
            'modifiedMs': DateTime.now().millisecondsSinceEpoch,
          };
          return null;
      }
      return null;
    });
  });

  tearDown(() async {
    api.close();
    await fake.server.close(force: true);
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(saveChannel, null);
  });

  test('sign-in sends the anon key and keeps the user session', () async {
    final s = await api.signIn('an@pig.vn', 'secret123');
    expect(s.userId, 'u-1');
    final r = fake.requests.single;
    expect(r.path, '/auth/v1/token?grant_type=password');
    expect(r.headers['apikey'], 'anon-key');
  });

  test('wrong password surfaces the server message', () async {
    expect(() => api.signIn('an@pig.vn', 'nope'),
        throwsA(isA<SupabaseException>()
            .having((e) => e.message, 'message', 'Invalid login credentials')));
  });

  test('upload stores the save and a profile summary for the signed-in user',
      () async {
    await api.signIn('khoa@pig.vn', 'secret123');
    localFile = {
      'json': jsonEncode({'SaveVersion': 3, 'FarmLevel': 4, 'Gold': 2500, 'CurrentDay': 7}),
      'modifiedMs': DateTime.utc(2026, 10, 31, 8).millisecondsSinceEpoch,
    };
    final r = await SaveSync(api).pushAfterReturn('Khoa');
    expect(r, SyncResult.uploaded);

    final save = fake.saveRows.single;
    expect(save['user_id'], 'u-1');
    expect(save['save_version'], 3);
    expect((save['save_json'] as Map)['Gold'], 2500);
    final upsert = fake.requests.firstWhere((q) => q.path.startsWith('/rest/v1/save_slots'));
    expect(upsert.headers['authorization'], 'Bearer jwt-1');
    expect(upsert.headers['prefer'], contains('merge-duplicates'));

    final profile = fake.profileRows.single;
    expect(profile['character'], 'Khoa');
    expect(profile['farm_level'], 4);
    expect(profile['gold'], 2500);
    expect(profile['current_day'], 7);
  });

  test('newer cloud save replaces the local file before launch', () async {
    await api.signIn('an@pig.vn', 'secret123');
    localFile = {
      'json': '{"Gold":10}',
      'modifiedMs': DateTime.utc(2026, 10, 30).millisecondsSinceEpoch,
    };
    fake.saveRows.add({
      'save_json': {'Gold': 999},
      'save_version': 3,
      'saved_at': '2026-10-31T09:00:00Z',
    });
    expect(await SaveSync(api).pullBeforeLaunch(), SyncResult.downloaded);
    expect(jsonDecode(localFile!['json'] as String)['Gold'], 999);
  });

  test('older cloud save never overwrites newer local progress', () async {
    await api.signIn('an@pig.vn', 'secret123');
    localFile = {
      'json': '{"Gold":10}',
      'modifiedMs': DateTime.utc(2026, 10, 31, 12).millisecondsSinceEpoch,
    };
    fake.saveRows.add({
      'save_json': {'Gold': 999},
      'save_version': 3,
      'saved_at': '2026-10-31T09:00:00Z',
    });
    expect(await SaveSync(api).pullBeforeLaunch(), SyncResult.kept);
    expect(localFile!['json'], '{"Gold":10}');
  });

  test('nothing to upload when the game has not saved yet', () async {
    await api.signIn('an@pig.vn', 'secret123');
    expect(await SaveSync(api).pushAfterReturn('An'), SyncResult.nothing);
    expect(fake.saveRows, isEmpty);

    final s = await api.signUp('new@pig.vn', 'secret123');
    expect(s, isNotNull);
    expect(s!.userId, 'u-new');
    expect(s.accessToken, 'jwt-signup');

    fake.enableSignupEdgeFunction = false;
    final pending = await api.signUp('pending@pig.vn', 'secret123');
    expect(pending, isNull);
  });
}
