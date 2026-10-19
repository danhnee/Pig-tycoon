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
  bool _isAnPerspective = false;
  String _selectedCharacter =
      'Khoa (STR 24 · AGI 18 · CTRL 18 · RES 22 · Trại Chủ Lão Luyện)';
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
    final String youngestSibling = _isAnPerspective
        ? 'Em trai Bé Khoa (13/05/2019)'
        : 'Em gái Bé An (13/05/2019)';
    final String notebookStyle = _isAnPerspective
        ? 'Sổ bìa hồng dán sticker con thỏ (Thư gửi người thân + ★ / ☾ + :))'
        : 'Sổ bìa đen 2 cột ("Thấy" bên trái | "Có thật" bên phải)';

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
                  'Ngày $_day | Vàng: ${_gold}G | Kho cám: ${_feedKg.toStringAsFixed(1)} kg | Sinh Cảnh: $_habitatSc | ÁLSK: $_eventPressureAlsk\n'
                  'Em út mất ngày 13/05/2019: $youngestSibling | Sổ ghi chép: $notebookStyle',
                ),
                trailing: FilledButton(
                  key: const Key('btn_switch_character'),
                  onPressed: () {
                    setState(() {
                      _isAnPerspective = !_isAnPerspective;
                      _selectedCharacter = _isAnPerspective
                          ? 'An (STR 17 · AGI 23 · CTRL 24 · RES 18 · Người Đọc Dòng)'
                          : 'Khoa (STR 24 · AGI 18 · CTRL 18 · RES 22 · Trại Chủ Lão Luyện)';
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
                      'CỐT TRUYỆN MÙA 1: "SÁU VỚI MỘT" (GÓC NHÌN KÉP KHOA & AN · PHẦN 1–12)',
                      style:
                          TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      '• Dòng thời gian Gia đình (Bảng 1.4 / 1B): Ông Lâm (17/07/2018), Bà Sương (04/09/2018), Anh Minh (19/02/2019), $youngestSibling, Cha Hậu (24/08/2019), Đêm Giữ Chỗ (05/10/2019 02:00), Mẹ Thủy (08/11/2019), đến Bờ Rìa (24/09/2026).\n'
                      '• Phần 11–12: Đại Giới Lưu (Minh Ước 24 × 125 = 3.000 năm), 6 câu Mặc Khải bằng Khắc Ngữ trên Đại Cổ Thụ, Kim Ô Thánh Hội 4 thành viên (Thánh Minh Nhật Giả, Nguyệt Thư, Custos, Sirphina <=3 câu <=3 từ).\n'
                      '• Chương 8 "Lời chào cuối cùng": Đưa hai lòng bàn tay ngửa lên để Sirphina dung hợp 2 nửa linh hồn, tạm biệt 11 cư dân Bờ Rìa, về nhà ăn tối và cảnh sau credit "Ba năm sau".',
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

