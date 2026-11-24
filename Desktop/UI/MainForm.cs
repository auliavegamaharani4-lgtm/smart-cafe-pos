using System.Drawing.Printing;
using SmartCafeDesktop.Data;
using SmartCafeDesktop.Logic;

namespace SmartCafeDesktop.UI;

public partial class MainForm : Form
{
    private readonly PosService _posService;
    private string _strukTerakhir = string.Empty;

    private static readonly Color BgCream = Color.FromArgb(251, 248, 243);
    private static readonly Color CardWhite = Color.FromArgb(255, 255, 255);
    private static readonly Color EspressoDark = Color.FromArgb(58, 34, 19);
    private static readonly Color CaramelAccent = Color.FromArgb(217, 119, 6);
    private static readonly Color CaramelHover = Color.FromArgb(180, 83, 9);
    private static readonly Color SoftBorder = Color.FromArgb(229, 221, 210);
    private static readonly Color TextMuted = Color.FromArgb(115, 102, 92);
    private static readonly Color RowAlt = Color.FromArgb(250, 245, 238);
    private static readonly Color SelectionSoft = Color.FromArgb(254, 243, 199);

    private readonly List<Button> _navButtons = new();
    private readonly List<Panel> _pages = new();

    private DataGridView _dgvMenu = null!;
    private TextBox _txtIdMenu = null!;
    private TextBox _txtNamaMenu = null!;
    private NumericUpDown _numHargaMenu = null!;
    private ComboBox _cmbKategori = null!;
    private ComboBox _cmbStatusStok = null!;

    private DataGridView _dgvAntrean = null!;
    private DataGridView _dgvDetailPesanan = null!;
    private Label _lblRingkasanAntrean = null!;
    private TextBox _txtIdPesanan = null!;
    private TextBox _txtNoMeja = null!;
    private TextBox _txtNamaPelanggan = null!;
    private ComboBox _cmbMenuPesanan = null!;
    private NumericUpDown _numQtyPesanan = null!;
    private TextBox _txtCatatanPesanan = null!;

    private ComboBox _cmbPesananKasir = null!;
    private Label _lblInfoPelanggan = null!;
    private Label _lblTotalBayar = null!;
    private NumericUpDown _numUangBayar = null!;
    private Label _lblKembalian = null!;
    private RichTextBox _rtbStruk = null!;

    public MainForm()
    {
        var repo = new CafeRepository();
        _posService = new PosService(repo);

        Text = "Smart Café POS & Back-Office";
        Size = new Size(1180, 740);
        MinimumSize = new Size(1020, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgCream;
        Font = new Font("Segoe UI", 10F);

        BuildModernLayout();
        MuatDataAwal();
        PilihTab(0);
    }

    private void BuildModernLayout()
    {
        var headerBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = EspressoDark
        };

        var leftHeader = new Panel
        {
            Dock = DockStyle.Left,
            Width = 420,
            BackColor = EspressoDark
        };

        var lblBrand = new Label
        {
            Text = "SMART CAFÉ POS",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 22)
        };

        var lblSubBrand = new Label
        {
            Text = "Back-Office & Kasir",
            ForeColor = Color.FromArgb(253, 230, 138),
            Font = new Font("Segoe UI", 9.5F),
            AutoSize = true,
            Location = new Point(215, 26)
        };

        leftHeader.Controls.AddRange(new Control[] { lblBrand, lblSubBrand });

        var rightHeader = new Panel
        {
            Dock = DockStyle.Right,
            Width = 510,
            BackColor = EspressoDark
        };

        string[] judulTab = { "Master Menu", "Antrean Pesanan", "Kasir & Struk" };
        _navButtons.Clear();

        int xPos = 10;
        for (int i = 0; i < judulTab.Length; i++)
        {
            int targetIdx = i;
            var btnNav = new Button
            {
                Text = judulTab[i],
                Location = new Point(xPos, 16),
                Size = new Size(156, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNav.FlatAppearance.BorderSize = 0;
            btnNav.Click += (_, _) => PilihTab(targetIdx);
            _navButtons.Add(btnNav);
            rightHeader.Controls.Add(btnNav);
            xPos += 164;
        }

        headerBar.Controls.Add(rightHeader);
        headerBar.Controls.Add(leftHeader);

        var contentContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            BackColor = BgCream
        };

        var pageMenu = new Panel { Dock = DockStyle.Fill, BackColor = BgCream };
        var pageAntrean = new Panel { Dock = DockStyle.Fill, BackColor = BgCream };
        var pageKasir = new Panel { Dock = DockStyle.Fill, BackColor = BgCream };

        BuildPageMenu(pageMenu);
        BuildPageAntrean(pageAntrean);
        BuildPageKasir(pageKasir);

        _pages.AddRange(new[] { pageMenu, pageAntrean, pageKasir });
        contentContainer.Controls.AddRange(new Control[] { pageMenu, pageAntrean, pageKasir });

        Controls.Add(contentContainer);
        Controls.Add(headerBar);
    }

    private void PilihTab(int index)
    {
        for (int i = 0; i < _pages.Count; i++)
        {
            _pages[i].Visible = (i == index);
            if (i == index)
            {
                _navButtons[i].BackColor = CaramelAccent;
                _navButtons[i].ForeColor = Color.White;
            }
            else
            {
                _navButtons[i].BackColor = Color.FromArgb(84, 51, 30);
                _navButtons[i].ForeColor = Color.FromArgb(245, 235, 224);
            }
        }
    }

    private void BuildPageMenu(Panel page)
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 680,
            BackColor = BgCream
        };

        var cardTable = CreateCardPanel();
        cardTable.Padding = new Padding(14);

        var lblTableTitle = new Label
        {
            Text = "Daftar Menu & Ketersediaan Stok",
            Dock = DockStyle.Top,
            Height = 32,
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            ForeColor = EspressoDark
        };

        _dgvMenu = CreateStyledGrid();
        _dgvMenu.SelectionChanged += (_, _) => PilihBarisMenu();
        _dgvMenu.CellFormatting += (_, e) =>
        {
            if (_dgvMenu.Columns[e.ColumnIndex].Name == "StatusStok" && e.Value != null)
            {
                string stok = e.Value.ToString() ?? "";
                if (stok.Equals("Tersedia", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(21, 128, 61);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(185, 28, 28);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
            }
            else if (_dgvMenu.Columns[e.ColumnIndex].Name == "Harga" && e.Value is decimal harga)
            {
                e.Value = $"Rp {harga:N0}";
                e.FormattingApplied = true;
            }
        };

        cardTable.Controls.Add(_dgvMenu);
        cardTable.Controls.Add(lblTableTitle);
        split.Panel1.Controls.Add(cardTable);

        var cardForm = CreateCardPanel();
        cardForm.Padding = new Padding(20);

        var lblFormTitle = new Label
        {
            Text = "Kelola Data Menu",
            Location = new Point(20, 16),
            Size = new Size(340, 30),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = EspressoDark
        };
        cardForm.Controls.Add(lblFormTitle);

        int y = 56;
        void AddLabel(string text)
        {
            cardForm.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(20, y),
                Size = new Size(340, 22),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            });
            y += 24;
        }

        AddLabel("Kode / ID Menu");
        _txtIdMenu = CreateStyledTextBox(20, y, 340);
        cardForm.Controls.Add(_txtIdMenu);
        y += 42;

        AddLabel("Nama Menu");
        _txtNamaMenu = CreateStyledTextBox(20, y, 340);
        cardForm.Controls.Add(_txtNamaMenu);
        y += 42;

        AddLabel("Kategori");
        _cmbKategori = new ComboBox
        {
            Location = new Point(20, y),
            Size = new Size(340, 32),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = BgCream
        };
        cardForm.Controls.Add(_cmbKategori);
        y += 42;

        AddLabel("Harga (Rp)");
        _numHargaMenu = new NumericUpDown
        {
            Location = new Point(20, y),
            Size = new Size(340, 32),
            Maximum = 10000000,
            Increment = 1000,
            ThousandsSeparator = true
        };
        cardForm.Controls.Add(_numHargaMenu);
        y += 42;

        AddLabel("Status Stok");
        _cmbStatusStok = new ComboBox
        {
            Location = new Point(20, y),
            Size = new Size(340, 32),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = BgCream
        };
        _cmbStatusStok.Items.AddRange(new object[] { "Tersedia", "Habis" });
        _cmbStatusStok.SelectedIndex = 0;
        cardForm.Controls.Add(_cmbStatusStok);
        y += 50;

        var btnTambah = CreateAccentButton("+ Tambah", new Point(20, y), new Size(106, 38));
        var btnUpdate = CreateSecondaryButton("Update", new Point(134, y), new Size(106, 38));
        var btnHapus = CreateDangerButton("Hapus", new Point(248, y), new Size(112, 38));

        btnTambah.Click += (_, _) =>
        {
            var kat = _cmbKategori.SelectedItem as KategoriItem;
            var res = _posService.SimpanMenuBaru(
                _txtIdMenu.Text,
                kat?.IdKategori ?? "",
                _txtNamaMenu.Text,
                _numHargaMenu.Value,
                _cmbStatusStok.Text);
            MessageBox.Show(res.Pesan, "Informasi", MessageBoxButtons.OK,
                res.Sukses ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (res.Sukses)
            {
                RefreshSemuaData();
                BersihkanFormMenu();
            }
        };

        btnUpdate.Click += (_, _) =>
        {
            var kat = _cmbKategori.SelectedItem as KategoriItem;
            var res = _posService.UbahMenu(
                _txtIdMenu.Text,
                kat?.IdKategori ?? "",
                _txtNamaMenu.Text,
                _numHargaMenu.Value,
                _cmbStatusStok.Text);
            MessageBox.Show(res.Pesan, "Informasi", MessageBoxButtons.OK,
                res.Sukses ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (res.Sukses) RefreshSemuaData();
        };

        btnHapus.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtIdMenu.Text)) return;
            var res = _posService.HapusMenu(_txtIdMenu.Text);
            MessageBox.Show(res.Pesan, "Informasi", MessageBoxButtons.OK,
                res.Sukses ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (res.Sukses)
            {
                RefreshSemuaData();
                BersihkanFormMenu();
            }
        };

        cardForm.Controls.AddRange(new Control[] { btnTambah, btnUpdate, btnHapus });
        split.Panel2.Controls.Add(cardForm);
        page.Controls.Add(split);
    }

    private void BuildPageAntrean(Panel page)
    {
        var topCard = CreateCardPanel();
        topCard.Dock = DockStyle.Top;
        topCard.Height = 112;
        topCard.Padding = new Padding(14, 10, 14, 10);

        var lblJudulInput = new Label
        {
            Text = "Tambah Pesanan / Pelanggan Baru",
            Location = new Point(14, 8),
            Size = new Size(300, 22),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = EspressoDark
        };

        _txtIdPesanan = CreateStyledTextBox(14, 32, 105);
        _txtIdPesanan.PlaceholderText = "ID (ORD-003)";

        _txtNoMeja = CreateStyledTextBox(125, 32, 85);
        _txtNoMeja.PlaceholderText = "Meja (M03)";

        _txtNamaPelanggan = CreateStyledTextBox(216, 32, 165);
        _txtNamaPelanggan.PlaceholderText = "Nama Pelanggan";

        _cmbMenuPesanan = new ComboBox
        {
            Location = new Point(387, 32),
            Size = new Size(210, 32),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = BgCream
        };

        _numQtyPesanan = new NumericUpDown
        {
            Location = new Point(603, 32),
            Size = new Size(65, 32),
            Minimum = 1,
            Maximum = 100,
            Value = 1
        };

        _txtCatatanPesanan = CreateStyledTextBox(674, 32, 150);
        _txtCatatanPesanan.PlaceholderText = "Catatan (opsional)";

        var btnBuatPesanan = CreateAccentButton("+ Buat Pesanan", new Point(830, 31), new Size(145, 34));
        btnBuatPesanan.Click += (_, _) => BuatPesananBaruKlik();

        var btnRefresh = CreateSecondaryButton("Refresh", new Point(981, 31), new Size(95, 34));
        btnRefresh.Click += (_, _) => RefreshSemuaData();

        _lblRingkasanAntrean = new Label
        {
            Text = "Pilih salah satu pesanan di sebelah kiri untuk melihat rincian item.",
            Location = new Point(14, 72),
            Size = new Size(1060, 24),
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9F)
        };

        topCard.Controls.AddRange(new Control[]
        {
            lblJudulInput, _txtIdPesanan, _txtNoMeja, _txtNamaPelanggan,
            _cmbMenuPesanan, _numQtyPesanan, _txtCatatanPesanan,
            btnBuatPesanan, btnRefresh, _lblRingkasanAntrean
        });

        var spacer = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = BgCream };

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 650,
            BackColor = BgCream
        };

        var cardKiri = CreateCardPanel();
        cardKiri.Padding = new Padding(14);
        var lblKiri = new Label
        {
            Text = "Daftar Pesanan Masuk",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = EspressoDark
        };
        _dgvAntrean = CreateStyledGrid();
        _dgvAntrean.SelectionChanged += (_, _) => TampilkanDetailPesananTerpilih();
        _dgvAntrean.CellFormatting += (_, e) =>
        {
            if (_dgvAntrean.Columns[e.ColumnIndex].Name == "StatusPesanan" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                if (status.Equals("Selesai", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(21, 128, 61);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle!.ForeColor = CaramelHover;
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
            }
        };
        cardKiri.Controls.Add(_dgvAntrean);
        cardKiri.Controls.Add(lblKiri);
        split.Panel1.Controls.Add(cardKiri);

        var cardKanan = CreateCardPanel();
        cardKanan.Padding = new Padding(14);
        var lblKanan = new Label
        {
            Text = "Rincian Item Pesanan",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = EspressoDark
        };
        _dgvDetailPesanan = CreateStyledGrid();
        cardKanan.Controls.Add(_dgvDetailPesanan);
        cardKanan.Controls.Add(lblKanan);
        split.Panel2.Controls.Add(cardKanan);

        page.Controls.Add(split);
        page.Controls.Add(spacer);
        page.Controls.Add(topCard);
    }

    private void BuildPageKasir(Panel page)
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 520,
            BackColor = BgCream
        };

        var cardBayar = CreateCardPanel();
        cardBayar.Padding = new Padding(22);

        var lblJudul = new Label
        {
            Text = "Proses Pembayaran Kasir",
            Location = new Point(22, 16),
            Size = new Size(380, 30),
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            ForeColor = EspressoDark
        };
        cardBayar.Controls.Add(lblJudul);

        int y = 58;
        cardBayar.Controls.Add(new Label
        {
            Text = "Pilih Pesanan Aktif",
            Location = new Point(22, y),
            Size = new Size(360, 22),
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        });
        y += 24;

        _cmbPesananKasir = new ComboBox
        {
            Location = new Point(22, y),
            Size = new Size(420, 32),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = BgCream
        };
        _cmbPesananKasir.SelectedIndexChanged += (_, _) => HitungUlangKasir();
        cardBayar.Controls.Add(_cmbPesananKasir);
        y += 44;

        _lblInfoPelanggan = new Label
        {
            Text = "Meja / Pelanggan: -",
            Location = new Point(22, y),
            Size = new Size(420, 24),
            ForeColor = TextMuted
        };
        cardBayar.Controls.Add(_lblInfoPelanggan);
        y += 32;

        var panelTotal = new Panel
        {
            Location = new Point(22, y),
            Size = new Size(420, 52),
            BackColor = RowAlt
        };
        _lblTotalBayar = new Label
        {
            Text = "Total Bayar: Rp 0",
            Location = new Point(14, 12),
            Size = new Size(390, 28),
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            ForeColor = EspressoDark
        };
        panelTotal.Controls.Add(_lblTotalBayar);
        cardBayar.Controls.Add(panelTotal);
        y += 66;

        cardBayar.Controls.Add(new Label
        {
            Text = "Uang Bayar (Rp)",
            Location = new Point(22, y),
            Size = new Size(360, 22),
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        });
        y += 24;

        _numUangBayar = new NumericUpDown
        {
            Location = new Point(22, y),
            Size = new Size(420, 34),
            Maximum = 100000000,
            Increment = 5000,
            ThousandsSeparator = true
        };
        _numUangBayar.ValueChanged += (_, _) => HitungUlangKasir();
        cardBayar.Controls.Add(_numUangBayar);
        y += 48;

        _lblKembalian = new Label
        {
            Text = "Kembalian: Rp 0",
            Location = new Point(22, y),
            Size = new Size(420, 30),
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(21, 128, 61)
        };
        cardBayar.Controls.Add(_lblKembalian);
        y += 48;

        var btnBayar = CreateAccentButton("Proses Bayar", new Point(22, y), new Size(195, 42));
        var btnCetak = CreateSecondaryButton("Cetak Struk", new Point(232, y), new Size(210, 42));

        btnBayar.Click += (_, _) => ProsesBayarKlik();
        btnCetak.Click += (_, _) => CetakStrukKlik();

        cardBayar.Controls.AddRange(new Control[] { btnBayar, btnCetak });
        split.Panel1.Controls.Add(cardBayar);

        var cardStruk = CreateCardPanel();
        cardStruk.Padding = new Padding(18);

        var lblStrukTitle = new Label
        {
            Text = "Pratinjau Struk Pembayaran",
            Dock = DockStyle.Top,
            Height = 32,
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            ForeColor = EspressoDark
        };

        _rtbStruk = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Consolas", 10F),
            BackColor = Color.FromArgb(254, 252, 248),
            BorderStyle = BorderStyle.FixedSingle
        };

        cardStruk.Controls.Add(_rtbStruk);
        cardStruk.Controls.Add(lblStrukTitle);
        split.Panel2.Controls.Add(cardStruk);

        page.Controls.Add(split);
    }

    private static Panel CreateCardPanel() => new()
    {
        Dock = DockStyle.Fill,
        BackColor = CardWhite
    };

    private static TextBox CreateStyledTextBox(int x, int y, int width) => new()
    {
        Location = new Point(x, y),
        Size = new Size(width, 32),
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = BgCream
    };

    private static Button CreateAccentButton(string text, Point loc, Size size)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            BackColor = CaramelAccent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (_, _) => btn.BackColor = CaramelHover;
        btn.MouseLeave += (_, _) => btn.BackColor = CaramelAccent;
        return btn;
    }

    private static Button CreateSecondaryButton(string text, Point loc, Size size)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            BackColor = EspressoDark,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static Button CreateDangerButton(string text, Point loc, Size size)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(185, 28, 28),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static DataGridView CreateStyledGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = CardWhite,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = SoftBorder,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 34 }
        };

        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = EspressoDark,
            ForeColor = Color.White,
            SelectionBackColor = EspressoDark,
            SelectionForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Padding = new Padding(6, 0, 6, 0)
        };
        grid.ColumnHeadersHeight = 38;

        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = CardWhite,
            ForeColor = EspressoDark,
            SelectionBackColor = SelectionSoft,
            SelectionForeColor = EspressoDark,
            Font = new Font("Segoe UI", 9.5F),
            Padding = new Padding(6, 0, 6, 0)
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = RowAlt,
            ForeColor = EspressoDark,
            SelectionBackColor = SelectionSoft,
            SelectionForeColor = EspressoDark,
            Font = new Font("Segoe UI", 9.5F),
            Padding = new Padding(6, 0, 6, 0)
        };

        return grid;
    }

    private void MuatDataAwal()
    {
        _cmbKategori.DataSource = _posService.AmbilKategori();
        _cmbKategori.DisplayMember = nameof(KategoriItem.NamaKategori);
        _cmbKategori.ValueMember = nameof(KategoriItem.IdKategori);

        RefreshSemuaData();
    }

    private void RefreshSemuaData()
    {
        var daftarMenu = _posService.AmbilDaftarMenu();
        _dgvMenu.DataSource = null;
        _dgvMenu.DataSource = daftarMenu;

        var menuDipilihSebelumnya = _cmbMenuPesanan.SelectedItem as MenuItemModel;
        _cmbMenuPesanan.DataSource = null;
        _cmbMenuPesanan.DataSource = daftarMenu;
        _cmbMenuPesanan.DisplayMember = nameof(MenuItemModel.NamaMenu);
        _cmbMenuPesanan.ValueMember = nameof(MenuItemModel.IdMenu);
        if (menuDipilihSebelumnya != null)
        {
            var match = daftarMenu.FirstOrDefault(m => m.IdMenu == menuDipilihSebelumnya.IdMenu);
            if (match != null) _cmbMenuPesanan.SelectedItem = match;
        }

        var antrean = _posService.AmbilAntreanPesanan();
        _dgvAntrean.DataSource = null;
        _dgvAntrean.DataSource = antrean.Select(p => new
        {
            p.IdPesanan,
            p.NomorMeja,
            p.NamaPelanggan,
            Waktu = p.WaktuPesan.ToString("HH:mm"),
            Total = $"Rp {p.TotalHarga:N0}",
            p.StatusPesanan
        }).ToList();

        int jumlahMenunggu = antrean.Count(p => p.StatusPesanan != "Selesai");
        _lblRingkasanAntrean.Text = $"Total {antrean.Count} pesanan ({jumlahMenunggu} menunggu pembayaran). Klik baris untuk melihat detail.";

        var pesananBelumLunas = antrean.Where(p => p.StatusPesanan != "Selesai").ToList();
        _cmbPesananKasir.DataSource = null;
        _cmbPesananKasir.DataSource = pesananBelumLunas;
        _cmbPesananKasir.DisplayMember = nameof(PesananItem.IdPesanan);
        _cmbPesananKasir.ValueMember = nameof(PesananItem.IdPesanan);

        HitungUlangKasir();
        TampilkanDetailPesananTerpilih();
    }

    private void BuatPesananBaruKlik()
    {
        var menuDipilih = _cmbMenuPesanan.SelectedItem as MenuItemModel;
        var res = _posService.BuatPesananBaru(
            _txtIdPesanan.Text,
            _txtNoMeja.Text,
            _txtNamaPelanggan.Text,
            menuDipilih,
            (int)_numQtyPesanan.Value,
            _txtCatatanPesanan.Text);

        MessageBox.Show(res.Pesan, "Informasi", MessageBoxButtons.OK,
            res.Sukses ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        if (res.Sukses)
        {
            _txtIdPesanan.Clear();
            _txtNoMeja.Clear();
            _txtNamaPelanggan.Clear();
            _numQtyPesanan.Value = 1;
            _txtCatatanPesanan.Clear();
            RefreshSemuaData();
        }
    }

    private void PilihBarisMenu()
    {
        if (_dgvMenu.CurrentRow?.DataBoundItem is not MenuItemModel item) return;
        _txtIdMenu.Text = item.IdMenu;
        _txtNamaMenu.Text = item.NamaMenu;
        _numHargaMenu.Value = item.Harga;
        _cmbStatusStok.Text = item.StatusStok;
        _cmbKategori.SelectedValue = item.IdKategori;
    }

    private void BersihkanFormMenu()
    {
        _txtIdMenu.Clear();
        _txtNamaMenu.Clear();
        _numHargaMenu.Value = 0;
        if (_cmbStatusStok.Items.Count > 0) _cmbStatusStok.SelectedIndex = 0;
    }

    private void TampilkanDetailPesananTerpilih()
    {
        if (_dgvAntrean.CurrentRow == null)
        {
            _dgvDetailPesanan.DataSource = null;
            return;
        }
        string? idPesanan = _dgvAntrean.CurrentRow.Cells["IdPesanan"]?.Value?.ToString();
        var pesanan = _posService.AmbilAntreanPesanan().FirstOrDefault(p => p.IdPesanan == idPesanan);
        _dgvDetailPesanan.DataSource = pesanan?.Details.Select(d => new
        {
            d.NamaMenu,
            d.Qty,
            Harga = $"Rp {d.HargaSatuan:N0}",
            Subtotal = $"Rp {d.Subtotal:N0}",
            d.Catatan
        }).ToList();

        if (_dgvDetailPesanan.Columns.Count > 0)
        {
            _dgvDetailPesanan.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            if (_dgvDetailPesanan.Columns["NamaMenu"] != null)
                _dgvDetailPesanan.Columns["NamaMenu"]!.Width = 150;
            if (_dgvDetailPesanan.Columns["Qty"] != null)
                _dgvDetailPesanan.Columns["Qty"]!.Width = 45;
            if (_dgvDetailPesanan.Columns["Harga"] != null)
                _dgvDetailPesanan.Columns["Harga"]!.Width = 85;
            if (_dgvDetailPesanan.Columns["Subtotal"] != null)
                _dgvDetailPesanan.Columns["Subtotal"]!.Width = 90;
            if (_dgvDetailPesanan.Columns["Catatan"] != null)
                _dgvDetailPesanan.Columns["Catatan"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
    }

    private void HitungUlangKasir()
    {
        if (_cmbPesananKasir.SelectedItem is not PesananItem pesanan)
        {
            _lblInfoPelanggan.Text = "Meja / Pelanggan: -";
            _lblTotalBayar.Text = "Total Bayar: Rp 0";
            _lblKembalian.Text = "Kembalian: Rp 0";
            return;
        }

        _lblInfoPelanggan.Text = $"Meja / Pelanggan: {pesanan.NomorMeja} / {pesanan.NamaPelanggan}";
        _lblTotalBayar.Text = $"Total Bayar: Rp {pesanan.TotalHarga:N0}";

        decimal kembali = _posService.HitungKembalian(pesanan.TotalHarga, _numUangBayar.Value);
        _lblKembalian.Text = $"Kembalian: Rp {kembali:N0}";
    }

    private void ProsesBayarKlik()
    {
        if (_cmbPesananKasir.SelectedItem is not PesananItem pesanan)
        {
            MessageBox.Show("Pilih pesanan yang akan dibayar.", "Informasi");
            return;
        }

        var hasil = _posService.BayarPesanan(pesanan.IdPesanan, _numUangBayar.Value);
        MessageBox.Show(hasil.Pesan, "Pembayaran", MessageBoxButtons.OK,
            hasil.Sukses ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        if (hasil.Sukses)
        {
            _strukTerakhir = hasil.StrukText;
            _rtbStruk.Text = _strukTerakhir;
            _numUangBayar.Value = 0;
            RefreshSemuaData();
        }
    }

    private void CetakStrukKlik()
    {
        if (string.IsNullOrWhiteSpace(_rtbStruk.Text))
        {
            MessageBox.Show("Belum ada struk untuk dicetak.", "Informasi");
            return;
        }

        _strukTerakhir = _rtbStruk.Text;
        var doc = new PrintDocument();
        doc.PrintPage += (_, e) =>
        {
            e.Graphics?.DrawString(_strukTerakhir, new Font("Consolas", 10F), Brushes.Black, new PointF(20, 20));
        };

        using var preview = new PrintPreviewDialog { Document = doc, Width = 600, Height = 700 };
        preview.ShowDialog();
    }
}