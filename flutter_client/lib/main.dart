import 'package:flutter/material.dart';
import 'pig_tycoon_polyglot_engine.dart';

void main() {
  runApp(const PigTycoonMultiPlatformApp());
}

class PigTycoonMultiPlatformApp extends StatelessWidget {
  const PigTycoonMultiPlatformApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Pig Tycoon · Sáu Với Một (Android / PC / Web)',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFFB85D19),
          brightness: Brightness.dark,
        ),
        useMaterial3: true,
      ),
      home: const PigTycoonDashboardScreen(),
    );
  }
}

class PigTycoonDashboardScreen extends StatefulWidget {
  const PigTycoonDashboardScreen({super.key});

  @override
  State<PigTycoonDashboardScreen> createState() =>
      _PigTycoonDashboardScreenState();
}

class _PigTycoonDashboardScreenState extends State<PigTycoonDashboardScreen> {
  final PigTycoonPolyglotEngine _engine = PigTycoonPolyglotEngine();
  String _selectedCharacter = 'Khoa (STR 24 · AGI 18 · CTRL 18 · RES 22)';
  int _day = 1;
  int _gold = 1200;
  double _feedKg = 250.0;
  int _habitatSc = 78;
  int _eventPressureAlsk = 12;
  PolyglotBenchmarkMetrics? _lastMetrics;

  void _runSimulationStep() {
    setState(() {
      _day += 1;
      final metrics = _engine.runBenchmarks(
        preferNativeFfi: true,
        bfsIters: 20,
        boidsSteps: 200,
        combatTicks: 1000,
        economySeeds: 50,
      );
      _lastMetrics = metrics;
      _gold += 145;
      _feedKg = (_feedKg - 18.5).clamp(20.0, 500.0);
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('PIG TYCOON v6.0 · MÙA 1: SÁU VỚI MỘT'),
        actions: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Chip(
              label: Text(
                _engine.isNativeFfiLoaded
                    ? 'NATIVE FFI ACTIVE (Rust/C++)'
                    : 'WEB / DART VM KERNEL',
              ),
            ),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Card(
              child: ListTile(
                title: Text('Nhân vật đang điều khiển: $_selectedCharacter'),
                subtitle: Text(
                  'Ngày $_day | Vàng: ${_gold}G | Kho cám: ${_feedKg.toStringAsFixed(1)} kg | Sinh Cảnh: $_habitatSc | ÁLSK: $_eventPressureAlsk',
                ),
                trailing: FilledButton(
                  key: const Key('btn_switch_character'),
                  onPressed: () {
                    setState(() {
                      _selectedCharacter = _selectedCharacter.startsWith('Khoa')
                          ? 'An (STR 17 · AGI 23 · CTRL 24 · RES 18)'
                          : 'Khoa (STR 24 · AGI 18 · CTRL 18 · RES 22)';
                    });
                  },
                  child: const Text('Đổi An / Khoa'),
                ),
              ),
            ),
            const SizedBox(height: 12),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'CỐT TRUYỆN MÙA 1: "SÁU VỚI MỘT" (P01–P08, Q01–Q20, S01–S12, CHƯƠNG 8)',
                      style:
                          TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      '• Bảng 1.4 Gia đình Khoa: Ông Lâm (17/07/2018), Bà Sương (04/09/2018 07:02), Anh Minh (19/02/2019), Bé Na (13/05/2019), Cha Hậu (24/08/2019), Đêm Giữ Chỗ (05/10/2019), Mẹ Thủy (08/11/2019), Khoa (24/09/2026 07:02).\n'
                      '• Hoàng Hôn Thu Hồn Giả Sirphina & Thánh Minh Nhật Giả (24 chốt × 125 năm = 3.000 năm).\n'
                      '• Chương 8 "Lời chào cuối cùng": Viết tên KHOA lên Nhật Quỹ Tàn Khuyết, trả kim đồng hồ cho Sirphina, tạm biệt 6 người thân và về nhà ăn tối.',
                    ),
                    const SizedBox(height: 12),
                    FilledButton.icon(
                      key: const Key('btn_run_polyglot_step'),
                      onPressed: _runSimulationStep,
                      icon: const Icon(Icons.bolt),
                      label: const Text(
                        'Chạy Mô Phỏng Ngày Mới & Benchmark Native FFI',
                      ),
                    ),
                  ],
                ),
              ),
            ),
            if (_lastMetrics != null) ...[
              const SizedBox(height: 12),
              Card(
                child: ListTile(
                  title: Text('Backend: ${_lastMetrics!.backendName}'),
                  subtitle: Text(
                    'BFS: ${_lastMetrics!.kernel1BfsMs.toStringAsFixed(2)} ms | '
                    'Boids: ${_lastMetrics!.kernel2BoidsMs.toStringAsFixed(2)} ms | '
                    'Combat: ${_lastMetrics!.kernel3CombatMs.toStringAsFixed(2)} ms | '
                    'Economy: ${_lastMetrics!.kernel4EconomyMs.toStringAsFixed(2)} ms',
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
