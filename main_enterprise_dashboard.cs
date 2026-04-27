using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PAKETS
{
    public partial class main_enterprise_dashboard : UserControl
    {
        // DataGridView que contendrá la "tabla" de archivos
        private DataGridView filesDataGridView;
        private string currentPaketsProfilePath;
        private string _lastLoginRecordedPath = null;

        public main_enterprise_dashboard()
        {
            InitializeComponent();

            // Crear la tabla programáticamente y añadirla al control
            CreateFilesTable();

            // Ajustes responsive iniciales
            UpdateResponsiveLayout();

            // Si el control ya se carga en tiempo de diseño, también intentamos establecer el nombre.
            TryLoadCompanyNameFromDocumentsPakets();

            // Suscribir evento del botón "Crear" (button1)
            if (this.button1 != null)
            {
                this.button1.Click += Button1_Click;
            }

            // Reaccionar al cambio de tamaño del control para ajustar layout
            this.Resize += (s, e) =>
            {
                UpdateResponsiveLayout();
            };
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Abrir la ventana document_visor con título "Nuevo documento"
                var documentVisor = new document_visor();
                documentVisor.Text = "Nuevo documento";
                documentVisor.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el visor de documentos:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateFilesTable()
        {
            // Evitar crearla dos veces
            if (filesDataGridView != null)
                return;

            filesDataGridView = new DataGridView
            {
                Name = "filesDataGridView",
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                RowHeadersVisible = false,
                AllowUserToOrderColumns = false,       // no mover columnas
                AllowUserToResizeColumns = false,      // no redimensionar columnas
                AllowUserToResizeRows = false,         // no redimensionar filas (refuerzo)
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersVisible = false
            };

            // Columnas (columna oculta para ruta completa del archivo)
            filesDataGridView.Columns.Add("colName", "Nombre");
            filesDataGridView.Columns.Add("colDate", "Fecha");
            filesDataGridView.Columns.Add("colUser", "Usuario");
            filesDataGridView.Columns.Add("colSize", "Tamaño");
            var colPath = new DataGridViewTextBoxColumn
            {
                Name = "colPath",
                HeaderText = "Ruta",
                Visible = false
            };
            filesDataGridView.Columns.Add(colPath);

            // Evitar que cada columna sea redimensionable/movable y que muestre azul al seleccionarse por foco
            foreach (DataGridViewColumn c in filesDataGridView.Columns)
            {
                c.Resizable = DataGridViewTriState.False;
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.ReadOnly = true;
            }

            // Estética y comportamiento
            filesDataGridView.EnableHeadersVisualStyles = false;
            filesDataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            filesDataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            filesDataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular);
            filesDataGridView.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            filesDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            filesDataGridView.ColumnHeadersHeight = 25;

            // Asegurar que al "seleccionar" cabecera no cambie visual (no azul)
            filesDataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = filesDataGridView.ColumnHeadersDefaultCellStyle.BackColor;
            filesDataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor = filesDataGridView.ColumnHeadersDefaultCellStyle.ForeColor;

            // Texto de celdas más pequeño y color de selección para clicks reales
            filesDataGridView.DefaultCellStyle.Font = new Font("Segoe UI", 7.0F, FontStyle.Regular);
            filesDataGridView.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            filesDataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            filesDataGridView.DefaultCellStyle.SelectionForeColor = Color.White;
            filesDataGridView.GridColor = Color.LightGray;
            // Incremento del 5% en la altura de fila inicial
            filesDataGridView.RowTemplate.Height = (int)Math.Ceiling(21.0);

            // Cargar archivos .paketfile desde la carpeta del perfil actual
            LoadPaketFiles();

            // Posicionar bajo header según diseño existente
            int topY = 72;
            try
            {
                int candidate1 = (this.label4 != null && !this.label4.IsDisposed && this.label4.Visible) ? this.label4.Bottom + 6 : 0;
                int candidate2 = (this.panel3 != null && !this.panel3.IsDisposed && this.panel3.Visible) ? this.panel3.Bottom + 8 : 0;
                int candidate3 = (this.label2 != null && !this.label2.IsDisposed && this.label2.Visible) ? this.label2.Bottom + 10 : 0;
                int candidateBtn = 0;
                if (this.button1 != null && !this.button1.IsDisposed && this.button1.Visible) candidateBtn = this.button1.Bottom + 6;
                if (this.button3 != null && !this.button3.IsDisposed && this.button3.Visible) candidateBtn = Math.Max(candidateBtn, this.button3.Bottom + 6);
                if (this.button4 != null && !this.button4.IsDisposed && this.button4.Visible) candidateBtn = Math.Max(candidateBtn, this.button4.Bottom + 6);

                topY = Math.Max(72, Math.Max(candidateBtn, Math.Max(candidate1, Math.Max(candidate2, candidate3))));
            }
            catch
            {
                topY = 72;
            }

            filesDataGridView.Location = new Point(8, topY);
            filesDataGridView.Size = new Size(Math.Max(100, this.ClientSize.Width - 16), Math.Max(60, this.ClientSize.Height - topY - 8));

            this.Controls.Add(filesDataGridView);

            // Asegurar que encabezado y botones queden al frente
            try
            {
                this.label2?.BringToFront();
                this.label1?.BringToFront();
                this.last_login?.BringToFront();
                this.label4?.BringToFront();
                this.panel3?.BringToFront();
                this.button1?.BringToFront();
                this.button3?.BringToFront();
                this.button4?.BringToFront();
            }
            catch { }

            // Evitar que la tabla muestre selección al recibir foco; mostrará selección solo tras click del usuario
            filesDataGridView.ClearSelection();
            filesDataGridView.CurrentCell = null;

            // Al entrar (por TAB o foco) limpiar selección para no mostrar azul automáticamente
            filesDataGridView.Enter += (s, e) =>
            {
                filesDataGridView.ClearSelection();
                filesDataGridView.CurrentCell = null;
            };

            // Seleccionar únicamente cuando el usuario hace click en una celda
            filesDataGridView.MouseDown += (s, e) =>
            {
                var hit = filesDataGridView.HitTest(e.X, e.Y);
                if (hit.Type == DataGridViewHitTestType.Cell && hit.RowIndex >= 0 && hit.ColumnIndex >= 0)
                {
                    filesDataGridView.CurrentCell = filesDataGridView.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
                    filesDataGridView.Rows[hit.RowIndex].Selected = true;
                }
                else
                {
                    filesDataGridView.ClearSelection();
                    filesDataGridView.CurrentCell = null;
                }
            };

            // Doble click para abrir archivo
            filesDataGridView.CellDoubleClick += FilesDataGridView_CellDoubleClick;

            // Evitar reordenado/redimensionado por el usuario (refuerzo adicional)
            filesDataGridView.ColumnWidthChanged += (s, e) =>
            {
                try
                {
                    filesDataGridView.SuspendLayout();
                    int idx = e.Column != null ? e.Column.Index : -1;
                    if (idx >= 0 && idx < filesDataGridView.Columns.Count)
                    {
                        // noop: evitar que manipulaciones externas alteren el ancho por error
                        filesDataGridView.Columns[idx].Width = filesDataGridView.Columns[idx].Width;
                    }
                }
                finally
                {
                    try { filesDataGridView.ResumeLayout(); } catch { }
                }
            };

            filesDataGridView.ColumnDisplayIndexChanged += (s, e) =>
            {
                try
                {
                    if (e.Column != null)
                        e.Column.DisplayIndex = e.Column.Index; // restaurar índice lógico si se intenta mover
                }
                catch { }
            };

            UpdateTableColumnWidths();
            filesDataGridView.SelectionChanged += FilesDataGridView_SelectionChanged;
        }

        private void LoadPaketFiles()
        {
            try
            {
                filesDataGridView.Rows.Clear();

                // Si hay un perfil actual cargado, buscar en su carpeta
                if (!string.IsNullOrWhiteSpace(currentPaketsProfilePath))
                {
                    var profileDir = Path.GetDirectoryName(currentPaketsProfilePath);
                    if (!string.IsNullOrEmpty(profileDir) && Directory.Exists(profileDir))
                    {
                        LoadPaketFilesFromDirectory(profileDir);
                        return;
                    }
                }

                // Si no hay perfil actual, buscar en la carpeta PAKETS de Documentos
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string paketsRoot = Path.Combine(documents, "PAKETS");

                if (Directory.Exists(paketsRoot))
                {
                    LoadPaketFilesFromDirectory(paketsRoot);
                }
            }
            catch { }
        }

        private void LoadPaketFilesFromDirectory(string directory)
        {
            try
            {
                // Buscar todos los archivos .pak y .paketfile en el directorio y subdirectorios
                var paketFiles = System.Linq.Enumerable.Concat(
                        Directory.GetFiles(directory, "*.pak",      SearchOption.AllDirectories),
                        Directory.GetFiles(directory, "*.paketfile", SearchOption.AllDirectories))
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToArray();

                foreach (var filePath in paketFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        string fileName = fileInfo.Name;
                        string fileDate = fileInfo.LastWriteTime.ToString("dd/MM/yyyy HH:mm");
                        string fileUser = GetFileOwner(filePath);
                        string fileSize = FormatFileSize(fileInfo.Length);

                        filesDataGridView.Rows.Add(fileName, fileDate, fileUser, fileSize, filePath);
                    }
                    catch
                    {
                        // Ignorar archivos que no se puedan leer
                        continue;
                    }
                }
            }
            catch { }
        }

        private string GetFileOwner(string filePath)
        {
            try
            {
                // Intentar obtener el propietario del archivo
                var fileInfo = new FileInfo(filePath);
                var security = fileInfo.GetAccessControl();
                var owner = security.GetOwner(typeof(System.Security.Principal.NTAccount));
                if (owner != null)
                {
                    string ownerName = owner.ToString();
                    // Extraer solo el nombre de usuario (después del \)
                    int slashIndex = ownerName.LastIndexOf('\\');
                    if (slashIndex >= 0 && slashIndex < ownerName.Length - 1)
                    {
                        return ownerName.Substring(slashIndex + 1);
                    }
                    return ownerName;
                }
            }
            catch { }

            return Environment.UserName;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }

        private void FilesDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;
                var row = filesDataGridView.Rows[e.RowIndex];
                var fileName = Convert.ToString(row.Cells["colName"].Value);
                var storedPath = Convert.ToString(row.Cells["colPath"].Value);

                if (string.IsNullOrWhiteSpace(fileName)) return;

                // Buscar y abrir el archivo en document_visor
                string foundPath = FindAndOpenFileInVisor(fileName, storedPath);

                if (string.IsNullOrEmpty(foundPath))
                {
                    MessageBox.Show($"No se puede abrir el documento '{fileName}' porque no se encuentra el archivo.",
                        "Archivo no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al intentar abrir el archivo:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FindAndOpenFileInVisor(string fileName, string storedPath)
        {
            string foundPath = null;

            // 1. Intentar con la ruta almacenada si existe
            if (!string.IsNullOrWhiteSpace(storedPath) && File.Exists(storedPath))
            {
                foundPath = storedPath;
            }
            // 2. Buscar en la carpeta del perfil actual
            else if (!string.IsNullOrWhiteSpace(currentPaketsProfilePath))
            {
                var profileDir = Path.GetDirectoryName(currentPaketsProfilePath);
                if (!string.IsNullOrEmpty(profileDir))
                {
                    var pathInProfile = Path.Combine(profileDir, fileName);
                    if (File.Exists(pathInProfile))
                    {
                        foundPath = pathInProfile;
                    }
                }
            }

            // 3. Buscar en carpeta PAKETS de Documentos
            if (string.IsNullOrEmpty(foundPath))
            {
                try
                {
                    string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string paketsRoot = Path.Combine(documents, "PAKETS");

                    if (Directory.Exists(paketsRoot))
                    {
                        var foundFiles = Directory.GetFiles(paketsRoot, fileName, SearchOption.AllDirectories);
                        if (foundFiles.Length > 0)
                        {
                            foundPath = foundFiles[0];
                        }
                    }
                }
                catch { }
            }

            // Si no se encontró, preguntar si buscar en todo el sistema
            if (string.IsNullOrEmpty(foundPath))
            {
                var result = MessageBox.Show(
                    $"No se encontró el archivo '{fileName}' en la carpeta de PAKETS.\n\n" +
                    "¿Desea buscar el archivo en todo el equipo?\n\n" +
                    "Nota: Esta búsqueda puede tardar varios minutos.",
                    "Archivo no encontrado",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var searchForm = new Form
                    {
                        Text = "Buscando archivo...",
                        Width = 400,
                        Height = 100,
                        StartPosition = FormStartPosition.CenterScreen,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        ControlBox = false
                    };

                    var label = new Label
                    {
                        Text = $"Buscando '{fileName}' en todo el sistema...\nEsto puede tardar varios minutos.",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };

                    searchForm.Controls.Add(label);
                    searchForm.Show();
                    Application.DoEvents();

                    try
                    {
                        foundPath = SearchFileInSystem(fileName);
                        searchForm.Close();
                    }
                    catch
                    {
                        searchForm.Close();
                    }
                }
            }

            // Si se encontró el archivo, abrir en document_visor
            if (!string.IsNullOrEmpty(foundPath))
            {
                var documentVisor = new document_visor(foundPath);
                documentVisor.Show();
                return foundPath;
            }

            return null;
        }

        private string SearchFileInSystem(string fileName)
        {
            try
            {
                // Obtener todas las unidades del sistema
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable))
                    .ToArray();

                foreach (var drive in drives)
                {
                    try
                    {
                        var found = SearchInDirectory(drive.RootDirectory.FullName, fileName);
                        if (!string.IsNullOrEmpty(found))
                            return found;
                    }
                    catch
                    {
                        // Ignorar errores de acceso en unidades/carpetas protegidas
                        continue;
                    }
                }
            }
            catch { }

            return null;
        }

        private string SearchInDirectory(string directory, string fileName)
        {
            try
            {
                // Buscar archivos en el directorio actual
                var files = Directory.GetFiles(directory, fileName, SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                    return files[0];

                // Buscar en subdirectorios
                var subdirs = Directory.GetDirectories(directory);
                foreach (var subdir in subdirs)
                {
                    try
                    {
                        // Evitar carpetas del sistema que suelen estar protegidas
                        string dirName = Path.GetFileName(subdir).ToLowerInvariant();
                        if (dirName == "system volume information" ||
                            dirName == "$recycle.bin" ||
                            dirName == "windows" ||
                            dirName == "program files" ||
                            dirName == "program files (x86)")
                            continue;

                        var found = SearchInDirectory(subdir, fileName);
                        if (!string.IsNullOrEmpty(found))
                            return found;
                    }
                    catch
                    {
                        // Ignorar errores de acceso
                        continue;
                    }
                }
            }
            catch { }

            return null;
        }

        private void OpenFileWithDefaultApp(string filePath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir el archivo con la aplicación predeterminada:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilesDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            // placeholder para lógica cuando cambie la selección
        }

        private void UpdateTableColumnWidths()
        {
            if (filesDataGridView == null || filesDataGridView.IsDisposed) return;

            try
            {
                filesDataGridView.SuspendLayout();

                int total = filesDataGridView.ClientSize.Width;
                int scrollbar = SystemInformation.VerticalScrollBarWidth;
                int usable = Math.Max(120, total - scrollbar);

                int wName = (int)(usable * 0.50f);
                int wDate = (int)(usable * 0.20f);
                int wUser = (int)(usable * 0.20f);
                int wSize = usable - (wName + wDate + wUser);

                wName = Math.Max(120, wName);
                wDate = Math.Max(110, wDate);
                wUser = Math.Max(100, wUser);
                wSize = Math.Max(60, wSize);

                if (filesDataGridView.Columns.Contains("colName")) filesDataGridView.Columns["colName"].Width = wName;
                if (filesDataGridView.Columns.Contains("colDate")) filesDataGridView.Columns["colDate"].Width = wDate;
                if (filesDataGridView.Columns.Contains("colUser")) filesDataGridView.Columns["colUser"].Width = wUser;
                if (filesDataGridView.Columns.Contains("colSize")) filesDataGridView.Columns["colSize"].Width = wSize;
            }
            finally
            {
                try { filesDataGridView.ResumeLayout(); } catch { }
            }
        }

        private void UpdateResponsiveLayout()
        {
            try
            {
                int w = Math.Max(0, this.ClientSize.Width);
                int h = Math.Max(0, this.ClientSize.Height);

                const int smallW = 480;
                const int mediumW = 800;

                // Valores reducidos para fuentes y encabezados
                float labelFontSmall = 7.0f;
                float labelFontMed = 8.5f;
                float labelFontLarge = 10.0f;

                float headerFontSmall = 7.5f;
                float headerFontMed = 9.0f;
                float headerFontLarge = 10.5f;

                float fontLabelSize;
                float fontHeaderSize;
                if (w <= smallW)
                {
                    fontLabelSize = labelFontSmall;
                    fontHeaderSize = headerFontSmall;
                }
                else if (w <= mediumW)
                {
                    fontLabelSize = labelFontMed;
                    fontHeaderSize = headerFontMed;
                }
                else
                {
                    fontLabelSize = labelFontLarge;
                    fontHeaderSize = headerFontLarge;
                }

                TrySetFontSafe(this.label2, fontHeaderSize, FontStyle.Bold);
                TrySetFontSafe(this.label1, fontLabelSize, FontStyle.Regular);
                TrySetFontSafe(this.last_login, fontLabelSize, FontStyle.Regular);
                TrySetFontSafe(this.label4, fontLabelSize, FontStyle.Regular);

                try
                {
                    if (this.panel3 != null && !this.panel3.IsDisposed)
                    {
                        int leftPadding = 8;
                        int rightPadding = 8;
                        int newWidth = Math.Max(64, w - leftPadding - rightPadding);
                        int designerX = this.panel3.Location.X;
                        this.panel3.Location = new Point(designerX, this.panel3.Location.Y);
                        this.panel3.Width = newWidth;
                    }
                }
                catch { }

                if (filesDataGridView != null && !filesDataGridView.IsDisposed)
                {
                    int topY = 72;
                    try
                    {
                        int candidate1 = (this.label4 != null && !this.label4.IsDisposed && this.label4.Visible) ? this.label4.Bottom + 6 : 0;
                        int candidate2 = (this.panel3 != null && !this.panel3.IsDisposed && this.panel3.Visible) ? this.panel3.Bottom + 8 : 0;
                        int candidate3 = (this.label2 != null && !this.label2.IsDisposed && this.label2.Visible) ? this.label2.Bottom + 10 : 0;
                        int candidateBtn = 0;
                        if (this.button1 != null && !this.button1.IsDisposed && this.button1.Visible) candidateBtn = this.button1.Bottom + 6;
                        if (this.button3 != null && !this.button3.IsDisposed && this.button3.Visible) candidateBtn = Math.Max(candidateBtn, this.button3.Bottom + 6);
                        if (this.button4 != null && !this.button4.IsDisposed && this.button4.Visible) candidateBtn = Math.Max(candidateBtn, this.button4.Bottom + 6);

                        topY = Math.Max(72, Math.Max(candidateBtn, Math.Max(candidate1, Math.Max(candidate2, candidate3))));
                    }
                    catch { topY = 72; }

                    int leftPad = 8;
                    int rightPad = 8;

                    filesDataGridView.Location = new Point(leftPad, topY);
                    filesDataGridView.Size = new Size(Math.Max(100, w - leftPad - rightPad), Math.Max(60, h - topY - 8));

                    try
                    {
                        // Aplicar los tamaños reducidos al DataGridView de forma reactiva
                        filesDataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", fontHeaderSize, FontStyle.Regular);
                        filesDataGridView.DefaultCellStyle.Font = new Font("Segoe UI", fontLabelSize, FontStyle.Regular);
                        // Ajustar la altura de filas según fuente más pequeña y aumentar un 5%
                        filesDataGridView.RowTemplate.Height = Math.Max(16, (int)Math.Ceiling(fontLabelSize * 2.2f * 1.05f));
                    }
                    catch { }

                    UpdateTableColumnWidths();
                }

                try
                {
                    this.label2?.BringToFront();
                    this.label1?.BringToFront();
                    this.last_login?.BringToFront();
                    this.label4?.BringToFront();
                    this.panel3?.BringToFront();
                    this.button1?.BringToFront();
                    this.button3?.BringToFront();
                    this.button4?.BringToFront();
                }
                catch { }
            }
            catch
            {
                // no lanzar excepciones de layout
            }
        }

        private static void TrySetFontSafe(Control c, float size, FontStyle style)
        {
            try
            {
                if (c == null || c.IsDisposed) return;
                var family = c.Font != null ? c.Font.FontFamily : SystemFonts.DefaultFont.FontFamily;
                c.Font = new Font(family, size, style);
            }
            catch { }
        }

        private void TryLoadCompanyNameFromDocumentsPakets()
        {
            try
            {
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string paketsRoot = Path.Combine(documents, "PAKETS");

                if (!Directory.Exists(paketsRoot))
                    return;

                var paketsFiles = Directory.EnumerateFiles(paketsRoot, "*.pakets", SearchOption.AllDirectories);
                string selected = paketsFiles.OrderByDescending(p => File.GetLastWriteTimeUtc(p)).FirstOrDefault();

                if (selected != null)
                {
                    UpdateCompanyNameFromPaketsFile(selected);
                    UpdateUserNameFromPaketsFile(selected);
                }
            }
            catch { }
        }

        public void UpdateCompanyNameFromPaketsFile(string paketsFilePath)
        {
            if (string.IsNullOrWhiteSpace(paketsFilePath)) return;

            try
            {
                if (!File.Exists(paketsFilePath)) return;

                // Guardar la ruta del perfil actual para búsquedas
                currentPaketsProfilePath = paketsFilePath;

                // Mostrar y registrar el último login
                UpdateLastLoginLabel(paketsFilePath);

                var parent = Directory.GetParent(paketsFilePath);
                if (parent != null)
                {
                    string razonSocial = parent.Name;
                    if (this.label2 != null && !this.label2.IsDisposed)
                    {
                        if (this.InvokeRequired)
                            this.BeginInvoke(new Action(() => this.label2.Text = razonSocial));
                        else
                            this.label2.Text = razonSocial;

                        UpdateResponsiveLayout();
                    }
                }

                // Recargar archivos .paketfile de la nueva carpeta de perfil
                if (filesDataGridView != null && !filesDataGridView.IsDisposed)
                {
                    LoadPaketFiles();
                }
            }
            catch { }
        }

        public void UpdateUserNameFromPaketsFile(string paketsFilePath)
        {
            if (string.IsNullOrWhiteSpace(paketsFilePath)) return;

            try
            {
                if (!File.Exists(paketsFilePath)) return;

                string content = File.ReadAllText(paketsFilePath, Encoding.UTF8);
                string usuario = ParseUsuarioFromContent(content);
                if (string.IsNullOrWhiteSpace(usuario)) usuario = Environment.UserName;

                if (this.label1 != null && !this.label1.IsDisposed)
                {
                    if (this.InvokeRequired)
                        this.BeginInvoke(new Action(() => this.label1.Text = usuario));
                    else
                        this.label1.Text = usuario;

                    UpdateResponsiveLayout();
                }
            }
            catch { }
        }

        private string ParseUsuarioFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            try
            {
                var jsonPatterns = new[]
                {
                    "\"usuario\"\\s*:\\s*\"([^\"]+)\"",
                    "\"user\"\\s*:\\s*\"([^\"]+)\"",
                    "\"username\"\\s*:\\s*\"([^\"]+)\"",
                    "'usuario'\\s*:\\s*'([^']+)'",
                    "'user'\\s*:\\s*'([^']+)'"
                };

                foreach (var pat in jsonPatterns)
                {
                    var m = Regex.Match(content, pat, RegexOptions.IgnoreCase);
                    if (m.Success && m.Groups.Count > 1)
                        return m.Groups[1].Value.Trim().TrimEnd(',').Trim();
                }

                var linePattern = new Regex(@"(?mi)^\s*(?:usuario|user|username)\s*[:=]\s*(.+)$", RegexOptions.IgnoreCase);
                var lm = linePattern.Match(content);
                if (lm.Success && lm.Groups.Count > 1)
                {
                    var val = lm.Groups[1].Value.Trim();
                    val = val.Trim().Trim('"', '\'', ',').Trim();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }

                var anyPattern = new Regex(@"(?i)(?:usuario|user|username)\s*[:=]\s*([^\s,;""']+)");
                var am = anyPattern.Match(content);
                if (am.Success && am.Groups.Count > 1)
                {
                    var candidate = am.Groups[1].Value.Trim().Trim('"', '\'').Trim();
                    if (!string.IsNullOrWhiteSpace(candidate))
                        return candidate;
                }
            }
            catch { }

            return null;
        }

        private void UpdateLastLoginLabel(string paketsFilePath)
        {
            if (string.IsNullOrWhiteSpace(paketsFilePath)) return;
            if (_lastLoginRecordedPath == paketsFilePath) return;
            _lastLoginRecordedPath = paketsFilePath;

            try
            {
                string lastLoginFile = Path.ChangeExtension(paketsFilePath, ".lastlogin");
                string previousLoginText = null;

                if (File.Exists(lastLoginFile))
                {
                    try
                    {
                        string stored = File.ReadAllText(lastLoginFile, Encoding.UTF8).Trim();
                        if (DateTime.TryParse(stored, out DateTime dt))
                            previousLoginText = dt.ToString("dd/MM/yyyy | HH:mm");
                    }
                    catch { }
                }

                // Guardar el momento actual como nuevo último login
                try
                {
                    File.WriteAllText(lastLoginFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Encoding.UTF8);
                }
                catch { }

                string labelText = previousLoginText != null
                    ? "Ultimo acceso: " + previousLoginText
                    : "Ultimo acceso: Sin registros";

                Action update = () =>
                {
                    if (this.last_login != null && !this.last_login.IsDisposed)
                        this.last_login.Text = labelText;
                };

                if (this.InvokeRequired)
                    this.BeginInvoke(update);
                else
                    update();
            }
            catch { }
        }

        private void label2_Click(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label4_Click(object sender, EventArgs e) { }

        private void main_enterprise_dashboard_Load(object sender, EventArgs e)
        {
            // Aquí puedes agregar cualquier lógica de inicialización necesaria al cargar el control.
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Archivo PAKETS (*.pak)|*.pak|Archivos Paket (*.paketfile)|*.paketfile|Todos los archivos (*.*)|*.*";
                openFileDialog.DefaultExt = "pak";
                openFileDialog.Title = "Abrir documento";

                // Si hay un perfil activo, usar esa carpeta como inicial
                if (!string.IsNullOrEmpty(currentPaketsProfilePath))
                {
                    string profileDir = Path.GetDirectoryName(currentPaketsProfilePath);
                    openFileDialog.InitialDirectory = profileDir;
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            var documentVisor = new document_visor(openFileDialog.FileName);
                            documentVisor.Show();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error al abrir el documento:\n{ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void refresh_Click(object sender, EventArgs e)
        {
            LoadPaketFiles();
        }
    }
}
