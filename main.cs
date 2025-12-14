using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PAKETS
{
    public partial class main : Form
    {
        // Requisitos y parámetros
        private const int MinWidth = 1280;
        private const int MinHeight = 720;
        private const int CalendarBottomMargin = 12;     // margen inferior para calendario
        private const int MaxRecentEntries = 50;

        // Dashboard de empresa mostrado al abrir un perfil
        private main_enterprise_dashboard enterpriseDashboard;

        public main()
        {
            InitializeComponent();

            // Suscribirse a eventos de sesión de perfil
            ProfileSessionManager.ProfileOpened += OnProfileOpened;
            ProfileSessionManager.ProfileClosed += OnProfileClosed;

            // Asegurar comportamiento responsive del calendario manteniéndolo anclado
            if (monthCalendar1 != null)
            {
                monthCalendar1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            }

            // Reaccionar cuando el panel izquierdo cambie de tamaño (recalcula responsive)
            if (panel1 != null)
            {
                panel1.Resize += (s, e) => UpdateCalendarResponsive();
            }

            // Suscribir el menú "Ver la ayuda"
            this.VerlaayudaToolStripMenuItem.Click += VerlaayudaToolStripMenuItem_Click;

            // Adjuntar manejador dinámico para el menú "Visita nuestra web"
            AttachWebsiteMenuHandlers();

            // Eventos para comportamiento al mostrar y redimensionar
            this.Shown += Main_Shown;
            this.Resize += Main_Resize;
        }

        private void Main_Shown(object sender, EventArgs e)
        {
            // Verificar resolución mínima de la pantalla principal
            var primary = Screen.PrimaryScreen.Bounds.Size;
            if (primary.Width < MinWidth || primary.Height < MinHeight)
            {
                MessageBox.Show($"Resolución mínima requerida {MinWidth}x{MinHeight}. Resolución actual {primary.Width}x{primary.Height}. La aplicación no puede ejecutarse.", "Resolución insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            // Ajustar posición/dimensiones del calendario dentro de panel1 (responsive)
            UpdateCalendarResponsive();
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            // Recalcular responsive del calendario
            UpdateCalendarResponsive();
        }

        // Actualiza las dimensiones y la posición del monthCalendar1 según el ancho disponible en panel1.
        // Mantiene el anclaje Bottom|Left y evita que el calendario salga del panel.
        private void UpdateCalendarResponsive()
        {
            if (monthCalendar1 == null || panel1 == null) return;

            int panelW = panel1.ClientSize.Width;
            int panelH = panel1.ClientSize.Height;

            int cols;
            if (panelW < 320) cols = 1;
            else if (panelW < 640) cols = 2;
            else if (panelW < 960) cols = 3;
            else cols = 4;

            int rows = 1;

            var desired = new Size(cols, rows);
            if (monthCalendar1.CalendarDimensions != desired)
            {
                monthCalendar1.CalendarDimensions = desired;
            }

            float baseFontSize = 9.0f;
            float fontSize = baseFontSize;
            if (cols >= 3) fontSize = baseFontSize - 0.5f;
            if (cols >= 4) fontSize = baseFontSize - 1.0f;
            try
            {
                monthCalendar1.Font = new Font(monthCalendar1.Font.FontFamily, fontSize, monthCalendar1.Font.Style);
            }
            catch
            {
                // ignorar si la fuente no se puede ajustar
            }

            int leftMargin = 17;
            int x;
            if (leftMargin + monthCalendar1.PreferredSize.Width <= panelW)
            {
                x = leftMargin;
            }
            else
            {
                x = 0;
                while (cols > 1 && monthCalendar1.PreferredSize.Width > panelW)
                {
                    cols--;
                    monthCalendar1.CalendarDimensions = new Size(cols, rows);
                }
            }

            int targetY = panelH - monthCalendar1.PreferredSize.Height - CalendarBottomMargin;
            if (targetY < 0) targetY = 0;

            monthCalendar1.Location = new Point(x, targetY);
        }

        // Adjunta manejadores para el menú "Visita nuestra web" buscando el item por texto.
        // Esto evita depender del nombre generado en el Designer y funciona aunque la jerarquía cambie.
        private void AttachWebsiteMenuHandlers()
        {
            try
            {
                if (menuStrip1 == null) return;

                var menuItem = FindMenuItemByText(menuStrip1.Items, "Visita nuestra web");
                if (menuItem != null)
                {
                    // Evitar múltiples suscripciones
                    menuItem.Click -= VisitWebsiteMenuItem_Click;
                    menuItem.Click += VisitWebsiteMenuItem_Click;
                }
            }
            catch
            {
                // Silenciar errores no críticos de inicialización
            }
        }

        // Busca recursivamente un ToolStripMenuItem por su texto (case-insensitive).
        private ToolStripMenuItem FindMenuItemByText(ToolStripItemCollection items, string text)
        {
            foreach (ToolStripItem it in items)
            {
                if (it is ToolStripMenuItem tsi)
                {
                    if (string.Equals(tsi.Text?.Trim(), text, StringComparison.OrdinalIgnoreCase))
                        return tsi;

                    if (tsi.DropDownItems != null && tsi.DropDownItems.Count > 0)
                    {
                        var found = FindMenuItemByText(tsi.DropDownItems, text);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }

        // Manejador que abre la web en el navegador por defecto
        private void VisitWebsiteMenuItem_Click(object sender, EventArgs e)
        {
            OpenUrl("https://pakets-org.github.io/web/");
        }

        // Abre la URL con el navegador por defecto de forma segura.
        // Si no se puede abrir, copia la URL al portapapeles y muestra error.
        private void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                try { Clipboard.SetText(url); } catch { /* ignorar fallo de portapapeles */ }
                MessageBox.Show($"No se pudo abrir la URL en el navegador. La URL se ha copiado al portapapeles:\n{url}\n\nError: {ex.Message}", "Error al abrir URL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Placeholder: sin lógica responsiva que use panel2
        private void ApplyResponsiveLayout()
        {
            // Intencionalmente vacío: se ha eliminado todo lo relacionado con panel2.
        }

        private void main_Load(object sender, EventArgs e)
        {
            // no-op
        }

        private void nuevoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form childForm = new newcreation();
            childForm.Show();
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }
        private void toolStripStatusLabel1_Click(object sender, EventArgs e) { }
        private void salirToolStripMenuItem_Click(object sender, EventArgs e) { this.Close(); }
        private void seleccionarToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e) { }
        private void acercaDeToolStripMenuItem_Click(object sender, EventArgs e) { new AboutBox1().Show(); }
        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e) { }
        private void abrirToolStripMenuItem_Click(object sender, EventArgs e) { }

        // Nuevo helper: abrir perfil DENTRO de la aplicación (no lanzar asociación/exe externo)
        private void OpenProfileInApp(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            // Registrar en MRU
            RecentProfilesManager.RegisterOpenedProfile(path);

            // Notificar apertura de perfil a suscriptores (main ocultará welcome y mostrará dashboard)
            ProfileSessionManager.OpenProfile(path);

            // Si tienes lógica adicional para cargar el perfil en memoria aquí, ejecútala.
            // Ejemplo: ProfileLoader.Load(path);
        }

        // Al abrir perfil desde el menú ahora abrimos DENTRO de la aplicación (evitar Process.Start en .pakets)
        private void perfilToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog() { Filter = "Perfil de Pakets (*.pakets)|*.pakets" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var path = ofd.FileName;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        // Abrir dentro de la app (no lanzar la asociación que crea otra instancia)
                        OpenProfileInApp(path);

                        // No llamar a Process.Start(path) para evitar que se abra otra instancia de la aplicación.
                        // Si realmente necesitas lanzar el archivo en la asociación del SO, hazlo explícitamente
                        // desde una opción del usuario (ej. "Abrirexternamente").
                    }
                }
            }
        }

        private void VerlaayudaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var helpPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "help.chm");
                if (System.IO.File.Exists(helpPath))
                {
                    Help.ShowHelp(this, helpPath);
                    return;
                }
                MessageBox.Show("No se encontró el archivo de ayuda 'help.chm' junto al ejecutable.", "Ayuda no disponible", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Error al abrir la ayuda: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void separatorPanel_Paint(object sender, PaintEventArgs e) { }
        private void panel3_Paint(object sender, PaintEventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void dateTimePicker1_ValueChanged(object sender, EventArgs e) { }

        private void panel3_Paint_1(object sender, PaintEventArgs e) { }

        private void recentlyopened_screen_Load(object sender, EventArgs e)
        {

        }

        private void usuariosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void informesToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        // Manejador que oculta los controles de bienvenida al abrir perfil y muestra el dashboard
        private void OnProfileOpened(string paketsPath)
        {
            if (this.IsDisposed) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnProfileOpened(paketsPath)));
                return;
            }

            try
            {
                // Ocultar los controles de bienvenida
                if (this.recentlyopened_screen != null) this.recentlyopened_screen.Visible = false;
                if (this.label2 != null) this.label2.Visible = false;
                if (this.panel3 != null) this.panel3.Visible = false;

                // Crear o mostrar el dashboard anclado en 251,27
                if (enterpriseDashboard == null)
                {
                    enterpriseDashboard = new main_enterprise_dashboard
                    {
                        Name = "enterpriseDashboard",
                        Location = new Point(251, 27),
                        Size = new Size(Math.Max(800, this.ClientSize.Width - 251 - 20), Math.Max(400, this.ClientSize.Height - 27 - 50)),
                        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                        Visible = true
                    };
                    this.Controls.Add(enterpriseDashboard);
                }
                else
                {
                    enterpriseDashboard.Location = new Point(251, 27);
                    enterpriseDashboard.Visible = true;
                }

                enterpriseDashboard.BringToFront();

                // Actualizar datos del dashboard desde el archivo .pakets si los métodos públicos existen
                try
                {
                    enterpriseDashboard.UpdateCompanyNameFromPaketsFile(paketsPath);
                    enterpriseDashboard.UpdateUserNameFromPaketsFile(paketsPath);
                }
                catch { }
            }
            catch { }
        }

        // Manejador que restaura los controles al cerrar sesión y oculta/descarta el dashboard
        private void OnProfileClosed()
        {
            if (this.IsDisposed) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(OnProfileClosed));
                return;
            }

            try
            {
                if (this.recentlyopened_screen != null) this.recentlyopened_screen.Visible = true;
                if (this.label2 != null) this.label2.Visible = true;
                if (this.panel3 != null) this.panel3.Visible = true;

                if (enterpriseDashboard != null)
                {
                    try
                    {
                        this.Controls.Remove(enterpriseDashboard);
                        enterpriseDashboard.Dispose();
                    }
                    catch { /* ignorar errores en dispose */ }
                    enterpriseDashboard = null;
                }
            }
            catch { }
        }

        // Manejador para "Cerrar sesión" en el menu Archivo
        private void cerrarSesionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Notificar cierre de perfil: restaura UI si había uno abierto
                ProfileSessionManager.CloseProfile();
                // Mensaje opcional
                MessageBox.Show("Sesión cerrada correctamente.", "Cerrar sesión", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cerrar la sesión: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ayudaToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }

    /// <summary>
    /// Gestor simple de sesión de perfil. 
    /// - Llamar ProfileSessionManager.OpenProfile(path) cuando se abra un .pakets desde cualquier sitio.
    /// - Llamar ProfileSessionManager.CloseProfile() cuando se cierre la sesión del perfil.
    /// Otros componentes pueden suscribirse a los eventos ProfileOpened/ProfileClosed.
    /// </summary>
    public static class ProfileSessionManager
    {
        public static event Action<string> ProfileOpened;
        public static event Action ProfileClosed;

        public static void OpenProfile(string path)
        {
            try
            {
                ProfileOpened?.Invoke(path);
            }
            catch { }
        }

        public static void CloseProfile()
        {
            try
            {
                ProfileClosed?.Invoke();
            }
            catch { }
        }
    }
}
