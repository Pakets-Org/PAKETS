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

        public main()
        {
            InitializeComponent();

            // Suscribir el menú "Ver la ayuda"
            this.VerlaayudaToolStripMenuItem.Click += VerlaayudaToolStripMenuItem_Click;

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

            // Ya no hay lógica dependiente de panel2. Sólo asegurar posición del calendario.
            PositionMonthCalendarInPanel1();
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            // Mantener calendario pegado al borde inferior de su panel
            PositionMonthCalendarInPanel1();
        }

        // Posiciona monthCalendar1 en la parte inferior del panel1 con un pequeño margen
        private void PositionMonthCalendarInPanel1()
        {
            if (monthCalendar1 == null || panel1 == null) return;

            // Calcular Y para pegar al fondo del panel1
            int targetY = panel1.ClientSize.Height - monthCalendar1.Height - CalendarBottomMargin;
            if (targetY < 0) targetY = 0;

            // Mantener X actual si es válido, si no usar padding original (17)
            int x = monthCalendar1.Left;
            if (x < 0) x = 17;

            monthCalendar1.Location = new Point(x, targetY);
            monthCalendar1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
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
        private void perfilToolStripMenuItem_Click(object sender, EventArgs e) { using (var ofd = new OpenFileDialog() { Filter = "Perfil de Pakets (*.pakets)|*.pakets" }) ofd.ShowDialog(); }

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

        private void recentlyopened_screen1_Load(object sender, EventArgs e)
        {

        }
    }
}
