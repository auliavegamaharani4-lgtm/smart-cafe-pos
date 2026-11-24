# Dokumentasi Proyek Smart Cafe (POS & Self-Order)

## 1. Gambaran Umum Sistem
**Smart Cafe** merupakan sistem terintegrasi yang menghubungkan dua aplikasi dengan basis data terpusat **SmartCafeDB** pada Microsoft SQL Server:
- **Aplikasi Desktop (C# WinForms .NET 8):** Digunakan oleh staf kafe/kasir untuk mengelola master menu dan kategori (CRUD), memantau antrean pesanan masuk, memproses pembayaran kasir, serta menampilkan pratinjau/cetak struk.
- **Aplikasi Mobile (Flutter):** Digunakan oleh pelanggan untuk memasukkan nama dan nomor meja, melihat katalog menu, mengatur keranjang belanja beserta catatan tambahan per item, dan mengirim pesanan (*checkout*).

## 2. Struktur Basis Data (`database.sql`)
Basis data utama yang digunakan adalah **`SmartCafeDB`** dengan lima tabel relasional:
- **`Kategori`**: Menyimpan data kategori menu (`IdKategori` PK, `NamaKategori`).
- **`Menu`**: Menyimpan daftar menu kafe (`IdMenu` PK, `IdKategori` FK, `NamaMenu`, `Harga`, `StatusStok`).
- **`Pesanan`**: Menyimpan data header pesanan pelanggan (`IdPesanan` PK, `NomorMeja`, `NamaPelanggan`, `WaktuPesan`, `TotalHarga`, `StatusPesanan`).
- **`DetailPesanan`**: Menyimpan rincian item dalam setiap pesanan (`IdDetail` PK, `IdPesanan` FK, `IdMenu` FK, `Qty`, `Subtotal`, `Catatan`).
- **`Transaksi`**: Menyimpan data pembayaran kasir (`IdTransaksi` PK, `IdPesanan` FK, `TotalBayar`, `UangBayar`, `UangKembali`, `WaktuBayar`).

Data awal yang tersedia pada skrip mencakup kategori (`Makanan`, `Minuman`, `Snack`) serta beberapa contoh data awal pada tabel `Menu`.

## 3. Langkah Menjalankan Basis Data
1. Buka **SQL Server Management Studio (SSMS)** dan hubungkan ke instance SQL Server lokal.
2. Pastikan database **`SmartCafeDB`** sudah tersedia (atau buat terlebih dahulu sesuai nama pada skrip).
3. Buka dan jalankan file `database.sql` agar seluruh tabel dan data awal terbentuk.
4. Sesuaikan *connection string* atau konfigurasi koneksi pada aplikasi Desktop, API/layanan data (jika digunakan), dan aplikasi Mobile sebelum menjalankan demo.

## 4. Alur Singkat Pengujian (End-to-End)
1. Admin/kasir memastikan data kategori dan menu tersedia melalui aplikasi Desktop.
2. Pelanggan membuka aplikasi Mobile, mengisi nama dan nomor meja, memilih menu yang tersedia, menambahkan catatan bila perlu, lalu melakukan *checkout*.
3. Pesanan masuk ke daftar antrean di aplikasi Desktop untuk diproses.
4. Kasir memproses pembayaran pada pesanan tersebut, mencatat transaksi, mengubah status pesanan menjadi selesai, dan mengecek pratinjau struk.