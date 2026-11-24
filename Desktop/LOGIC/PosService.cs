using System.Text;
using SmartCafeDesktop.Data;

namespace SmartCafeDesktop.Logic;

public class PosService
{
    private readonly CafeRepository _repo;

    public PosService(CafeRepository repo)
    {
        _repo = repo;
    }

    public List<KategoriItem> AmbilKategori() => _repo.GetKategori();
    public List<MenuItemModel> AmbilDaftarMenu() => _repo.GetAllMenu();
    public List<PesananItem> AmbilAntreanPesanan() => _repo.GetAllPesanan();

    public (bool Sukses, string Pesan) SimpanMenuBaru(string idMenu, string idKategori, string namaMenu, decimal harga, string statusStok)
    {
        var item = new MenuItemModel
        {
            IdMenu = idMenu,
            IdKategori = idKategori,
            NamaMenu = namaMenu,
            Harga = harga,
            StatusStok = statusStok
        };
        return _repo.TambahMenu(item);
    }

    public (bool Sukses, string Pesan) UbahMenu(string idMenu, string idKategori, string namaMenu, decimal harga, string statusStok)
    {
        var item = new MenuItemModel
        {
            IdMenu = idMenu,
            IdKategori = idKategori,
            NamaMenu = namaMenu,
            Harga = harga,
            StatusStok = statusStok
        };
        return _repo.UpdateMenu(item);
    }

    public (bool Sukses, string Pesan) HapusMenu(string idMenu) => _repo.HapusMenu(idMenu);

    public (bool Sukses, string Pesan) BuatPesananBaru(string idPesanan, string nomorMeja, string namaPelanggan, MenuItemModel? menu, int qty, string? catatan)
    {
        if (menu == null)
            return (false, "Pilih menu terlebih dahulu.");
        return _repo.TambahPesanan(idPesanan, nomorMeja, namaPelanggan, menu, qty, catatan);
    }

    public decimal HitungKembalian(decimal totalBayar, decimal uangBayar)
    {
        if (uangBayar < totalBayar) return 0;
        return uangBayar - totalBayar;
    }

    public (bool Sukses, string Pesan, string StrukText) BayarPesanan(string idPesanan, decimal uangBayar)
    {
        var pesanan = _repo.GetAllPesanan().FirstOrDefault(p => p.IdPesanan == idPesanan);
        var hasil = _repo.ProsesPembayaran(idPesanan, uangBayar);
        if (!hasil.Sukses || pesanan == null || hasil.Transaksi == null)
            return (hasil.Sukses, hasil.Pesan, string.Empty);

        string struk = BuatTeksStruk(pesanan, hasil.Transaksi);
        return (true, hasil.Pesan, struk);
    }

    public string BuatTeksStruk(PesananItem pesanan, TransaksiItem trx)
    {
        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("            SMART CAFE POS              ");
        sb.AppendLine("========================================");
        sb.AppendLine($"ID Transaksi : {trx.IdTransaksi}");
        sb.AppendLine($"ID Pesanan   : {pesanan.IdPesanan}");
        sb.AppendLine($"Meja / Nama  : {pesanan.NomorMeja} / {pesanan.NamaPelanggan}");
        sb.AppendLine($"Waktu Bayar  : {trx.WaktuBayar:dd/MM/yyyy HH:mm}");
        sb.AppendLine("----------------------------------------");
        foreach (var d in pesanan.Details)
        {
            sb.AppendLine($"{d.NamaMenu} x{d.Qty}");
            sb.AppendLine($"   @{d.HargaSatuan:N0} -> Rp {d.Subtotal:N0}");
            if (!string.IsNullOrWhiteSpace(d.Catatan) && d.Catatan != "-")
                sb.AppendLine($"   Catatan: {d.Catatan}");
        }
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"Total Bayar  : Rp {trx.TotalBayar:N0}");
        sb.AppendLine($"Uang Bayar   : Rp {trx.UangBayar:N0}");
        sb.AppendLine($"Kembalian    : Rp {trx.UangKembali:N0}");
        sb.AppendLine("========================================");
        sb.AppendLine("     Terima kasih atas kunjungan Anda!  ");
        sb.AppendLine("========================================");
        return sb.ToString();
    }
}