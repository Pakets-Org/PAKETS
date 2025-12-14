using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PAKETS
{
    public partial class recentlyopened_screen : UserControl
    {
        // Posición fija solicitada en coordenadas del Form principal
        private const int FixedX = 261;
        private const int FixedY = 138;

        // Márgenes y mínimos
        private const int RightPadding = 12;
        private const int BottomPadding = 12;
        private const int MinWidth = 200;
        private const int MinHeight = 120;

        // Layout interno
        private const int ControlMargin = 20;
        private const int ButtonsTopY = 50;
        private const int ButtonsSpacing = 6;

        // Tabla de recientes
        private DataGridView dgvRecent;

        // Colores para filas y hover
        private Color rowDefaultBackColor;
        private Color rowAltBackColor;
        private int hoveredRowIndex = -1;

        private Form parentForm;

        public recentlyopened_screen()
        {
            InitializeComponent();

            InitializeRecentGrid();

            // No forzamos tamaño fijo aquí: lo calcula UpdateSizeToParent según el Form padre.
            this.HandleCreated += (s, e) => AttachToParentForm();
            this.Disposed += (s, e) =>
            {
                DetachFromParentForm();
                RecentProfilesManager.RecentChanged -= OnRecentChanged;
            };
            this.Load += (s, e) =>
            {
                UpdateSizeToParent();
                AdjustChildLayout();
                LoadRecentProfilesToGrid();
            };

            // Recalcular layout interno cuando el control cambie de tamaño
            this.Resize += (s, e) => AdjustChildLayout();

            // Suscribirse a cambios del MRU para refrescar la tabla automáticamente
            RecentProfilesManager.RecentChanged += OnRecentChanged;
        }

        private void OnRecentChanged(object sender, EventArgs e)
        {
            if (this.IsHandleCreated)
            {
                this.BeginInvoke((Action)LoadRecentProfilesToGrid);
            }
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            AttachToParentForm();
            UpdateSizeToParent();
            AdjustChildLayout();
        }

        private void AttachToParentForm()
        {
            try
            {
                DetachFromParentForm();

                parentForm = this.FindForm();
                if (parentForm != null)
                {
                    parentForm.Resize += ParentForm_Resize;
                    parentForm.SizeChanged += ParentForm_Resize;
                }
            }
            catch { /* no bloquear la UI por errores no críticos */ }
        }

        private void DetachFromParentForm()
        {
            if (parentForm != null)
            {
                parentForm.Resize -= ParentForm_Resize;
                parentForm.SizeChanged -= ParentForm_Resize;
                parentForm = null;
            }
        }

        private void ParentForm_Resize(object sender, EventArgs e)
        {
            UpdateSizeToParent();
            AdjustChildLayout();
        }

        // Calcula el tamaño usando las coordenadas del Form principal (comportamiento "responsive")
        private void UpdateSizeToParent()
        {
            try
            {
                var form = this.FindForm();
                var container = this.Parent ?? (Control)form;
                if (form == null || container == null) return;

                // Tamaño disponible relativo al Form
                int availWidthFromForm = Math.Max(0, form.ClientSize.Width - FixedX - RightPadding);
                int availHeightFromForm = Math.Max(0, form.ClientSize.Height - FixedY - BottomPadding);

                // Convertir la posición deseada (FixedX,FixedY) del Form al sistema de coordenadas del contenedor actual
                Point desiredInContainer;
                if (container == form)
                {
                    desiredInContainer = new Point(FixedX, FixedY);
                }
                else
                {
                    Point screenPt = form.PointToScreen(new Point(FixedX, FixedY));
                    desiredInContainer = container.PointToClient(screenPt);
                }

                // Calcular ancho/alto máximos disponibles dentro del contenedor a partir de la ubicación convertida
                int maxWByContainer = Math.Max(0, container.ClientSize.Width - desiredInContainer.X - RightPadding);
                int maxHByContainer = Math.Max(0, container.ClientSize.Height - desiredInContainer.Y - BottomPadding);

                // Tomar la intersección entre lo calculado desde el Form y lo permitido por el contenedor
                int newW = Math.Max(MinWidth, Math.Min(availWidthFromForm, maxWByContainer));
                int newH = Math.Max(MinHeight, Math.Min(availHeightFromForm, maxHByContainer));

                // Asegurar que la ubicación no salga del contenedor
                if (desiredInContainer.X < 0) desiredInContainer.X = 0;
                if (desiredInContainer.Y < 0) desiredInContainer.Y = 0;

                // Si al mantener la X fija el control se sale por la derecha, ajustar X para que quepa (mantener "lo más cerca posible" de la X fija)
                if (desiredInContainer.X + newW + RightPadding > container.ClientSize.Width)
                {
                    desiredInContainer.X = Math.Max(0, container.ClientSize.Width - newW - RightPadding);
                }

                if (desiredInContainer.Y + newH + BottomPadding > container.ClientSize.Height)
                {
                    desiredInContainer.Y = Math.Max(0, container.ClientSize.Height - newH - BottomPadding);
                }

                // Aplicar ubicación y nuevo tamaño calculado dinámicamente
                this.Location = desiredInContainer;
                this.Size = new Size(newW, newH);
            }
            catch
            {
                // no bloquear la UI por errores no críticos
            }
        }

        // Ajusta layout interno de los controles para que sean responsive dentro del UserControl
        private void AdjustChildLayout()
        {
            try
            {
                if (label2 == null || panel3 == null || linkLabel1 == null || button1 == null || button2 == null || button4 == null || dgvRecent == null)
                    return;

                int w = this.ClientSize.Width;
                int h = this.ClientSize.Height;

                // label2: mantener margen izquierdo y derecho, altura del diseñador
                int labelLeft = ControlMargin;
                int labelTop = ControlMargin;
                int labelHeight = Math.Max(24, label2.Height);
                int labelWidth = Math.Max(100, w - (ControlMargin * 2));
                label2.Location = new Point(labelLeft, labelTop);
                label2.Size = new Size(labelWidth, labelHeight);

                // panel3: línea horizontal justo por debajo del label
                int panelTop = label2.Bottom + 18;
                panel3.Location = new Point(0, panelTop);
                panel3.Size = new Size(w, Math.Max(1, panel3.Height));

                // Botones: alineados en la esquina superior derecha con separación constante
                int right = w - ControlMargin;
                var buttons = new[] { button4, button2, button1 };
                foreach (var b in buttons)
                {
                    if (b == null) continue;
                    int bx = right - b.Width;
                    int by = ButtonsTopY;
                    b.Location = new Point(Math.Max(ControlMargin, bx), by);
                    right = bx - ButtonsSpacing;
                }

                // dgvRecent: llenar la zona por debajo del panel3 hasta arriba del linkLabel
                int gridTop = panel3.Bottom + 10;
                int gridLeft = ControlMargin;
                int gridRight = w - ControlMargin;
                int gridBottom = Math.Max(gridTop + 50, h - ControlMargin - (linkLabel1.Height + 10));
                dgvRecent.Location = new Point(gridLeft, gridTop);
                dgvRecent.Size = new Size(Math.Max(100, gridRight - gridLeft), Math.Max(60, gridBottom - gridTop));

                // linkLabel: esquina inferior derecha con margen
                bool prevAutoSize = linkLabel1.AutoSize;
                linkLabel1.AutoSize = true;
                linkLabel1.PerformLayout();
                int linkW = linkLabel1.Size.Width;
                int linkH = linkLabel1.Size.Height;
                int linkX = Math.Max(ControlMargin, w - ControlMargin - linkW);
                int linkY = Math.Max(panel3.Bottom + 10, h - ControlMargin - linkH);
                linkLabel1.Location = new Point(linkX, linkY);
                linkLabel1.AutoSize = prevAutoSize;
            }
            catch
            {
                // no bloquear la UI por errores no críticos
            }
        }

        // Inicializa programáticamente la DataGridView para los perfiles recientes
        private void InitializeRecentGrid()
        {
            dgvRecent = new DataGridView
            {
                Name = "dgvRecent",
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,               // <- impedir redimensionar columnas
                AllowUserToOrderColumns = false,                // <- impedir reordenar columnas por arrastre
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Control,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing // <- impedir cambiar alto encabezado
            };

            // colores base para filas (sin azul)
            rowDefaultBackColor = Color.FromArgb(250, 250, 250); // color claro neutro
            rowAltBackColor = Color.FromArgb(240, 240, 240);     // alternado un pelín más oscuro

            dgvRecent.RowsDefaultCellStyle.BackColor = rowDefaultBackColor;
            dgvRecent.AlternatingRowsDefaultCellStyle.BackColor = rowAltBackColor;

            // Evitar el color azul típico de selección: usar transparente
            dgvRecent.DefaultCellStyle.SelectionBackColor = Color.Transparent;
            dgvRecent.DefaultCellStyle.SelectionForeColor = dgvRecent.DefaultCellStyle.ForeColor;
            dgvRecent.RowsDefaultCellStyle.SelectionBackColor = Color.Transparent;
            dgvRecent.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.Transparent;

            // Altura de filas nuevas (más anchas)
            dgvRecent.RowTemplate.Height = 30; // aumentado respecto al valor por defecto

            // Columnas: Perfil (nombre), Fecha (última apertura), Ruta (oculta)
            var colPerfil = new DataGridViewTextBoxColumn { Name = "Perfil", HeaderText = "Perfil", FillWeight = 60, Resizable = DataGridViewTriState.False };
            var colFecha = new DataGridViewTextBoxColumn { Name = "Fecha", HeaderText = "Última apertura", FillWeight = 40, Resizable = DataGridViewTriState.False };
            var colRuta = new DataGridViewTextBoxColumn { Name = "Ruta", HeaderText = "Ruta", Visible = false, Resizable = DataGridViewTriState.False };

            dgvRecent.Columns.AddRange(new DataGridViewColumn[] { colPerfil, colFecha, colRuta });

            // Asegurar que todas las columnas no sean redimensionables (defensa adicional)
            foreach (DataGridViewColumn c in dgvRecent.Columns)
            {
                c.Resizable = DataGridViewTriState.False;
            }

            // Hover: cambiar fondo de la fila a blanco mientras el puntero esté encima
            dgvRecent.CellMouseEnter += DgvRecent_CellMouseEnter;
            dgvRecent.CellMouseLeave += DgvRecent_CellMouseLeave;
            dgvRecent.MouseLeave += DgvRecent_MouseLeave;

            // Evitar que la fila se quede seleccionada al hacer click simple:
            dgvRecent.CellClick += (s, e) => { if (e.RowIndex >= 0) dgvRecent.ClearSelection(); };

            // Abrir sólo en doble click sobre la fila (usar CellDoubleClick para obtener fila concreta)
            dgvRecent.CellDoubleClick += DgvRecent_CellDoubleClick;

            this.Controls.Add(dgvRecent);
            // Traer al frente respecto al panel visual
            dgvRecent.BringToFront();
        }

        private void DgvRecent_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;
                // Si ya hay una fila hover restaurada, restauramos antes de aplicar la nueva
                if (hoveredRowIndex == e.RowIndex) return;
                RestoreHoveredRow();
                hoveredRowIndex = e.RowIndex;
                if (hoveredRowIndex >= 0 && hoveredRowIndex < dgvRecent.Rows.Count)
                {
                    dgvRecent.Rows[hoveredRowIndex].DefaultCellStyle.BackColor = Color.White;
                }
            }
            catch { }
        }

        private void DgvRecent_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // Si el puntero sale de una celda concreta y esa era la fila hovered, restaurar
                if (e.RowIndex >= 0 && hoveredRowIndex == e.RowIndex)
                {
                    RestoreHoveredRow();
                }
            }
            catch { }
        }

        private void DgvRecent_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                RestoreHoveredRow();
            }
            catch { }
        }

        private void RestoreHoveredRow()
        {
            if (hoveredRowIndex >= 0 && hoveredRowIndex < dgvRecent.Rows.Count)
            {
                var row = dgvRecent.Rows[hoveredRowIndex];
                // recuperar color según si es fila par o impar (alternating)
                if (hoveredRowIndex % 2 == 0)
                    row.DefaultCellStyle.BackColor = rowDefaultBackColor;
                else
                    row.DefaultCellStyle.BackColor = rowAltBackColor;
            }
            hoveredRowIndex = -1;
        }

        // Abrir sólo en doble click sobre la fila concreta
        private void DgvRecent_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;
                var row = dgvRecent.Rows[e.RowIndex];
                var ruta = Convert.ToString(row.Cells["Ruta"].Value);
                if (string.IsNullOrEmpty(ruta)) return;
                if (File.Exists(ruta))
                {
                    try
                    {
                        // Abrir el perfil DENTRO de la aplicación en lugar de lanzar asociación del SO
                        RecentProfilesManager.RegisterOpenedProfile(ruta);
                        ProfileSessionManager.OpenProfile(ruta);

                        // refrescar la tabla por si fuera necesario
                        LoadRecentProfilesToGrid();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("No se pudo abrir el perfil:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("El archivo ya no existe:\n" + ruta, "Archivo no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch
            {
                // no bloquear la UI
            }
        }

        // Cargar en la tabla los últimos 5 perfiles ordenados desc por fecha de apertura
        private void LoadRecentProfilesToGrid()
        {
            try
            {
                var items = RecentProfilesManager.LoadRecentProfiles()
                    .OrderByDescending(x => x.OpenedUtc)   // más reciente primero
                    .Take(5)
                    .ToList();

                dgvRecent.Rows.Clear();
                foreach (var it in items)
                {
                    var fileName = Path.GetFileName(it.Path);
                    var fechaLocal = it.OpenedUtc.ToLocalTime().ToString("g");
                    dgvRecent.Rows.Add(fileName, fechaLocal, it.Path);
                }

                // Asegurar que no haya ninguna fila seleccionada por defecto y que visualmente no se vea azul
                dgvRecent.ClearSelection();
            }
            catch
            {
                // no bloquear la UI
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Abrir diálogo y registrar el .pakets seleccionado como reciente, además de abrirlo
            using (var ofd = new OpenFileDialog() { Filter = "Perfil de pakets (*.pakets)|*.pakets" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var path = ofd.FileName;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        RecentProfilesManager.RegisterOpenedProfile(path);

                        // Abrir IN-APP en lugar de Process.Start
                        ProfileSessionManager.OpenProfile(path);

                        LoadRecentProfilesToGrid();
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog() { Filter = "Archivo de pakets (*.pakfile)|*.pakfile" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var path = ofd.FileName;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        // No es .pakets: si quieres también incluir .pakfile en MRU, registra aquí.
                        try { Process.Start(path); } catch { }
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Intento de abrir el diálogo nativo apuntando a "Red".
            IntPtr owner = this.FindForm()?.Handle ?? IntPtr.Zero;
            IFileOpenDialog dialog = null;
            IShellItem networkItem = null;
            try
            {
                var dialogType = Type.GetTypeFromCLSID(new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")); // CLSID_FileOpenDialog
                dialog = (IFileOpenDialog)Activator.CreateInstance(dialogType);

                FILEOPENDIALOGOPTIONS opts;
                dialog.GetOptions(out opts);

                opts &= ~FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS;
                opts |= FILEOPENDIALOGOPTIONS.FOS_NOCHANGEDIR
                      | FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST
                      | FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST
                      | FILEOPENDIALOGOPTIONS.FOS_ALLNONSTORAGEITEMS;

                dialog.SetOptions(opts);

                int hr = SHCreateItemFromParsingName("::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", IntPtr.Zero, typeof(IShellItem).GUID, out networkItem);
                if (hr == 0 && networkItem != null)
                {
                    try { dialog.SetDefaultFolder(networkItem); } catch { }
                    try { dialog.SetFolder(networkItem); } catch { }
                }

                hr = dialog.Show(owner);
                const int S_OK = 0;
                if (hr == S_OK)
                {
                    IShellItem result;
                    dialog.GetResult(out result);
                    if (result != null)
                    {
                        IntPtr pszName;
                        // Si obtenemos FILESYSPATH, es una ruta válida para registrar/abrir
                        if (result.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out pszName) == 0 && pszName != IntPtr.Zero)
                        {
                            string path = Marshal.PtrToStringUni(pszName);
                            Marshal.FreeCoTaskMem(pszName);
                            if (!string.IsNullOrEmpty(path) && File.Exists(path) && path.EndsWith(".pakets", StringComparison.OrdinalIgnoreCase))
                            {
                                RecentProfilesManager.RegisterOpenedProfile(path);

                                // Abrir IN-APP en lugar de Process.Start
                                ProfileSessionManager.OpenProfile(path);

                                LoadRecentProfilesToGrid();
                            }
                        }
                        // Si no hay FILESYSPATH sólo mostramos el nombre; no podemos registrar una ruta inexistente
                        else if (result.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY, out pszName) == 0 && pszName != IntPtr.Zero)
                        {
                            string name = Marshal.PtrToStringUni(pszName);
                            Marshal.FreeCoTaskMem(pszName);
                            // elemento virtual: no registrar
                        }
                        Marshal.FinalReleaseComObject(result);
                    }
                }
            }
            catch
            {
                // Fallback simple: OpenFileDialog apuntando a raíz UNC (\\) y registrar si es .pakets
                try
                {
                    using (var ofd = new OpenFileDialog()
                    {
                        Filter = "Perfil de pakets (*.pakets)|*.pakets",
                        RestoreDirectory = true,
                        InitialDirectory = @"\\"
                    })
                    {
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            var path = ofd.FileName;
                            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                            {
                                RecentProfilesManager.RegisterOpenedProfile(path);

                                // Abrir IN-APP en lugar de Process.Start
                                ProfileSessionManager.OpenProfile(path);

                                LoadRecentProfilesToGrid();
                            }
                        }
                    }
                }
                catch
                {
                    // no bloquear la UI
                }
            }
            finally
            {
                if (networkItem != null) Marshal.FinalReleaseComObject(networkItem);
                if (dialog != null) Marshal.FinalReleaseComObject(dialog);
            }
        }

        #region COM interop helpers

        [ComImport]
        [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            int Show([In] IntPtr parent);
            void SetFileTypes(); // placeholder
            void SetFileTypeIndex(); // placeholder
            void GetFileTypeIndex(); // placeholder
            void Advise(); // placeholder
            void Unadvise(); // placeholder
            void SetOptions([In] FILEOPENDIALOGOPTIONS fos);
            void GetOptions(out FILEOPENDIALOGOPTIONS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(); // placeholder
            void GetCurrentSelection(); // placeholder
            void SetFileName(); // placeholder
            void GetFileName(); // placeholder
            void SetTitle(); // placeholder
            void SetOkButtonLabel(); // placeholder
            void SetFileNameLabel(); // placeholder
            void GetResult(out IShellItem ppsi);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(); // not used
            void GetParent(); // not used
            [PreserveSig]
            int GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
            void GetAttributes(); // not used
            void Compare(); // not used
        }

        [Flags]
        private enum FILEOPENDIALOGOPTIONS : uint
        {
            FOS_OVERWRITEPROMPT = 0x00000002,
            FOS_STRICTFILETYPES = 0x00000004,
            FOS_NOCHANGEDIR = 0x00000008,
            FOS_PICKFOLDERS = 0x00000020,
            FOS_FORCEFILESYSTEM = 0x00000040,
            FOS_ALLNONSTORAGEITEMS = 0x00000080,
            FOS_NOVALIDATE = 0x00000100,
            FOS_ALLOWMULTISELECT = 0x00000200,
            FOS_PATHMUSTEXIST = 0x00000800,
            FOS_FILEMUSTEXIST = 0x00001000,
            FOS_FORCEPREVIEWPANEON = 0x00080000
        }

        private enum SIGDN : uint
        {
            SIGDN_NORMALDISPLAY = 0x00000000,
            SIGDN_PARENTRELATIVEPARSING = 0x80018001,
            SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
            SIGDN_PARENTRELATIVEEDITING = 0x80031001,
            SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
            SIGDN_FILESYSPATH = 0x80058000,
            SIGDN_URL = 0x80068000,
            SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem ppv);

        #endregion

        private void recentlyopened_screen_Load(object sender, EventArgs e)
        {

        }
    }

    // Pequeño gestor MRU para perfiles .pakets (almacena en %APPDATA%\PAKETS\recent_profiles.txt)
    internal static class RecentProfilesManager
    {
        private static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PAKETS");
        private static readonly string FilePath = Path.Combine(Dir, "recent_profiles.txt");
        // Formato por línea: ticks|ruta
        public static event EventHandler RecentChanged;

        public struct RecentItem
        {
            public DateTime OpenedUtc;
            public string Path;
        }

        public static void RegisterOpenedProfile(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);

                var list = LoadRecentProfiles().ToList();

                // Eliminar duplicados por ruta (ignorando mayúsculas/minúsculas)
                list.RemoveAll(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));

                // Insertar al principio con marca de tiempo actual
                list.Insert(0, new RecentItem { OpenedUtc = DateTime.UtcNow, Path = path });

                // Mantener máximo 20 entradas internas (aunque la vista muestra 5)
                list = list.Take(20).ToList();

                // Guardar
                using (var sw = new StreamWriter(FilePath, false))
                {
                    foreach (var it in list)
                    {
                        sw.WriteLine("{0}|{1}", it.OpenedUtc.Ticks, it.Path);
                    }
                }

                RecentChanged?.Invoke(null, EventArgs.Empty);
            }
            catch
            {
                // no bloquear la UI; fallar silenciosamente
            }
        }

        public static System.Collections.Generic.List<RecentItem> LoadRecentProfiles()
        {
            var result = new System.Collections.Generic.List<RecentItem>();
            try
            {
                if (!File.Exists(FilePath)) return result;

                var lines = File.ReadAllLines(FilePath);
                foreach (var line in lines)
                {
                    var idx = line.IndexOf('|');
                    if (idx <= 0) continue;
                    long ticks;
                    if (!long.TryParse(line.Substring(0, idx), out ticks)) continue;
                    var path = line.Substring(idx + 1).Trim();
                    if (string.IsNullOrEmpty(path)) continue;
                    // Sólo perfiles .pakets
                    if (!path.EndsWith(".pakets", StringComparison.OrdinalIgnoreCase)) continue;
                    result.Add(new RecentItem { OpenedUtc = new DateTime(ticks, DateTimeKind.Utc), Path = path });
                }

                // Ordenar descendente por tiempo (más recientes primero)
                result = result.OrderByDescending(x => x.OpenedUtc).ToList();
            }
            catch
            {
                // ignore
            }
            return result;
        }
    }
}
