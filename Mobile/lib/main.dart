import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

const String kApiBaseUrl = 'http://localhost:5180';

void main() {
  runApp(const SmartCafeApp());
}

class SmartCafeApp extends StatelessWidget {
  const SmartCafeApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Smart Café — Customer Self-Order',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        colorSchemeSeed: const Color(0xFF6D3B47),
      ),
      home: const SelfOrderPage(),
    );
  }
}

class MenuItemModel {
  final String idMenu;
  final String namaMenu;
  final double harga;
  final int stok;
  final String kategori;

  const MenuItemModel({
    required this.idMenu,
    required this.namaMenu,
    required this.harga,
    required this.stok,
    required this.kategori,
  });

  factory MenuItemModel.fromJson(Map<String, dynamic> json) {
    final rawId = json['idMenu'] ?? json['IdMenu'] ?? '';
    final rawNama = json['namaMenu'] ?? json['NamaMenu'] ?? '';
    final rawHarga = json['harga'] ?? json['Harga'] ?? 0;
    final rawStok = json['stok'] ?? json['Stok'] ?? 0;
    final rawKategori =
        json['kategori'] ?? json['Kategori'] ?? json['namaKategori'] ?? 'Umum';

    return MenuItemModel(
      idMenu: rawId.toString(),
      namaMenu: rawNama.toString(),
      harga: rawHarga is num
          ? rawHarga.toDouble()
          : double.tryParse(rawHarga.toString()) ?? 0.0,
      stok: rawStok is int
          ? rawStok
          : rawStok is num
              ? rawStok.toInt()
              : int.tryParse(rawStok.toString()) ?? 0,
      kategori: rawKategori.toString(),
    );
  }
}

class CartEntry {
  final MenuItemModel menu;
  int jumlah;

  CartEntry({required this.menu, this.jumlah = 1});

  double get subtotal => menu.harga * jumlah;
}

class SelfOrderPage extends StatefulWidget {
  const SelfOrderPage({super.key});

  @override
  State<SelfOrderPage> createState() => _SelfOrderPageState();
}

class _SelfOrderPageState extends State<SelfOrderPage> {
  final TextEditingController _namaController = TextEditingController();
  final TextEditingController _mejaController = TextEditingController();
  final TextEditingController _catatanController = TextEditingController();

  List<MenuItemModel> _menus = [];
  final Map<String, CartEntry> _cart = {};
  bool _isLoading = true;
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _fetchMenus();
  }

  @override
  void dispose() {
    _namaController.dispose();
    _mejaController.dispose();
    _catatanController.dispose();
    super.dispose();
  }

  String _formatRupiah(double value) {
    final intVal = value.round();
    final digits = intVal.toString();
    final buf = StringBuffer();
    for (int i = 0; i < digits.length; i++) {
      final idxFromEnd = digits.length - i;
      buf.write(digits[i]);
      if (idxFromEnd > 1 && idxFromEnd % 3 == 1) {
        buf.write('.');
      }
    }
    return 'Rp ${buf.toString()}';
  }

  Future<void> _fetchMenus() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final uri = Uri.parse('$kApiBaseUrl/api/menu');
      final response = await http.get(uri).timeout(const Duration(seconds: 10));

      if (response.statusCode != 200) {
        throw Exception('HTTP ${response.statusCode}: ${response.body}');
      }

      final decoded = jsonDecode(response.body);
      final List<dynamic> listData = decoded is List
          ? decoded
          : (decoded is Map<String, dynamic> && decoded['data'] is List)
              ? decoded['data'] as List<dynamic>
              : throw Exception('Format JSON menu tidak sesuai.');

      final parsed = listData
          .map((item) => MenuItemModel.fromJson(Map<String, dynamic>.from(item as Map))).toList();

      if (!mounted) return;
      setState(() {
        _menus = parsed;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _errorMessage =
            'Tidak bisa memuat menu dari API lokal ($kApiBaseUrl).\nDetail: $e';
      });
    }
  }

  void _addToCart(MenuItemModel menu) {
    setState(() {
      final existing = _cart[menu.idMenu];
      if (existing != null) {
        existing.jumlah += 1;
      } else {
        _cart[menu.idMenu] = CartEntry(menu: menu, jumlah: 1);
      }
    });
  }

  void _changeQty(String idMenu, int delta) {
    setState(() {
      final entry = _cart[idMenu];
      if (entry == null) return;
      final nextQty = entry.jumlah + delta;
      if (nextQty <= 0) {
        _cart.remove(idMenu);
      } else {
        entry.jumlah = nextQty;
      }
    });
  }

  double get _totalHarga =>
      _cart.values.fold(0.0, (sum, item) => sum + item.subtotal);

  Future<void> _submitOrder() async {
    if (_cart.isEmpty) {
      _showSnack('Keranjang masih kosong.');
      return;
    }

    final nama = _namaController.text.trim();
    final meja = _mejaController.text.trim();
    final catatan = _catatanController.text.trim();

    if (nama.isEmpty || meja.isEmpty) {
      _showSnack('Isi Nama Pelanggan dan No. Meja terlebih dahulu.');
      return;
    }

    setState(() => _isSubmitting = true);

    try {
      final payload = {
        'namaPelanggan': nama,
        'nomorMeja': meja,
        'catatan': catatan,
        'items': _cart.values
            .map((e) => {
                  'idMenu': e.menu.idMenu,
                  'namaMenu': e.menu.namaMenu,
                  'harga': e.menu.harga,
                  'jumlah': e.jumlah,
                })
            .toList(),
      };

      final uri = Uri.parse('$kApiBaseUrl/api/pesanan');
      final response = await http
          .post(
            uri,
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode(payload),
          )
          .timeout(const Duration(seconds: 10));

      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw Exception('HTTP ${response.statusCode}: ${response.body}');
      }

      if (!mounted) return;
      setState(() {
        _cart.clear();
        _catatanController.clear();
      });
      _showSnack('Pesanan berhasil dikirim!');
    } catch (e) {
      if (!mounted) return;
      _showSnack('Gagal mengirim pesanan: $e');
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  void _showSnack(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Smart Café — Customer Self-Order',
          overflow: TextOverflow.ellipsis,
        ),
        actions: [
          IconButton(
            tooltip: 'Refresh Menu',
            onPressed: _isLoading ? null : _fetchMenus,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: LayoutBuilder(
        builder: (context, constraints) {
          final isWide = constraints.maxWidth >= 720;
          if (isWide) {
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(flex: 3, child: _buildCatalogPanel()),
                const VerticalDivider(width: 1),
                Expanded(flex: 2, child: _buildCartPanel()),
              ],
            );
          }
          return SingleChildScrollView(
            child: Column(
              children: [
                _buildCatalogPanel(shrinkWrap: true),
                const Divider(height: 1),
                _buildCartPanel(shrinkWrap: true),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildCatalogPanel({bool shrinkWrap = false}) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _namaController,
                  decoration: const InputDecoration(
                    labelText: 'Nama Pelanggan',
                    border: OutlineInputBorder(),
                    isDense: true,
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: TextField(
                  controller: _mejaController,
                  decoration: const InputDecoration(
                    labelText: 'No. Meja',
                    border: OutlineInputBorder(),
                    isDense: true,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          const Text(
            'Katalog Menu',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 12),
          if (_isLoading)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 32),
              child: Center(child: CircularProgressIndicator()),
            )
          else if (_errorMessage != null)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 24),
              child: Column(
                children: [
                  Text(
                    _errorMessage!,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 12),
                  ElevatedButton.icon(
                    onPressed: _fetchMenus,
                    icon: const Icon(Icons.refresh),
                    label: const Text('Coba Lagi'),
                  ),
                ],
              ),
            )
          else if (_menus.isEmpty)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 24),
              child: Center(child: Text('Belum ada data menu.')),
            )
          else if (shrinkWrap)
            ListView.separated(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: _menus.length,
              separatorBuilder: (_, __) => const SizedBox(height: 8),
              itemBuilder: (context, index) => _buildMenuCard(_menus[index]),
            )
          else
            Expanded(
              child: ListView.separated(
                itemCount: _menus.length,
                separatorBuilder: (_, __) => const SizedBox(height: 8),
                itemBuilder: (context, index) => _buildMenuCard(_menus[index]),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildMenuCard(MenuItemModel item) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.namaMenu,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    '${item.kategori} • Stok: ${item.stok}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontSize: 12, color: Colors.grey[700]),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    _formatRupiah(item.harga),
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            ElevatedButton(
              onPressed: item.stok > 0 ? () => _addToCart(item) : null,
              child: const Text('Tambah'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildCartPanel({bool shrinkWrap = false}) {
    final entries = _cart.values.toList();
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'Keranjang Pesanan',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 12),
          if (entries.isEmpty)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 32),
              child: Center(child: Text('Keranjang masih kosong.')),
            )
          else if (shrinkWrap)
            ListView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: entries.length,
              itemBuilder: (context, index) => _buildCartRow(entries[index]),
            )
          else
            Expanded(
              child: ListView.builder(
                itemCount: entries.length,
                itemBuilder: (context, index) => _buildCartRow(entries[index]),
              ),
            ),
          const SizedBox(height: 12),
          TextField(
            controller: _catatanController,
            decoration: const InputDecoration(
              labelText: 'Catatan Pesanan (opsional)',
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Total',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
              ),
              Flexible(
                child: Text(
                  _formatRupiah(_totalHarga),
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          FilledButton(
            onPressed: _isSubmitting ? null : _submitOrder,
            child: Text(_isSubmitting ? 'Mengirim...' : 'Kirim Pesanan'),
          ),
        ],
      ),
    );
  }

  Widget _buildCartRow(CartEntry entry) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  entry.menu.namaMenu,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
                Text(
                  _formatRupiah(entry.subtotal),
                  style: const TextStyle(fontSize: 12),
                ),
              ],
            ),
          ),
          IconButton(
            onPressed: () => _changeQty(entry.menu.idMenu, -1),
            icon: const Icon(Icons.remove_circle_outline),
          ),
          Text('${entry.jumlah}'),
          IconButton(
            onPressed: () => _changeQty(entry.menu.idMenu, 1),
            icon: const Icon(Icons.add_circle_outline),
          ),
        ],
      ),
    );
  }
}
