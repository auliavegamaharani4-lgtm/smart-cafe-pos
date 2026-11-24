CREATE TABLE KategoriMenu (
    IdKategori VARCHAR(20) PRIMARY KEY,
    NamaKategori VARCHAR(50) NOT NULL
);

CREATE TABLE Menu (
    IdMenu VARCHAR(20) PRIMARY KEY,
    IdKategori VARCHAR(20) NOT NULL,
    NamaMenu VARCHAR(100) NOT NULL,
    Harga DECIMAL(12,2) NOT NULL CHECK (Harga > 0),
    StatusStok VARCHAR(20) NOT NULL DEFAULT 'Tersedia',
    CONSTRAINT FK_Menu_Kategori FOREIGN KEY (IdKategori)
        REFERENCES KategoriMenu(IdKategori)
);

CREATE TABLE Pesanan (
    IdPesanan VARCHAR(30) PRIMARY KEY,
    NomorMeja VARCHAR(20) NOT NULL,
    NamaPelanggan VARCHAR(100) NOT NULL,
    WaktuPesan DATETIME NOT NULL DEFAULT GETDATE(),
    TotalHarga DECIMAL(12,2) NOT NULL DEFAULT 0,
    StatusPesanan VARCHAR(25) NOT NULL DEFAULT 'Menunggu'
);

CREATE TABLE DetailPesanan (
    IdDetail VARCHAR(30) PRIMARY KEY,
    IdPesanan VARCHAR(30) NOT NULL,
    IdMenu VARCHAR(20) NOT NULL,
    Qty INT NOT NULL CHECK (Qty > 0),
    Subtotal DECIMAL(12,2) NOT NULL,
    Catatan VARCHAR(200) NULL,
    CONSTRAINT FK_Detail_Pesanan FOREIGN KEY (IdPesanan)
        REFERENCES Pesanan(IdPesanan),
    CONSTRAINT FK_Detail_Menu FOREIGN KEY (IdMenu)
        REFERENCES Menu(IdMenu)
);

CREATE TABLE TransaksiKasir (
    IdTransaksi VARCHAR(30) PRIMARY KEY,
    IdPesanan VARCHAR(30) NOT NULL,
    TotalBayar DECIMAL(12,2) NOT NULL,
    UangBayar DECIMAL(12,2) NOT NULL,
    UangKembali DECIMAL(12,2) NOT NULL,
    WaktuBayar DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Transaksi_Pesanan FOREIGN KEY (IdPesanan)
        REFERENCES Pesanan(IdPesanan)
);

INSERT INTO KategoriMenu (IdKategori, NamaKategori) VALUES
('KAT01', 'Makanan'),
('KAT02', 'Minuman'),
('KAT03', 'Snack');

INSERT INTO Menu (IdMenu, IdKategori, NamaMenu, Harga, StatusStok) VALUES
('MN01', 'KAT02', 'Kopi Susu Gula Aren', 18000, 'Tersedia'),
('MN02', 'KAT02', 'Matcha Latte', 22000, 'Tersedia'),
('MN03', 'KAT01', 'Nasi Goreng Kampung', 25000, 'Tersedia'),
('MN04', 'KAT03', 'Kentang Goreng', 15000, 'Habis');
