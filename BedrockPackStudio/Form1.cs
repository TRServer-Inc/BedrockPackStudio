using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BedrockPackStudio
{
    public class PixelPictureBox : PictureBox
    {
        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            base.OnPaint(pe);
        }
    }

    public partial class Form1 : Form
    {
        private MenuStrip mainMenuStrip = null!;
        private ToolStripMenuItem menuRecentFolders = null!;
        private TreeView treeViewFiles = null!;
        private RichTextBox txtCodeEditor = null!;
        private RichTextBox txtLogs = null!;
        private Button btnOpenFolder = null!;
        private Button btnSaveFile = null!;
        private Button btnRun = null!;
        private Label lblCurrentFile = null!;

        // tekli eşya/blok arama ve indirme paneli
        private GroupBox grpItemSearch = null!;
        private TextBox txtSearchItem = null!;
        private ComboBox cmbItemCategory = null!;
        private Button btnFetchSingleTexture = null!;

        // resim editörü kontrolleri
        private Panel panelImageEditor = null!;
        private PixelPictureBox picCanvas = null!;
        private Button btnPickColor = null!;
        private Panel panelCurrentColor = null!;
        private Button btnPencil = null!;
        private Button btnEraser = null!;
        private Button btnUndoImage = null!;
        private Button btnRedoImage = null!;
        private Button btnResizeImage = null!;
        private Button btnSaveImage = null!;

        private Bitmap? currentBitmap = null;
        private Color selectedColor = Color.Red;
        private bool isEraserMode = false;
        private bool isDrawing = false;

        private readonly Stack<Bitmap> imageUndoStack = new Stack<Bitmap>();
        private readonly Stack<Bitmap> imageRedoStack = new Stack<Bitmap>();

        private string currentFolderPath = string.Empty;
        private string activeFilePath = string.Empty;
        private Process? minecraftProcess = null;

        private readonly List<string> recentFolders = new List<string>();
        private readonly string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BedrockPackStudio");
        private string configFilePath => Path.Combine(appDataFolder, "recent.txt");

        private bool isDarkMode = true;

        public Form1()
        {
            SetupUI();
            ApplyTheme();
            LoadRecentFoldersConfig();
        }

        private void SetupUI()
        {
            this.Text = "Bedrock Pack Studio & Compiler";
            this.Size = new Size(1180, 820);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            // 1. üst menü
            mainMenuStrip = new MenuStrip();

            var menuFile = new ToolStripMenuItem("Dosya");
            menuFile.DropDownItems.Add("Klasör Aç", null, BtnOpenFolder_Click);

            menuRecentFolders = new ToolStripMenuItem("Son Açılan Klasörler");
            menuFile.DropDownItems.Add(menuRecentFolders);

            menuFile.DropDownItems.Add("Kaydet", null, BtnSaveFile_Click);
            menuFile.DropDownItems.Add(new ToolStripSeparator());
            menuFile.DropDownItems.Add("Çıkış", null, (s, e) => this.Close());

            var menuEdit = new ToolStripMenuItem("Düzen");
            menuEdit.DropDownItems.Add("Geri Al (Ctrl+Z)", null, (s, e) => PerformUndo());
            menuEdit.DropDownItems.Add("İleri Al (Ctrl+Y)", null, (s, e) => PerformRedo());
            menuEdit.DropDownItems.Add(new ToolStripSeparator());
            menuEdit.DropDownItems.Add("Yeni Dosya Oluştur", null, MenuNewFile_Click);
            menuEdit.DropDownItems.Add("Yeni Klasör Oluştur", null, MenuNewFolder_Click);
            menuEdit.DropDownItems.Add(new ToolStripSeparator());
            menuEdit.DropDownItems.Add("Seçili Öğeyi Sil", null, MenuDelete_Click);

            var menuSettings = new ToolStripMenuItem("Ayarlar");
            menuSettings.DropDownItems.Add("Tema & Özelleştirme", null, MenuSettings_Click);

            mainMenuStrip.Items.Add(menuFile);
            mainMenuStrip.Items.Add(menuEdit);
            mainMenuStrip.Items.Add(menuSettings);
            this.MainMenuStrip = mainMenuStrip;

            // 2. butonlar
            btnOpenFolder = new Button() { Text = "Klasör Aç", Left = 10, Top = 35, Width = 100, Height = 32 };
            btnOpenFolder.Click += BtnOpenFolder_Click;

            btnRun = new Button() { Text = "▶ Çalıştır (.mcpack)", Left = 120, Top = 35, Width = 150, Height = 32, BackColor = Color.LightGreen, ForeColor = Color.Black };
            btnRun.Click += BtnRun_Click;

            btnSaveFile = new Button() { Text = "Kaydet", Left = 280, Top = 35, Width = 80, Height = 32 };
            btnSaveFile.Click += BtnSaveFile_Click;

            lblCurrentFile = new Label() { Text = "Açık Dosya: Yok", Left = 380, Top = 42, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            // sol panel: Proje Dosyaları
            treeViewFiles = new TreeView()
            {
                Left = 10,
                Top = 75,
                Width = 260,
                Height = 380,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            treeViewFiles.NodeMouseDoubleClick += TreeViewFiles_NodeMouseDoubleClick;
            treeViewFiles.NodeMouseClick += TreeViewFiles_NodeMouseClick;

            // sol alt panel: Tekli Eşya/Dokusu Arama ve İndirme Paneli
            grpItemSearch = new GroupBox()
            {
                Text = "🔍 Mojang'dan Doku Çek",
                Left = 10,
                Top = 465,
                Width = 260,
                Height = 190,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            Label lblCategory = new Label() { Text = "Kategori:", Left = 10, Top = 25, AutoSize = true };
            cmbItemCategory = new ComboBox() { Left = 10, Top = 45, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbItemCategory.Items.Add("blocks (Bloklar)");
            cmbItemCategory.Items.Add("items (Eşyalar)");
            cmbItemCategory.SelectedIndex = 0;

            Label lblSearch = new Label() { Text = "Doku Adı (Örn: diamond_sword):", Left = 10, Top = 75, AutoSize = true };
            txtSearchItem = new TextBox() { Left = 10, Top = 95, Width = 240 };

            btnFetchSingleTexture = new Button() { Text = "📥 Dokuyu İndir ve Aç", Left = 10, Top = 240, Width = 240, Height = 35, BackColor = Color.LightSkyBlue, ForeColor = Color.Black };
            btnFetchSingleTexture.Click += async (s, e) => await FetchSingleTextureAsync();

            grpItemSearch.Controls.Add(lblCategory);
            grpItemSearch.Controls.Add(cmbItemCategory);
            grpItemSearch.Controls.Add(lblSearch);
            grpItemSearch.Controls.Add(txtSearchItem);
            grpItemSearch.Controls.Add(btnFetchSingleTexture);

            // metin / kod editörü
            txtCodeEditor = new RichTextBox()
            {
                Left = 280,
                Top = 75,
                Width = 870,
                Height = 580,
                Font = new Font("Consolas", 11),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // resim editörü paneli
            panelImageEditor = new Panel()
            {
                Left = 280,
                Top = 75,
                Width = 870,
                Height = 580,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            btnPencil = new Button() { Text = "✏ Kalem", Left = 10, Top = 10, Width = 80, Height = 30, BackColor = Color.LightSkyBlue };
            btnPencil.Click += (s, e) => SetEraserMode(false);

            btnEraser = new Button() { Text = "🧹 Silgi", Left = 95, Top = 10, Width = 80, Height = 30 };
            btnEraser.Click += (s, e) => SetEraserMode(true);

            btnPickColor = new Button() { Text = "Renk", Left = 180, Top = 10, Width = 70, Height = 30 };
            btnPickColor.Click += BtnPickColor_Click;

            panelCurrentColor = new Panel() { Left = 255, Top = 10, Width = 30, Height = 30, BackColor = selectedColor, BorderStyle = BorderStyle.FixedSingle };

            btnUndoImage = new Button() { Text = "↶ Geri", Left = 295, Top = 10, Width = 65, Height = 30 };
            btnUndoImage.Click += (s, e) => UndoImageState();

            btnRedoImage = new Button() { Text = "↷ İleri", Left = 365, Top = 10, Width = 65, Height = 30 };
            btnRedoImage.Click += (s, e) => RedoImageState();

            btnResizeImage = new Button() { Text = "Boyutlandır", Left = 440, Top = 10, Width = 100, Height = 30 };
            btnResizeImage.Click += BtnResizeImage_Click;

            btnSaveImage = new Button() { Text = "Resmi Kaydet", Left = 550, Top = 10, Width = 110, Height = 30 };
            btnSaveImage.Click += BtnSaveImage_Click;

            picCanvas = new PixelPictureBox()
            {
                Left = 10,
                Top = 50,
                Width = 850,
                Height = 520,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            picCanvas.MouseDown += PicCanvas_MouseDown;
            picCanvas.MouseMove += PicCanvas_MouseMove;
            picCanvas.MouseUp += (s, e) => isDrawing = false;

            panelImageEditor.Controls.Add(btnPencil);
            panelImageEditor.Controls.Add(btnEraser);
            panelImageEditor.Controls.Add(btnPickColor);
            panelImageEditor.Controls.Add(panelCurrentColor);
            panelImageEditor.Controls.Add(btnUndoImage);
            panelImageEditor.Controls.Add(btnRedoImage);
            panelImageEditor.Controls.Add(btnResizeImage);
            panelImageEditor.Controls.Add(btnSaveImage);
            panelImageEditor.Controls.Add(picCanvas);

            // terminal / log ekranı
            txtLogs = new RichTextBox()
            {
                Left = 10,
                Top = 665,
                Width = 1140,
                Height = 100,
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            this.Controls.Add(mainMenuStrip);
            this.Controls.Add(treeViewFiles);
            this.Controls.Add(grpItemSearch);
            this.Controls.Add(btnOpenFolder);
            this.Controls.Add(btnRun);
            this.Controls.Add(btnSaveFile);
            this.Controls.Add(lblCurrentFile);
            this.Controls.Add(txtCodeEditor);
            this.Controls.Add(panelImageEditor);
            this.Controls.Add(txtLogs);

            this.FormClosing += Form1_FormClosing;
        }

        private void BtnRun_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFolderPath) || !Directory.Exists(currentFolderPath))
            {
                Log("Geçerli bir paket klasörü açın!", true);
                return;
            }

            if (!string.IsNullOrEmpty(activeFilePath))
            {
                BtnSaveFile_Click(sender, e);
            }

            if (!ValidatePackStructure(currentFolderPath))
            {
                Log("Derleme durduruldu.", true);
                return;
            }

            try
            {
                Log("Minecraft dizinleri taranıyor...");

                string projectUUID = GetPackUUIDFromManifest(currentFolderPath);
                string folderName = new DirectoryInfo(currentFolderPath).Name;

                // 1. Olası tüm Minecraft Bedrock ve Preview AppData/LocalState yollarını tarıyoruz
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                List<string> possibleMcPaths = new List<string>
                {
                    Path.Combine(localAppData, @"Packages\Microsoft.MinecraftUWP_8wekyb3d8bbwe\LocalState\games\com.mojang"),
                    Path.Combine(localAppData, @"Packages\Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe\LocalState\games\com.mojang")
                };

                bool foundAnyMcDir = false;

                foreach (string mcPackagesPath in possibleMcPaths)
                {
                    if (Directory.Exists(mcPackagesPath))
                    {
                        foundAnyMcDir = true;
                        Log($"Minecraft konumu bulundu: {mcPackagesPath}");

                        string[] targetFolders = new string[]
                        {
                            Path.Combine(mcPackagesPath, "development_resource_packs"),
                            Path.Combine(mcPackagesPath, "resource_packs")
                        };

                        foreach (string targetDir in targetFolders)
                        {
                            if (Directory.Exists(targetDir))
                            {
                                string sameNameFolder = Path.Combine(targetDir, folderName);
                                if (Directory.Exists(sameNameFolder))
                                {
                                    TryDeleteDirectory(sameNameFolder);
                                    Log($"Eski paket klasörü silindi: {sameNameFolder}");
                                }

                                foreach (string subFolder in Directory.GetDirectories(targetDir))
                                {
                                    string manifestPath = Path.Combine(subFolder, "manifest.json");
                                    if (File.Exists(manifestPath))
                                    {
                                        string existingUUID = GetPackUUIDFromManifest(subFolder);
                                        if (!string.IsNullOrEmpty(projectUUID) && !string.IsNullOrEmpty(existingUUID) && projectUUID.Equals(existingUUID, StringComparison.OrdinalIgnoreCase))
                                        {
                                            TryDeleteDirectory(subFolder);
                                            Log($"UUID eşleşen eski paket silindi: {Path.GetFileName(subFolder)}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!foundAnyMcDir)
                {
                    Log("Uyarı: Minecraft varsayılan AppData konumunda bulunamadı (Farklı dizine kurulu olabilir).", true);
                }

                // 2. Manifest versiyonunu güncelle ve ismin sonuna (vX.X.X) ekle
                string updatedVersionStr = UpdateManifestVersionAndName(currentFolderPath);

                // 3. Dosya adının sonuna da sürüm bilgisini parantez içinde ekliyoruz
                DirectoryInfo? parentDirInfo = Directory.GetParent(currentFolderPath);
                if (parentDirInfo == null)
                {
                    Log("Üst dizin okunamadı!", true);
                    return;
                }

                string mcpackPath = Path.Combine(parentDirInfo.FullName, $"{folderName} ({updatedVersionStr}).mcpack");

                if (File.Exists(mcpackPath))
                {
                    File.Delete(mcpackPath);
                }

                Log("Yeni .mcpack paketi derleniyor...");
                ZipFile.CreateFromDirectory(currentFolderPath, mcpackPath);
                Log($".mcpack paketi başarıyla hazırlandı: {Path.GetFileName(mcpackPath)}");

                // 4. Minecraft'ı çalıştır
                Log("Minecraft başlatılıyor ve paket entegre ediliyor...");
                minecraftProcess = Process.Start(new ProcessStartInfo(mcpackPath) { UseShellExecute = true });

                Log($"BAŞARI: Sürüm ({updatedVersionStr}) ile yeni paket oyuna aktarıldı!");
            }
            catch (Exception ex)
            {
                Log($"Kritik çalıştırma hatası: {ex.Message}", true);
            }
        }

        private string GetPackUUIDFromManifest(string packFolderPath)
        {
            try
            {
                string manifestPath = Path.Combine(packFolderPath, "manifest.json");
                if (File.Exists(manifestPath))
                {
                    string content = File.ReadAllText(manifestPath);
                    Match match = Regex.Match(content, @"\""uuid\""\s*:\s*\""([^\""]+)\""");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        private void TryDeleteDirectory(string dirPath)
        {
            try
            {
                Directory.Delete(dirPath, true);
            }
            catch
            {
                foreach (string file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(dirPath, true);
            }
        }

        // manifest.json içindeki versiyonu yükselten ve "name" alanının sonuna (v1.0.X) ekleyen metot
        private string UpdateManifestVersionAndName(string folderPath)
        {
            string versionStr = "v1.0.0";
            try
            {
                string manifestPath = Path.Combine(folderPath, "manifest.json");
                if (File.Exists(manifestPath))
                {
                    string content = File.ReadAllText(manifestPath);
                    Regex versionRegex = new Regex(@"\""version\""\s*:\s*\[\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\]");
                    Match match = versionRegex.Match(content);

                    if (match.Success)
                    {
                        int major = int.Parse(match.Groups[1].Value);
                        int minor = int.Parse(match.Groups[2].Value);
                        int patch = int.Parse(match.Groups[3].Value) + 1;

                        versionStr = $"v{major}.{minor}.{patch}";

                        // 1. Versiyonu güncelle
                        string newVersion = $"\"version\": [{major}, {minor}, {patch}]";
                        content = versionRegex.Replace(content, newVersion, 1);

                        // 2. Header içindeki 'name' değerinin sonuna sürümü parantez içinde ekle
                        Regex nameRegex = new Regex(@"(""header""\s*:\s*\{[\s\S]*?""name""\s*:\s*"")([^""]+)("")");
                        Match nameMatch = nameRegex.Match(content);

                        if (nameMatch.Success)
                        {
                            string prefix = nameMatch.Groups[1].Value;
                            string rawName = nameMatch.Groups[2].Value;
                            string suffix = nameMatch.Groups[3].Value;

                            // Eğer zaten parantez içinde v... varsa temizle
                            rawName = Regex.Replace(rawName, @"\s*\([vV]\d+\.\d+\.\d+\)", "").Trim();
                            string newName = $"{rawName} ({versionStr})";

                            content = nameRegex.Replace(content, prefix + newName + suffix, 1);
                        }

                        File.WriteAllText(manifestPath, content);
                        Log($"manifest.json güncellendi: İsim ve Sürüm -> {versionStr}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Manifest sürüm güncelleme uyarısı: {ex.Message}");
            }
            return versionStr;
        }

        private async Task FetchSingleTextureAsync()
        {
            if (string.IsNullOrEmpty(currentFolderPath) || !Directory.Exists(currentFolderPath))
            {
                Log("Lütfen önce bir proje klasörü açın!", true);
                return;
            }

            string textureName = txtSearchItem.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(textureName))
            {
                Log("Lütfen bir doku adı yazın (Örn: wool_colored_red, diamond_sword)", true);
                return;
            }

            if (!textureName.EndsWith(".png"))
            {
                textureName += ".png";
            }

            string categoryFolder = cmbItemCategory.SelectedIndex == 0 ? "blocks" : "items";
            string targetFolder = Path.Combine(currentFolderPath, "textures", categoryFolder);

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            string targetFilePath = Path.Combine(targetFolder, textureName);
            string fireUrl = $"https://raw.githubusercontent.com/Mojang/bedrock-samples/main/resource_pack/textures/{categoryFolder}/{textureName}";

            try
            {
                btnFetchSingleTexture.Enabled = false;
                Log($"Doku Mojang sunucularından indiriliyor: {textureName}...");

                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(fireUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(targetFilePath, imageBytes);

                        Log($"BAŞARI: '{textureName}' dokusu indirildi ve projenize eklendi!");
                        RefreshDirectory();

                        activeFilePath = targetFilePath;
                        lblCurrentFile.Text = $"Resim Editörü: {textureName}";
                        txtCodeEditor.Visible = false;
                        panelImageEditor.Visible = true;

                        using (var tempImage = Image.FromFile(targetFilePath))
                        {
                            currentBitmap = new Bitmap(tempImage);
                        }
                        picCanvas.Image = currentBitmap;
                        imageUndoStack.Clear();
                        imageRedoStack.Clear();
                    }
                    else
                    {
                        Log($"HATA: '{textureName}' ismiyle bir doku bulunamadı! İsmi doğru yazdığınızdan emin olun.", true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"İndirme hatası: {ex.Message}", true);
            }
            finally
            {
                btnFetchSingleTexture.Enabled = true;
            }
        }

        private void LoadRecentFoldersConfig()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string[] lines = File.ReadAllLines(configFilePath);
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && Directory.Exists(line.Trim()))
                        {
                            recentFolders.Add(line.Trim());
                        }
                    }
                }

                UpdateRecentFoldersMenu();

                if (recentFolders.Count > 0 && Directory.Exists(recentFolders[0]))
                {
                    OpenProjectFolder(recentFolders[0]);
                }
            }
            catch (Exception ex)
            {
                Log($"Geçmiş klasörler yüklenemedi: {ex.Message}", true);
            }
        }

        private void SaveRecentFoldersConfig()
        {
            try
            {
                if (!Directory.Exists(appDataFolder))
                {
                    Directory.CreateDirectory(appDataFolder);
                }

                File.WriteAllLines(configFilePath, recentFolders);
            }
            catch (Exception ex)
            {
                Log($"Geçmiş kaydedilemedi: {ex.Message}", true);
            }
        }

        private void AddToRecentFolders(string path)
        {
            if (recentFolders.Contains(path))
            {
                recentFolders.Remove(path);
            }

            recentFolders.Insert(0, path);

            if (recentFolders.Count > 5)
            {
                recentFolders.RemoveAt(recentFolders.Count - 1);
            }

            SaveRecentFoldersConfig();
            UpdateRecentFoldersMenu();
        }

        private void UpdateRecentFoldersMenu()
        {
            menuRecentFolders.DropDownItems.Clear();

            if (recentFolders.Count == 0)
            {
                var itemEmpty = new ToolStripMenuItem("Henüz Yok") { Enabled = false };
                menuRecentFolders.DropDownItems.Add(itemEmpty);
                return;
            }

            foreach (string folder in recentFolders)
            {
                var item = new ToolStripMenuItem(folder);
                item.Click += (s, e) => OpenProjectFolder(folder);
                menuRecentFolders.DropDownItems.Add(item);
            }
        }

        private void OpenProjectFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                currentFolderPath = folderPath;
                AddToRecentFolders(folderPath);
                Log($"Klasör yüklendi: {currentFolderPath}");
                RefreshDirectory();
            }
            else
            {
                Log($"Klasör bulunamadı: {folderPath}", true);
            }
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                PerformUndo();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                PerformRedo();
                e.SuppressKeyPress = true;
            }
        }

        private void PerformUndo()
        {
            if (panelImageEditor.Visible)
            {
                UndoImageState();
            }
            else if (txtCodeEditor.Visible && txtCodeEditor.CanUndo)
            {
                txtCodeEditor.Undo();
            }
        }

        private void PerformRedo()
        {
            if (panelImageEditor.Visible)
            {
                RedoImageState();
            }
            else if (txtCodeEditor.Visible && txtCodeEditor.CanRedo)
            {
                txtCodeEditor.Redo();
            }
        }

        private void SetEraserMode(bool eraser)
        {
            isEraserMode = eraser;
            if (isEraserMode)
            {
                btnEraser.BackColor = Color.LightSkyBlue;
                btnPencil.BackColor = SystemColors.Control;
            }
            else
            {
                btnPencil.BackColor = Color.LightSkyBlue;
                btnEraser.BackColor = SystemColors.Control;
            }
        }

        private void SaveImageStateForUndo()
        {
            if (currentBitmap != null)
            {
                imageUndoStack.Push(new Bitmap(currentBitmap));
                imageRedoStack.Clear();
            }
        }

        private void UndoImageState()
        {
            if (imageUndoStack.Count > 0 && currentBitmap != null)
            {
                imageRedoStack.Push(new Bitmap(currentBitmap));
                currentBitmap = imageUndoStack.Pop();
                picCanvas.Image = currentBitmap;
                picCanvas.Invalidate();
                Log("Resim hamlesi geri alındı.");
            }
        }

        private void RedoImageState()
        {
            if (imageRedoStack.Count > 0 && currentBitmap != null)
            {
                imageUndoStack.Push(new Bitmap(currentBitmap));
                currentBitmap = imageRedoStack.Pop();
                picCanvas.Image = currentBitmap;
                picCanvas.Invalidate();
                Log("Resim hamlesi ileri alındı.");
            }
        }

        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                this.BackColor = Color.FromArgb(30, 30, 30);
                this.ForeColor = Color.White;

                mainMenuStrip.BackColor = Color.FromArgb(45, 45, 48);
                mainMenuStrip.ForeColor = Color.White;

                treeViewFiles.BackColor = Color.FromArgb(37, 37, 38);
                treeViewFiles.ForeColor = Color.Gainsboro;

                grpItemSearch.ForeColor = Color.White;
                txtSearchItem.BackColor = Color.FromArgb(45, 45, 48);
                txtSearchItem.ForeColor = Color.White;

                txtCodeEditor.BackColor = Color.FromArgb(30, 30, 30);
                txtCodeEditor.ForeColor = Color.LightGray;

                panelImageEditor.BackColor = Color.FromArgb(37, 37, 38);

                txtLogs.BackColor = Color.Black;
                txtLogs.ForeColor = Color.Lime;

                btnOpenFolder.BackColor = Color.FromArgb(60, 60, 60);
                btnOpenFolder.ForeColor = Color.White;
                btnSaveFile.BackColor = Color.FromArgb(60, 60, 60);
                btnSaveFile.ForeColor = Color.White;
            }
            else
            {
                this.BackColor = Color.FromArgb(240, 240, 240);
                this.ForeColor = Color.Black;

                mainMenuStrip.BackColor = Color.FromArgb(225, 225, 225);
                mainMenuStrip.ForeColor = Color.Black;

                treeViewFiles.BackColor = Color.White;
                treeViewFiles.ForeColor = Color.Black;

                grpItemSearch.ForeColor = Color.Black;
                txtSearchItem.BackColor = Color.White;
                txtSearchItem.ForeColor = Color.Black;

                txtCodeEditor.BackColor = Color.White;
                txtCodeEditor.ForeColor = Color.Black;

                panelImageEditor.BackColor = Color.LightGray;

                txtLogs.BackColor = Color.FromArgb(20, 20, 20);
                txtLogs.ForeColor = Color.Lime;

                btnOpenFolder.BackColor = Color.Gainsboro;
                btnOpenFolder.ForeColor = Color.Black;
                btnSaveFile.BackColor = Color.Gainsboro;
                btnSaveFile.ForeColor = Color.Black;
            }
        }

        private void Log(string message, bool isError = false)
        {
            txtLogs.SelectionStart = txtLogs.TextLength;
            txtLogs.SelectionLength = 0;
            txtLogs.SelectionColor = isError ? Color.Red : Color.Lime;
            txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            txtLogs.ScrollToCaret();
        }

        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    OpenProjectFolder(fbd.SelectedPath);
                }
            }
        }

        private void RefreshDirectory()
        {
            if (string.IsNullOrEmpty(currentFolderPath) || !Directory.Exists(currentFolderPath)) return;

            treeViewFiles.Nodes.Clear();
            DirectoryInfo di = new DirectoryInfo(currentFolderPath);
            TreeNode rootNode = treeViewFiles.Nodes.Add(di.Name);
            rootNode.Tag = di.FullName;
            GetDirectories(di.GetDirectories(), rootNode);
            GetFiles(di, rootNode);
            rootNode.Expand();
        }

        private void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo)
        {
            foreach (DirectoryInfo subDir in subDirs)
            {
                TreeNode aNode = nodeToAddTo.Nodes.Add(subDir.Name);
                aNode.Tag = subDir.FullName;
                GetDirectories(subDir.GetDirectories(), aNode);
                GetFiles(subDir, aNode);
            }
        }

        private void GetFiles(DirectoryInfo di, TreeNode nodeToAddTo)
        {
            foreach (FileInfo file in di.GetFiles())
            {
                TreeNode aNode = nodeToAddTo.Nodes.Add(file.Name);
                aNode.Tag = file.FullName;
            }
        }

        private void TreeViewFiles_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is string filePath && File.Exists(filePath))
            {
                if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                    filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                    filePath.EndsWith(".lang", StringComparison.OrdinalIgnoreCase))
                {
                    panelImageEditor.Visible = false;
                    txtCodeEditor.Visible = true;

                    activeFilePath = filePath;
                    lblCurrentFile.Text = $"Açık Dosya: {Path.GetFileName(activeFilePath)}";
                    txtCodeEditor.Text = File.ReadAllText(activeFilePath);
                    Log($"Dosya açıldı: {Path.GetFileName(activeFilePath)}");
                }
            }
        }

        private void TreeViewFiles_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is string filePath && File.Exists(filePath))
            {
                if (filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    activeFilePath = filePath;
                    lblCurrentFile.Text = $"Resim Editörü: {Path.GetFileName(activeFilePath)}";

                    txtCodeEditor.Visible = false;
                    panelImageEditor.Visible = true;

                    using (var tempImage = Image.FromFile(filePath))
                    {
                        currentBitmap = new Bitmap(tempImage);
                    }
                    picCanvas.Image = currentBitmap;

                    imageUndoStack.Clear();
                    imageRedoStack.Clear();

                    Log($"Resim piksel editöründe açıldı: {Path.GetFileName(activeFilePath)} ({currentBitmap.Width}x{currentBitmap.Height})");
                }
            }
        }

        private void PicCanvas_MouseDown(object? sender, MouseEventArgs e)
        {
            if (currentBitmap == null) return;
            SaveImageStateForUndo();
            isDrawing = true;
            PaintPixel(e.Location);
        }

        private void PicCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isDrawing && currentBitmap != null)
            {
                PaintPixel(e.Location);
            }
        }

        private void PaintPixel(Point mousePosition)
        {
            if (currentBitmap == null || picCanvas.Image == null) return;

            float imageRatio = (float)currentBitmap.Width / currentBitmap.Height;
            float containerRatio = (float)picCanvas.Width / picCanvas.Height;

            int drawWidth = picCanvas.Width;
            int drawHeight = picCanvas.Height;
            int offsetX = 0;
            int offsetY = 0;

            if (imageRatio > containerRatio)
            {
                drawHeight = (int)(picCanvas.Width / imageRatio);
                offsetY = (picCanvas.Height - drawHeight) / 2;
            }
            else
            {
                drawWidth = (int)(picCanvas.Height * imageRatio);
                offsetX = (picCanvas.Width - drawWidth) / 2;
            }

            int pixelX = (int)((mousePosition.X - offsetX) * ((float)currentBitmap.Width / drawWidth));
            int pixelY = (int)((mousePosition.Y - offsetY) * ((float)currentBitmap.Height / drawHeight));

            if (pixelX >= 0 && pixelX < currentBitmap.Width && pixelY >= 0 && pixelY < currentBitmap.Height)
            {
                Color colorToPaint = isEraserMode ? Color.Transparent : selectedColor;
                currentBitmap.SetPixel(pixelX, pixelY, colorToPaint);
                picCanvas.Invalidate();
            }
        }

        private void BtnPickColor_Click(object? sender, EventArgs e)
        {
            using (ColorDialog cd = new ColorDialog())
            {
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    selectedColor = cd.Color;
                    panelCurrentColor.BackColor = selectedColor;
                    SetEraserMode(false);
                }
            }
        }

        private void BtnResizeImage_Click(object? sender, EventArgs e)
        {
            if (currentBitmap == null) return;

            string input = PromptInput($"Mevcut Boyut: {currentBitmap.Width}x{currentBitmap.Height}\nYeni Boyut (Örn: 16x16 veya 64x64):", "Resmi Boyutlandır");
            if (!string.IsNullOrWhiteSpace(input) && input.Contains("x"))
            {
                string[] parts = input.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                {
                    SaveImageStateForUndo();
                    Bitmap resized = new Bitmap(w, h);
                    using (Graphics g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.DrawImage(currentBitmap, 0, 0, w, h);
                    }
                    currentBitmap = resized;
                    picCanvas.Image = currentBitmap;
                    Log($"Resim boyutu güncellendi: {w}x{h}");
                }
            }
        }

        private void BtnSaveImage_Click(object? sender, EventArgs e)
        {
            if (currentBitmap != null && !string.IsNullOrEmpty(activeFilePath))
            {
                currentBitmap.Save(activeFilePath);
                Log($"Resim kaydedildi: {Path.GetFileName(activeFilePath)}");
            }
        }

        private void BtnSaveFile_Click(object? sender, EventArgs e)
        {
            if (panelImageEditor.Visible)
            {
                BtnSaveImage_Click(sender, e);
            }
            else if (!string.IsNullOrEmpty(activeFilePath) && File.Exists(activeFilePath))
            {
                File.WriteAllText(activeFilePath, txtCodeEditor.Text);
                Log($"Dosya kaydedildi: {Path.GetFileName(activeFilePath)}");
            }
            else
            {
                Log("Kaydedilecek açık dosya bulunamadı!", true);
            }
        }

        private void MenuNewFile_Click(object? sender, EventArgs e)
        {
            string targetDir = GetSelectedOrRootFolder();
            if (string.IsNullOrEmpty(targetDir)) return;

            string fileName = PromptInput("Yeni Dosya Adı (Örn: item.json):", "Dosya Oluştur");
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string fullPath = Path.Combine(targetDir, fileName);
                if (!File.Exists(fullPath))
                {
                    File.WriteAllText(fullPath, "{\n}");
                    Log($"Yeni dosya oluşturuldu: {fileName}");
                    RefreshDirectory();
                }
                else
                {
                    Log("Bu isimde bir dosya zaten var!", true);
                }
            }
        }

        private void MenuNewFolder_Click(object? sender, EventArgs e)
        {
            string targetDir = GetSelectedOrRootFolder();
            if (string.IsNullOrEmpty(targetDir)) return;

            string folderName = PromptInput("Yeni Klasör Adı:", "Klasör Oluştur");
            if (!string.IsNullOrWhiteSpace(folderName))
            {
                string fullPath = Path.Combine(targetDir, folderName);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    Log($"Yeni klasör oluşturuldu: {folderName}");
                    RefreshDirectory();
                }
                else
                {
                    Log("Bu isimde bir klasör zaten var!", true);
                }
            }
        }

        private void MenuDelete_Click(object? sender, EventArgs e)
        {
            if (treeViewFiles.SelectedNode?.Tag is string path)
            {
                var confirm = MessageBox.Show($"'{Path.GetFileName(path)}' silinsin mi?", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm == DialogResult.Yes)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        if (activeFilePath == path)
                        {
                            activeFilePath = string.Empty;
                            txtCodeEditor.Clear();
                            panelImageEditor.Visible = false;
                            txtCodeEditor.Visible = true;
                            lblCurrentFile.Text = "Açık Dosya: Yok";
                        }
                        Log($"Dosya silindi: {Path.GetFileName(path)}");
                    }
                    else if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                        Log($"Klasör silindi: {Path.GetFileName(path)}");
                    }
                    RefreshDirectory();
                }
            }
            else
            {
                Log("Lütfen silmek için sol taraftan bir dosya veya klasör seçin!", true);
            }
        }

        private void MenuSettings_Click(object? sender, EventArgs e)
        {
            Form settingsForm = new Form()
            {
                Text = "Ayarlar & Özelleştirme",
                Size = new Size(350, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblTheme = new Label() { Text = "Tema Seçimi:", Left = 20, Top = 30, AutoSize = true };
            RadioButton rbDark = new RadioButton() { Text = "Koyu Mod (Dark Mode)", Left = 20, Top = 60, AutoSize = true, Checked = isDarkMode };
            RadioButton rbLight = new RadioButton() { Text = "Açık Mod (Light Mode)", Left = 20, Top = 90, AutoSize = true, Checked = !isDarkMode };
            Button btnApply = new Button() { Text = "Uygula", Left = 220, Top = 120, Width = 90, Height = 30 };

            btnApply.Click += (s, ev) =>
            {
                isDarkMode = rbDark.Checked;
                ApplyTheme();
                settingsForm.Close();
                Log($"Tema güncellendi: {(isDarkMode ? "Koyu Mod" : "Açık Mod")}");
            };

            settingsForm.Controls.Add(lblTheme);
            settingsForm.Controls.Add(rbDark);
            settingsForm.Controls.Add(rbLight);
            settingsForm.Controls.Add(btnApply);

            settingsForm.ShowDialog(this);
        }

        private string GetSelectedOrRootFolder()
        {
            if (string.IsNullOrEmpty(currentFolderPath))
            {
                Log("Önce bir ana klasör açmalısın!", true);
                return string.Empty;
            }

            if (treeViewFiles.SelectedNode?.Tag is string selectedPath)
            {
                if (Directory.Exists(selectedPath)) return selectedPath;
                if (File.Exists(selectedPath)) return Path.GetDirectoryName(selectedPath) ?? currentFolderPath;
            }

            return currentFolderPath;
        }

        private string PromptInput(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label textLabel = new Label() { Left = 20, Top = 15, Text = text, AutoSize = true };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 340 };
            Button confirmation = new Button() { Text = "Tamam", Left = 260, Top = 90, Width = 100, DialogResult = DialogResult.OK };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
        }

        private bool ValidatePackStructure(string folderPath)
        {
            Log("Klasör yapısı denetleniyor...");

            string manifestPath = Path.Combine(folderPath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Log("HATA: 'manifest.json' bulunamadı!", true);
                return false;
            }

            string manifestContent = File.ReadAllText(manifestPath);
            if (!manifestContent.Contains("uuid") || !manifestContent.Contains("header") || !manifestContent.Contains("modules"))
            {
                Log("HATA: 'manifest.json' içeriği eksik veya hatalı!", true);
                return false;
            }

            Log("Klasör yapısı doğrulandı.");
            return true;
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (minecraftProcess != null && !minecraftProcess.HasExited)
            {
                try
                {
                    minecraftProcess.Kill();
                }
                catch { }
            }
        }
    }
}