USE SmartCafeDB;
GO
CREATE TABLE Kategori (
    IdKategori NVARCHAR(20) PRIMARY KEY,
    NamaKategori NVARCHAR(100) NOT NULL
);

CREATE TABLE Menu (
    IdMenu NVARCHAR(20) PRIMARY KEY,
    IdKategori NVARCHAR(20) NOT NULL,
    NamaMenu NVARCHAR(150) NOT NULL,
    Harga DECIMAL(18, 2) NOT NULL,
    StatusStok NVARCHAR(30) NOT NULL DEFAULT 'Tersedia',
    CONSTRAINT FK_Menu_Kategori FOREIGN KEY (IdKategori) REFERENCES Kategori(IdKategori)
);

CREATE TABLE Pesanan (
    IdPesanan NVARCHAR(30) PRIMARY KEY,
    NomorMeja NVARCHAR(20) NOT NULL,
    NamaPelanggan NVARCHAR(100) NOT NULL,
    WaktuPesan DATETIME NOT NULL DEFAULT GETDATE(),
    TotalHarga DECIMAL(18, 2) NOT NULL,
    StatusPesanan NVARCHAR(30) NOT NULL DEFAULT 'Menunggu'
);

CREATE TABLE DetailPesanan (
    IdDetail INT IDENTITY(1,1) PRIMARY KEY,
    IdPesanan NVARCHAR(30) NOT NULL,
    NamaMenu NVARCHAR(150) NOT NULL,
    Qty INT NOT NULL,
    HargaSatuan DECIMAL(18, 2) NOT NULL,
    Catatan NVARCHAR(255) NULL,
    CONSTRAINT FK_DetailPesanan_Pesanan FOREIGN KEY (IdPesanan) REFERENCES Pesanan(IdPesanan) ON DELETE CASCADE
);

CREATE TABLE Transaksi (
    IdTransaksi NVARCHAR(40) PRIMARY KEY,
    IdPesanan NVARCHAR(30) NOT NULL,
    TotalBayar DECIMAL(18, 2) NOT NULL,
    UangBayar DECIMAL(18, 2) NOT NULL,
    UangKembali DECIMAL(18, 2) NOT NULL,
    WaktuBayar DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Transaksi_Pesanan FOREIGN KEY (IdPesanan) REFERENCES Pesanan(IdPesanan)
);

INSERT INTO Kategori (IdKategori, NamaKategori) VALUES
('KAT01', 'Makanan'),
('KAT02', 'Minuman'),
('KAT03', 'Snack');

INSERT INTO Menu (IdMenu, IdKategori, NamaMenu, Harga, StatusStok) VALUES
('MN01', 'KAT02', 'Kopi Susu Gula Aren', 18000, 'Tersedia'),
('MN02', 'KAT02', 'Matcha Latte', 22000, 'Tersedia'),
('MN03', 'KAT01', 'Nasi Goreng Kampung', 25000, 'Tersedia'),
('MN04', 'KAT03', 'Kentang Goreng', 15000, 'Habis');

