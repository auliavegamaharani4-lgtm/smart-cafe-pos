using Microsoft.Data.SqlClient;

namespace SmartCafeDesktop.Data;

public class KategoriItem
{
    public string IdKategori { get; set; } = string.Empty;
    public string NamaKategori { get; set; } = string.Empty;
    public override string ToString() => $"{IdKategori} - {NamaKategori}";
}

public class MenuItemModel
{
    public string IdMenu { get; set; } = string.Empty;
    public string IdKategori { get; set; } = string.Empty;
    public string NamaKategori { get; set; } = string.Empty;
    public string NamaMenu { get; set; } = string.Empty;
    public decimal Harga { get; set; }
    public string StatusStok { get; set; } = "Tersedia";
}

public class DetailPesananItem
{
    public string NamaMenu { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal HargaSatuan { get; set; }
    public decimal Subtotal => Qty * HargaSatuan;
    public string? Catatan { get; set; }
}

public class PesananItem
{
    public string IdPesanan { get; set; } = string.Empty;
    public string NomorMeja { get; set; } = string.Empty;
    public string NamaPelanggan { get; set; } = string.Empty;
    public DateTime WaktuPesan { get; set; } = DateTime.Now;
    public decimal TotalHarga { get; set; }
    public string StatusPesanan { get; set; } = "Menunggu";
    public List<DetailPesananItem> Details { get; set; } = new();
}

public class TransaksiItem
{
    public string IdTransaksi { get; set; } = string.Empty;
    public string IdPesanan { get; set; } = string.Empty;
    public decimal TotalBayar { get; set; }
    public decimal UangBayar { get; set; }
    public decimal UangKembali { get; set; }
    public DateTime WaktuBayar { get; set; } = DateTime.Now;
}

public class CafeRepository
{
    private string ConnStr => DatabaseConfig.ConnectionString;

    public List<KategoriItem> GetKategori()
    {
        var list = new List<KategoriItem>();
        using var conn = new SqlConnection(ConnStr);
        conn.Open();

        using var cmd = new SqlCommand("SELECT IdKategori, NamaKategori FROM Kategori ORDER BY IdKategori", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new KategoriItem
            {
                IdKategori = reader["IdKategori"]?.ToString() ?? string.Empty,
                NamaKategori = reader["NamaKategori"]?.ToString() ?? string.Empty
            });
        }
        return list;
    }

    public List<MenuItemModel> GetAllMenu()
    {
        var list = new List<MenuItemModel>();
        using var conn = new SqlConnection(ConnStr);
        conn.Open();

        const string sql = @"
            SELECT m.IdMenu, m.IdKategori, ISNULL(k.NamaKategori, '') AS NamaKategori,
                   m.NamaMenu, m.Harga, m.StatusStok
            FROM Menu m
            LEFT JOIN Kategori k ON m.IdKategori = k.IdKategori
            ORDER BY m.IdMenu";

        using var cmd = new SqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new MenuItemModel
            {
                IdMenu = reader["IdMenu"]?.ToString() ?? string.Empty,
                IdKategori = reader["IdKategori"]?.ToString() ?? string.Empty,
                NamaKategori = reader["NamaKategori"]?.ToString() ?? string.Empty,
                NamaMenu = reader["NamaMenu"]?.ToString() ?? string.Empty,
                Harga = reader["Harga"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Harga"]),
                StatusStok = reader["StatusStok"]?.ToString() ?? "Tersedia"
            });
        }
        return list;
    }

    public List<PesananItem> GetAllPesanan()
    {
        var pesananMap = new Dictionary<string, PesananItem>();
        using var conn = new SqlConnection(ConnStr);
        conn.Open();

        const string sqlPesanan = @"
            SELECT IdPesanan, NomorMeja, NamaPelanggan, WaktuPesan, TotalHarga, StatusPesanan
            FROM Pesanan
            ORDER BY WaktuPesan DESC";

        using (var cmd = new SqlCommand(sqlPesanan, conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string id = reader["IdPesanan"]?.ToString() ?? string.Empty;
                pesananMap[id] = new PesananItem
                {
                    IdPesanan = id,
                    NomorMeja = reader["NomorMeja"]?.ToString() ?? string.Empty,
                    NamaPelanggan = reader["NamaPelanggan"]?.ToString() ?? string.Empty,
                    WaktuPesan = reader["WaktuPesan"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["WaktuPesan"]),
                    TotalHarga = reader["TotalHarga"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalHarga"]),
                    StatusPesanan = reader["StatusPesanan"]?.ToString() ?? "Menunggu",
                    Details = new List<DetailPesananItem>()
                };
            }
        }

        if (pesananMap.Count > 0)
        {
            const string sqlDetail = @"
                SELECT IdPesanan, NamaMenu, Qty, HargaSatuan, Catatan
                FROM DetailPesanan
                ORDER BY IdDetail";

            using var cmdDetail = new SqlCommand(sqlDetail, conn);
            using var readerDetail = cmdDetail.ExecuteReader();
            while (readerDetail.Read())
            {
                string idPesanan = readerDetail["IdPesanan"]?.ToString() ?? string.Empty;
                if (pesananMap.TryGetValue(idPesanan, out var pesanan))
                {
                    pesanan.Details.Add(new DetailPesananItem
                    {
                        NamaMenu = readerDetail["NamaMenu"]?.ToString() ?? string.Empty,
                        Qty = readerDetail["Qty"] == DBNull.Value ? 0 : Convert.ToInt32(readerDetail["Qty"]),
                        HargaSatuan = readerDetail["HargaSatuan"] == DBNull.Value ? 0 : Convert.ToDecimal(readerDetail["HargaSatuan"]),
                        Catatan = readerDetail["Catatan"] == DBNull.Value ? "-" : readerDetail["Catatan"]?.ToString()
                    });
                }
            }
        }

        return pesananMap.Values.OrderByDescending(p => p.WaktuPesan).ToList();
    }

    public (bool Sukses, string Pesan) TambahMenu(MenuItemModel item)
    {
        if (string.IsNullOrWhiteSpace(item.IdMenu))
            return (false, "Kode/ID Menu wajib diisi.");
        if (string.IsNullOrWhiteSpace(item.NamaMenu))
            return (false, "Nama Menu wajib diisi.");
        if (item.Harga <= 0)
            return (false, "Harga menu harus lebih dari 0.");

        string idBersih = item.IdMenu.Trim().ToUpper();
        string namaBersih = item.NamaMenu.Trim();

        using var conn = new SqlConnection(ConnStr);
        conn.Open();

        using (var cekCmd = new SqlCommand("SELECT COUNT(1) FROM Menu WHERE IdMenu = @IdMenu", conn))
        {
            cekCmd.Parameters.AddWithValue("@IdMenu", idBersih);
            if (Convert.ToInt32(cekCmd.ExecuteScalar()) > 0)
                return (false, $"Kode menu '{idBersih}' sudah digunakan.");
        }

        const string insertSql = @"
            INSERT INTO Menu (IdMenu, IdKategori, NamaMenu, Harga, StatusStok)
            VALUES (@IdMenu, @IdKategori, @NamaMenu, @Harga, @StatusStok)";

        using var cmd = new SqlCommand(insertSql, conn);
        cmd.Parameters.AddWithValue("@IdMenu", idBersih);
        cmd.Parameters.AddWithValue("@IdKategori", item.IdKategori);
        cmd.Parameters.AddWithValue("@NamaMenu", namaBersih);
        cmd.Parameters.AddWithValue("@Harga", item.Harga);
        cmd.Parameters.AddWithValue("@StatusStok", string.IsNullOrWhiteSpace(item.StatusStok) ? "Tersedia" : item.StatusStok);
        cmd.ExecuteNonQuery();

        return (true, "Menu berhasil ditambahkan ke database.");
    }

    public (bool Sukses, string Pesan) UpdateMenu(MenuItemModel item)
    {
        if (string.IsNullOrWhiteSpace(item.NamaMenu))
            return (false, "Nama Menu wajib diisi.");
        if (item.Harga <= 0)
            return (false, "Harga menu harus lebih dari 0.");

        const string updateSql = @"
            UPDATE Menu
            SET IdKategori = @IdKategori,
                NamaMenu = @NamaMenu,
                Harga = @Harga,
                StatusStok = @StatusStok
            WHERE IdMenu = @IdMenu";

        using var conn = new SqlConnection(ConnStr);
        conn.Open();
        using var cmd = new SqlCommand(updateSql, conn);
        cmd.Parameters.AddWithValue("@IdMenu", item.IdMenu.Trim().ToUpper());
        cmd.Parameters.AddWithValue("@IdKategori", item.IdKategori);
        cmd.Parameters.AddWithValue("@NamaMenu", item.NamaMenu.Trim());
        cmd.Parameters.AddWithValue("@Harga", item.Harga);
        cmd.Parameters.AddWithValue("@StatusStok", item.StatusStok);

        int affected = cmd.ExecuteNonQuery();
        return affected > 0
            ? (true, "Menu berhasil diperbarui di database.")
            : (false, "Data menu tidak ditemukan.");
    }

    public (bool Sukses, string Pesan) HapusMenu(string idMenu)
    {
        using var conn = new SqlConnection(ConnStr);
        conn.Open();
        using var cmd = new SqlCommand("DELETE FROM Menu WHERE IdMenu = @IdMenu", conn);
        cmd.Parameters.AddWithValue("@IdMenu", idMenu.Trim().ToUpper());

        int affected = cmd.ExecuteNonQuery();
        return affected > 0
            ? (true, "Menu berhasil dihapus dari database.")
            : (false, "Menu tidak ditemukan.");
    }

    public (bool Sukses, string Pesan) TambahPesanan(string idPesanan, string nomorMeja, string namaPelanggan, MenuItemModel menu, int qty, string? catatan)
    {
        if (string.IsNullOrWhiteSpace(idPesanan))
            return (false, "ID Pesanan wajib diisi.");
        if (string.IsNullOrWhiteSpace(nomorMeja))
            return (false, "Nomor Meja wajib diisi.");
        if (string.IsNullOrWhiteSpace(namaPelanggan))
            return (false, "Nama Pelanggan wajib diisi.");
        if (qty <= 0)
            return (false, "Qty harus lebih dari 0.");

        string idBersih = idPesanan.Trim().ToUpper();
        decimal subtotal = qty * menu.Harga;
        DateTime waktu = DateTime.Now;
        string catatanBersih = string.IsNullOrWhiteSpace(catatan) ? "-" : catatan.Trim();

        using var conn = new SqlConnection(ConnStr);
        conn.Open();

        using (var cekCmd = new SqlCommand("SELECT COUNT(1) FROM Pesanan WHERE IdPesanan = @IdPesanan", conn))
        {
            cekCmd.Parameters.AddWithValue("@IdPesanan", idBersih);
            if (Convert.ToInt32(cekCmd.ExecuteScalar()) > 0)
                return (false, $"ID Pesanan '{idBersih}' sudah ada.");
        }

        using var tx = conn.BeginTransaction();
        try
        {
            const string insertPesanan = @"
                INSERT INTO Pesanan (IdPesanan, NomorMeja, NamaPelanggan, WaktuPesan, TotalHarga, StatusPesanan)
                VALUES (@IdPesanan, @NomorMeja, @NamaPelanggan, @WaktuPesan, @TotalHarga, 'Menunggu')";

            using (var cmdPesanan = new SqlCommand(insertPesanan, conn, tx))
            {
                cmdPesanan.Parameters.AddWithValue("@IdPesanan", idBersih);
                cmdPesanan.Parameters.AddWithValue("@NomorMeja", nomorMeja.Trim().ToUpper());
                cmdPesanan.Parameters.AddWithValue("@NamaPelanggan", namaPelanggan.Trim());
                cmdPesanan.Parameters.AddWithValue("@WaktuPesan", waktu);
                cmdPesanan.Parameters.AddWithValue("@TotalHarga", subtotal);
                cmdPesanan.ExecuteNonQuery();
            }

            const string insertDetail = @"
                INSERT INTO DetailPesanan (IdPesanan, NamaMenu, Qty, HargaSatuan, Catatan)
                VALUES (@IdPesanan, @NamaMenu, @Qty, @HargaSatuan, @Catatan)";

            using (var cmdDetail = new SqlCommand(insertDetail, conn, tx))
            {
                cmdDetail.Parameters.AddWithValue("@IdPesanan", idBersih);
                cmdDetail.Parameters.AddWithValue("@NamaMenu", menu.NamaMenu);
                cmdDetail.Parameters.AddWithValue("@Qty", qty);
                cmdDetail.Parameters.AddWithValue("@HargaSatuan", menu.Harga);
                cmdDetail.Parameters.AddWithValue("@Catatan", catatanBersih);
                cmdDetail.ExecuteNonQuery();
            }

            tx.Commit();
            return (true, "Pesanan customer baru berhasil disimpan ke database.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Gagal menyimpan pesanan: {ex.Message}");
        }
    }

    public (bool Sukses, string Pesan, TransaksiItem? Transaksi) ProsesPembayaran(string idPesanan, decimal uangBayar)
    {
        using var conn = new SqlConnection(ConnStr);
        conn.Open();

        decimal totalHarga;
        string statusPesanan;

        const string selectSql = "SELECT TotalHarga, StatusPesanan FROM Pesanan WHERE IdPesanan = @IdPesanan";
        using (var selectCmd = new SqlCommand(selectSql, conn))
        {
            selectCmd.Parameters.AddWithValue("@IdPesanan", idPesanan);
            using var reader = selectCmd.ExecuteReader();
            if (!reader.Read())
                return (false, "Pesanan tidak ditemukan.", null);

            totalHarga = reader["TotalHarga"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalHarga"]);
            statusPesanan = reader["StatusPesanan"]?.ToString() ?? "Menunggu";
        }

        if (statusPesanan == "Selesai")
            return (false, "Pesanan ini sudah lunas/selesai.", null);

        if (uangBayar < totalHarga)
            return (false, "Uang bayar kurang dari total tagihan.", null);

        decimal kembali = uangBayar - totalHarga;
        DateTime waktuBayar = DateTime.Now;
        string idTransaksi = $"TRX-{waktuBayar:yyyyMMddHHmmss}";

        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmdUpdate = new SqlCommand("UPDATE Pesanan SET StatusPesanan = 'Selesai' WHERE IdPesanan = @IdPesanan", conn, tx))
            {
                cmdUpdate.Parameters.AddWithValue("@IdPesanan", idPesanan);
                cmdUpdate.ExecuteNonQuery();
            }

            const string insertTrx = @"
                INSERT INTO Transaksi (IdTransaksi, IdPesanan, TotalBayar, UangBayar, UangKembali, WaktuBayar)
                VALUES (@IdTransaksi, @IdPesanan, @TotalBayar, @UangBayar, @UangKembali, @WaktuBayar)";

            using (var cmdTrx = new SqlCommand(insertTrx, conn, tx))
            {
                cmdTrx.Parameters.AddWithValue("@IdTransaksi", idTransaksi);
                cmdTrx.Parameters.AddWithValue("@IdPesanan", idPesanan);
                cmdTrx.Parameters.AddWithValue("@TotalBayar", totalHarga);
                cmdTrx.Parameters.AddWithValue("@UangBayar", uangBayar);
                cmdTrx.Parameters.AddWithValue("@UangKembali", kembali);
                cmdTrx.Parameters.AddWithValue("@WaktuBayar", waktuBayar);
                cmdTrx.ExecuteNonQuery();
            }

            tx.Commit();

            var trx = new TransaksiItem
            {
                IdTransaksi = idTransaksi,
                IdPesanan = idPesanan,
                TotalBayar = totalHarga,
                UangBayar = uangBayar,
                UangKembali = kembali,
                WaktuBayar = waktuBayar
            };

            return (true, "Pembayaran berhasil! Status pesanan diperbarui menjadi Selesai.", trx);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Gagal memproses pembayaran: {ex.Message}", null);
        }
    }
}