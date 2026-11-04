import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:pig_tycoon_flutter_client/unity_game_host.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  const channel = MethodChannel('pigtycoon/unity');
  final calls = <MethodCall>[];
  setUp(() {
    calls.clear();
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, (call) async {
      calls.add(call);
      if (call.method == 'launchGame') return null;
      return true;
    });
  });
  tearDown(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, null);
  });

  test('launch passes selected protagonist without simulating game state',
      () async {
    await const UnityGameHost().launch('An');
    expect(calls.single.method, 'launchGame');
    expect(calls.single.arguments, {'player': 'An'});
    expect(() => const UnityGameHost().launch('Other'), throwsArgumentError);
  });

  testWidgets('host presents native launch failure and remains usable',
      (tester) async {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, (_) async {
      throw PlatformException(
          code: 'unity_missing', message: 'Unity chưa được export.');
    });
    await tester.pumpWidget(const MaterialApp(home: UnityGameMenu()));
    await tester.tap(find.byKey(const Key('launch_unity')));
    await tester.pumpAndSettle();
    expect(find.textContaining('Unity chưa được export.'), findsOneWidget);
    expect(
        tester
            .widget<FilledButton>(find.byKey(const Key('launch_unity')))
            .onPressed,
        isNotNull);
  });

  testWidgets('double tap does not start two Unity runtimes', (tester) async {
    final completer = <void Function()>[];
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, (_) async {
      calls.add(const MethodCall('launchGame'));
      await Future<void>.delayed(const Duration(milliseconds: 500));
      completer.add(() {});
      return null;
    });
    await tester.pumpWidget(const MaterialApp(home: UnityGameMenu()));
    await tester.tap(find.byKey(const Key('launch_unity')));
    await tester.pump();
    expect(
        tester
            .widget<FilledButton>(find.byKey(const Key('launch_unity')))
            .onPressed,
        isNull);
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pumpAndSettle();
    expect(calls.length, 1);
  });
}
