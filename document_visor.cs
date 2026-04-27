using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace PAKETS
{
    public partial class document_visor : Form
    {
        private string _currentFilePath = null;
        private bool _hasUnsavedChanges = false;

        public document_visor()
        {
            InitializeComponent();
            this.MinimumSize = new System.Drawing.Size(620, 350);
            this.SizeGripStyle = SizeGripStyle.Show;
            WireChangeEvents();
            UpdateTitle();
            this.Load   += (s, e) => UpdateLayout();
            this.Resize += (s, e) => UpdateLayout();
        }

        public document_visor(string filePath) : this()
        {
            LoadPak(filePath);
        }

        // ── Cambios sin guardar ──────────────────────────────────────────────

        private void WireChangeEvents()
        {
            textBox1.TextChanged  += OnFieldChanged;
            textBox2.TextChanged  += OnFieldChanged;
            textBox3.TextChanged  += OnFieldChanged;
            textBox4.TextChanged  += OnFieldChanged;
            textBox5.TextChanged  += OnFieldChanged;
            textBox6.TextChanged  += OnFieldChanged;
            textBox7.TextChanged  += OnFieldChanged;
            textBox8.TextChanged  += OnFieldChanged;
            textBox9.TextChanged  += OnFieldChanged;
            textBox10.TextChanged += OnFieldChanged;
            textBox11.TextChanged += OnFieldChanged;
            textBox12.TextChanged += OnFieldChanged;
            comboBox1.SelectedIndexChanged += OnFieldChanged;
            incidencias_checkbox.CheckedChanged += OnFieldChanged;
            dateTimePicker1.ValueChanged += OnFieldChanged;
        }

        private void OnFieldChanged(object sender, EventArgs e)
        {
            if (!_hasUnsavedChanges)
            {
                _hasUnsavedChanges = true;
                UpdateTitle();
            }
        }

        private void UpdateTitle()
        {
            string name = _currentFilePath != null
                ? Path.GetFileNameWithoutExtension(_currentFilePath)
                : "Nuevo documento";
            Text = _hasUnsavedChanges ? name + " *" : name;
        }

        // ── Formato .pak (JSON) ──────────────────────────────────────────────

        private PakData CollectData()
        {
            return new PakData
            {
                Version          = "1",
                Fecha            = dateTimePicker1.Value.ToString("yyyy-MM-dd"),
                Hora             = textBox6.Text.Trim(),
                NSeguimiento     = textBox1.Text.Trim(),
                NExpedicion      = textBox4.Text.Trim(),
                AgenciaTransporte = textBox2.Text.Trim(),
                Portes           = comboBox1.SelectedItem?.ToString() ?? "",
                Peso             = textBox7.Text.Trim(),
                Volumen          = textBox11.Text.Trim(),
                MedidasX         = textBox10.Text.Trim(),
                MedidasY         = textBox9.Text.Trim(),
                MedidasZ         = textBox8.Text.Trim(),
                Bultos           = textBox3.Text.Trim(),
                Incidencias      = incidencias_checkbox.Checked,
                Observaciones    = textBox12.Text.Trim(),
                CampoExtra       = textBox5.Text.Trim()
            };
        }

        private void PopulateFields(PakData data)
        {
            if (DateTime.TryParse(data.Fecha, out DateTime fecha))
                dateTimePicker1.Value = fecha;

            textBox6.Text  = data.Hora;
            textBox1.Text  = data.NSeguimiento;
            textBox4.Text  = data.NExpedicion;
            textBox2.Text  = data.AgenciaTransporte;
            textBox7.Text  = data.Peso;
            textBox11.Text = data.Volumen;
            textBox10.Text = data.MedidasX;
            textBox9.Text  = data.MedidasY;
            textBox8.Text  = data.MedidasZ;
            textBox3.Text  = data.Bultos;
            textBox12.Text = data.Observaciones;
            textBox5.Text  = data.CampoExtra;
            incidencias_checkbox.Checked = data.Incidencias;

            int idx = comboBox1.Items.IndexOf(data.Portes);
            comboBox1.SelectedIndex = idx >= 0 ? idx : -1;
        }

        // ── Guardar ─────────────────────────────────────────────────────────

        private void SaveToPath(string path)
        {
            var data = CollectData();
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            _currentFilePath = path;
            _hasUnsavedChanges = false;
            UpdateTitle();
        }

        private void GuardarPak()
        {
            if (_currentFilePath != null)
            {
                SaveToPath(_currentFilePath);
                return;
            }
            GuardarComoPak();
        }

        private void GuardarComoPak()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title  = "Guardar paquete";
                dlg.Filter = "Archivo PAKETS (*.pak)|*.pak|Todos los archivos (*.*)|*.*";
                dlg.DefaultExt = "pak";

                string profileDir = GetProfileDirectory();
                if (profileDir != null)
                    dlg.InitialDirectory = profileDir;

                if (!string.IsNullOrEmpty(_currentFilePath))
                    dlg.FileName = Path.GetFileName(_currentFilePath);

                if (dlg.ShowDialog() == DialogResult.OK)
                    SaveToPath(dlg.FileName);
            }
        }

        // ── Abrir ────────────────────────────────────────────────────────────

        private void LoadPak(string path)
        {
            try
            {
                string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                var data = JsonConvert.DeserializeObject<PakData>(json);
                PopulateFields(data);
                _currentFilePath = path;
                _hasUnsavedChanges = false;
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir el archivo:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AbrirPak()
        {
            if (!ConfirmarDescartarCambios()) return;

            using (var dlg = new OpenFileDialog())
            {
                dlg.Title  = "Abrir paquete";
                dlg.Filter = "Archivo PAKETS (*.pak)|*.pak|Todos los archivos (*.*)|*.*";

                string profileDir = GetProfileDirectory();
                if (profileDir != null)
                    dlg.InitialDirectory = profileDir;

                if (dlg.ShowDialog() == DialogResult.OK)
                    LoadPak(dlg.FileName);
            }
        }

        // ── Utilidades ───────────────────────────────────────────────────────

        private bool ConfirmarDescartarCambios()
        {
            if (!_hasUnsavedChanges) return true;
            var result = MessageBox.Show(
                "Hay cambios sin guardar. ¿Desea guardarlos antes de continuar?",
                "Cambios sin guardar",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes) { GuardarPak(); return true; }
            if (result == DialogResult.No)  return true;
            return false;
        }

        private string GetProfileDirectory()
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string paketsDir = Path.Combine(docs, "PAKETS");
            return Directory.Exists(paketsDir) ? paketsDir : null;
        }

        // ── Eventos de menú ──────────────────────────────────────────────────

        private void guardarToolStripMenuItem_Click(object sender, EventArgs e)
            => GuardarPak();

        private void guardarComoToolStripMenuItem_Click(object sender, EventArgs e)
            => GuardarComoPak();

        private void paqueteAisladoToolStripMenuItem_Click(object sender, EventArgs e)
            => AbrirPak();

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
            => Close();

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_hasUnsavedChanges && !ConfirmarDescartarCambios())
                e.Cancel = true;
        }

        private void aToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void UpdateLayout()
        {
            if (!IsHandleCreated || ClientSize.Width <= 0) return;

            SuspendLayout();
            try
            {
                int w = ClientSize.Width;
                int h = ClientSize.Height;
                const int mL = 12;
                const int mR = 12;
                int usable   = Math.Max(200, w - mL - mR);
                int menuBot  = menuStrip1 != null ? menuStrip1.Bottom : 24;

                int row1Y    = menuBot + 16;
                int row2Y    = row1Y + 28;
                int row3Y    = row2Y + 28;
                int tableTop = row3Y + 30;

                int midX       = mL + usable / 2;
                int rightStart = midX + 8;
                int rightUsable = mL + usable - rightStart;
                int smallField  = Math.Max(35, (int)(rightUsable * 0.12));

                // ── Fila 1, izquierda: Fecha [dateTimePicker1]  Hora [textBox6] ──
                fecha.Location = new System.Drawing.Point(mL, row1Y + 3);
                int dpW = Math.Max(100, midX - fecha.Right - 4 - 60);
                dateTimePicker1.SetBounds(fecha.Right + 4, row1Y, dpW, dateTimePicker1.Height);
                hora.Location = new System.Drawing.Point(dateTimePicker1.Right + 8, row1Y + 3);
                textBox6.SetBounds(hora.Right + 4, row1Y,
                    Math.Max(40, midX - hora.Right - 4), textBox6.Height);

                // ── Fila 1, derecha: A.Transporte [textBox2]  Portes [comboBox1] [textBox5] ──
                agencia_transporte.Location = new System.Drawing.Point(rightStart, row1Y + 3);
                int tb2W = Math.Max(60, (int)(rightUsable * 0.32));
                textBox2.SetBounds(agencia_transporte.Right + 4, row1Y, tb2W, textBox2.Height);
                portes.Location = new System.Drawing.Point(textBox2.Right + 8, row1Y + 3);
                int cmbW = Math.Max(60, (int)(rightUsable * 0.24));
                comboBox1.SetBounds(portes.Right + 4, row1Y, cmbW, comboBox1.Height);
                textBox5.SetBounds(comboBox1.Right + 8, row1Y,
                    Math.Max(30, mL + usable - comboBox1.Right - 8), textBox5.Height);

                // ── Fila 2, izquierda: Nº Seguimiento [textBox1] ──
                seguimiento.Location = new System.Drawing.Point(mL, row2Y + 3);
                textBox1.SetBounds(seguimiento.Right + 4, row2Y,
                    Math.Max(80, midX - seguimiento.Right - 4), textBox1.Height);

                // ── Fila 2, derecha: Peso [textBox7]  Volumen [textBox11]  Bultos [textBox3]  [incidencias] ──
                peso.Location = new System.Drawing.Point(rightStart, row2Y + 3);
                textBox7.SetBounds(peso.Right + 4, row2Y, smallField, textBox7.Height);
                volumen.Location = new System.Drawing.Point(textBox7.Right + 6, row2Y + 3);
                textBox11.SetBounds(volumen.Right + 4, row2Y, smallField, textBox11.Height);
                bultos.Location = new System.Drawing.Point(textBox11.Right + 6, row2Y + 3);
                textBox3.SetBounds(bultos.Right + 4, row2Y, smallField, textBox3.Height);
                incidencias_checkbox.Location = new System.Drawing.Point(textBox3.Right + 8, row2Y + 1);

                // ── Fila 3, izquierda: Nº Expedición [textBox4] ──
                expedicion.Location = new System.Drawing.Point(mL, row3Y + 3);
                textBox4.SetBounds(expedicion.Right + 4, row3Y,
                    Math.Max(80, midX - expedicion.Right - 4), textBox4.Height);

                // ── Fila 3, derecha: Medidas [textBox10][textBox9][textBox8]  Observaciones [textBox12] ──
                medidas.Location = new System.Drawing.Point(rightStart, row3Y + 3);
                textBox10.SetBounds(medidas.Right + 4, row3Y, smallField, textBox10.Height);
                textBox9.SetBounds(textBox10.Right + 4, row3Y, smallField, textBox9.Height);
                textBox8.SetBounds(textBox9.Right + 4, row3Y, smallField, textBox8.Height);
                textBox12.SetBounds(textBox8.Right + 8, row3Y,
                    Math.Max(60, mL + usable - textBox8.Right - 8), textBox12.Height);

                // ── Panel de contenido: ocupa el espacio restante ──
                background_table.SetBounds(mL, tableTop, usable,
                    Math.Max(60, h - tableTop - 8));
            }
            finally
            {
                ResumeLayout();
            }
        }
    }

    internal class PakData
    {
        public string Version           { get; set; }
        public string Fecha             { get; set; }
        public string Hora              { get; set; }
        public string NSeguimiento      { get; set; }
        public string NExpedicion       { get; set; }
        public string AgenciaTransporte { get; set; }
        public string Portes            { get; set; }
        public string Peso              { get; set; }
        public string Volumen           { get; set; }
        public string MedidasX          { get; set; }
        public string MedidasY          { get; set; }
        public string MedidasZ          { get; set; }
        public string Bultos            { get; set; }
        public bool   Incidencias       { get; set; }
        public string Observaciones     { get; set; }
        public string CampoExtra        { get; set; }
    }
}
