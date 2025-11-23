using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PAKETS
{
    public partial class recentlyopened_screen : UserControl
    {
        // Posición fija solicitada en coordenadas del Form principal
        private const int FixedX = 261;
        private const int FixedY = 132;

        // Márgenes y mínimos
        private const int RightPadding = 12;
        private const int BottomPadding = 12;
        private const int MinWidth = 200;
        private const int MinHeight = 120;

        private Form parentForm;

        public recentlyopened_screen()
        {
            InitializeComponent();

            this.HandleCreated += (s, e) => AttachToParentForm();
            this.Disposed += (s, e) => DetachFromParentForm();
            this.Load += (s, e) => UpdateSizeToParent();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            AttachToParentForm();
            UpdateSizeToParent();
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
        }

        // Calcula tamaño usando las coordenadas del Form principal pero ajustándolo
        // al contenedor real del control (por si el parent no es el Form).
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
                    // Form -> pantalla -> contenedor
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

                // Aplicar ubicación fija (convertida) y nuevo tamaño
                this.Location = desiredInContainer;
                this.Size = new Size(newW, newH);
            }
            catch
            {
                // no bloquear la UI por errores no críticos
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog() { Filter = "Perfil de pakets (*.pakets)|*.pakets" }) ofd.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog() { Filter = "Archivo de pakets (*.pakfile)|*.pakfile" }) ofd.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Cambiado: NO usar FOS_PICKFOLDERS. Se muestra diálogo de apertura de fichero
            // pero intentando iniciar en la carpeta especial "Red".
            IntPtr owner = this.FindForm()?.Handle ?? IntPtr.Zero;
            IFileOpenDialog dialog = null;
            IShellItem networkItem = null;
            try
            {
                var dialogType = Type.GetTypeFromCLSID(new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")); // CLSID_FileOpenDialog
                dialog = (IFileOpenDialog)Activator.CreateInstance(dialogType);

                // Obtener opciones actuales
                FILEOPENDIALOGOPTIONS opts;
                dialog.GetOptions(out opts);

                // Asegurarnos de QUE NO esté en modo "seleccionar carpeta" y pedir que el fichero exista.
                opts &= ~FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS; // eliminar si estaba presente
                opts |= FILEOPENDIALOGOPTIONS.FOS_NOCHANGEDIR
                      | FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST
                      | FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST;

                // Permitir también elementos no-storage para que la vista "Red" sea accesible.
                opts |= FILEOPENDIALOGOPTIONS.FOS_ALLNONSTORAGEITEMS;

                dialog.SetOptions(opts);

                // Intentar establecer como carpeta inicial la carpeta especial "Network"
                int hr = SHCreateItemFromParsingName("::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", IntPtr.Zero, typeof(IShellItem).GUID, out networkItem);
                if (hr == 0 && networkItem != null)
                {
                    try { dialog.SetDefaultFolder(networkItem); } catch { }
                    try { dialog.SetFolder(networkItem); } catch { }
                }

                // Opcional: si quieres filtrar a *.pakets en el diálogo COM, puede implementarse
                // SetFileTypes; por simplicidad aquí dejamos el diálogo mostrar todos los ficheros.

                hr = dialog.Show(owner);
                const int S_OK = 0;
                if (hr == S_OK)
                {
                    IShellItem result;
                    dialog.GetResult(out result);
                    if (result != null)
                    {
                        IntPtr pszName;
                        // Preferimos la ruta de fichero si está disponible
                        if (result.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out pszName) == 0 && pszName != IntPtr.Zero)
                        {
                            string path = Marshal.PtrToStringUni(pszName);
                            Marshal.FreeCoTaskMem(pszName);
                            MessageBox.Show("Seleccionado: " + path, "Seleccionado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (result.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY, out pszName) == 0 && pszName != IntPtr.Zero)
                        {
                            string name = Marshal.PtrToStringUni(pszName);
                            Marshal.FreeCoTaskMem(pszName);
                            MessageBox.Show("Seleccionado: " + name, "Seleccionado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        Marshal.FinalReleaseComObject(result);
                    }
                }
            }
            catch
            {
                // Fallback simple: OpenFileDialog apuntando a raíz UNC (\\)
                try
                {
                    using (var ofd = new OpenFileDialog()
                    {
                        Filter = "Perfil de pakets (*.pakets)|*.pakets",
                        RestoreDirectory = true,
                        InitialDirectory = @"\\"
                    })
                    {
                        ofd.ShowDialog();
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

        // IFileOpenDialog (derivado de IFileDialog). Declaramos sólo lo que necesitamos.
        [ComImport]
        [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            // IModalWindow
            [PreserveSig]
            int Show([In] IntPtr parent);

            // IFileDialog (parcial) - sólo los métodos usados
            void SetFileTypes(); // not used (placeholder)
            void SetFileTypeIndex(); // not used (placeholder)
            void GetFileTypeIndex(); // not used (placeholder)
            void Advise(); // not used (placeholder)
            void Unadvise(); // not used (placeholder)
            void SetOptions([In] FILEOPENDIALOGOPTIONS fos);
            void GetOptions(out FILEOPENDIALOGOPTIONS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(); // not used
            void GetCurrentSelection(); // not used
            void SetFileName(); // not used
            void GetFileName(); // not used
            void SetTitle(); // not used
            void SetOkButtonLabel(); // not used
            void SetFileNameLabel(); // not used
            void GetResult(out IShellItem ppsi);
            // rest omitted
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
    }
}
