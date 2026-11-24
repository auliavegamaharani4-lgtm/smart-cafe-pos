using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();
app.UseCors();

const string connString =
    "Server=VEGA\\SQLEXPRESS;Database=SmartCafeDB;Trusted_Connection=True;TrustServerCertificate=True;";

// Ambil daftar menu beserta nama kategori
app.MapGet("/api/menu", async () =>
{
    var items = new List<object>();
    await using var conn = new SqlConnection(connString);
    await conn.OpenAsync();

    var sql = @"
        SELECT m.IdMenu, m.NamaMenu, m.Harga, 100 AS Stok, k.NamaKategori
        FROM Menu m
        LEFT JOIN Kategori k ON m.IdKategori = k.IdKategori";

    await using var cmd = new SqlCommand(sql, conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        items.Add(new
        {
            idMenu = reader["IdMenu"],
            namaMenu = reader["NamaMenu"]?.ToString() ?? "",
            harga = Convert.ToDecimal(reader["Harga"]),
            stok = Convert.ToInt32(reader["Stok"]),
            kategori = reader["NamaKategori"]?.ToString() ?? "Umum"
        });
    }
    return Results.Ok(items);
});

// Simpan pesanan baru + detail pesanan
app.MapPost("/api/pesanan", async (OrderRequest req) =>
{
    if (req.Items == null || req.Items.Count == 0)
        return Results.BadRequest(new { message = "Keranjang masih kosong." });

    await using var conn = new SqlConnection(connString);
    await conn.OpenAsync();
    await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

    try
    {
        decimal totalHarga = req.Items.Sum(i => i.Harga * i.Jumlah);

        var insertPesananSql = @"
            INSERT INTO Pesanan (NamaPelanggan, NomorMeja, Catatan, TotalHarga, StatusPesanan, TanggalPesan)
            OUTPUT INSERTED.IdPesanan
            VALUES (@NamaPelanggan, @NomorMeja, @Catatan, @TotalHarga, 'Menunggu', GETDATE())";

        int idPesanan;
        await using (var cmdPesanan = new SqlCommand(insertPesananSql, conn, tx))
        {
            cmdPesanan.Parameters.AddWithValue("@NamaPelanggan", req.NamaPelanggan.Trim());
            cmdPesanan.Parameters.AddWithValue("@NomorMeja", req.NomorMeja.Trim());
            cmdPesanan.Parameters.AddWithValue("@Catatan", (object?)req.Catatan?.Trim() ?? DBNull.Value);
            cmdPesanan.Parameters.AddWithValue("@TotalHarga", totalHarga);

            var result = await cmdPesanan.ExecuteScalarAsync();
            idPesanan = Convert.ToInt32(result);
        }

        var insertDetailSql = @"
            INSERT INTO DetailPesanan (IdPesanan, IdMenu, Jumlah, HargaSatuan, Subtotal)
            VALUES (@IdPesanan, @IdMenu, @Jumlah, @HargaSatuan, @Subtotal)";

        foreach (var item in req.Items)
        {
            await using var cmdDetail = new SqlCommand(insertDetailSql, conn, tx);
            cmdDetail.Parameters.AddWithValue("@IdPesanan", idPesanan);
            cmdDetail.Parameters.AddWithValue("@IdMenu", item.IdMenu);
            cmdDetail.Parameters.AddWithValue("@Jumlah", item.Jumlah);
            cmdDetail.Parameters.AddWithValue("@HargaSatuan", item.Harga);
            cmdDetail.Parameters.AddWithValue("@Subtotal", item.Harga * item.Jumlah);
            await cmdDetail.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return Results.Ok(new { message = "Pesanan berhasil dikirim!", idPesanan, totalHarga });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.Problem(ex.Message);
    }
});

app.Run("http://localhost:5180");

record OrderItemRequest(int IdMenu, string NamaMenu, decimal Harga, int Jumlah);
record OrderRequest(string NamaPelanggan, string NomorMeja, string? Catatan, List<OrderItemRequest> Items);
