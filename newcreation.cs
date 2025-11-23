using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PAKETS
{
    public partial class newcreation : Form
    {
        private Panel contentPanel;
        private List<UserControl> steps;
        private int currentStep = -1;

        public newcreation()
        {
            InitializeComponent();

            // Evita redimensionado por bordes/esquinas (incluye gestos táctiles)
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            // Contenedor donde mostraremos las "páginas" del asistente sin abrir nuevas ventanas
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoScroll = true
            };

            // Añadir detrás de los controles existentes (botones creados en el diseñador)
            this.Controls.Add(contentPanel);
            contentPanel.SendToBack();

            // Mostrar primero la pantalla de bienvenida dentro de este formulario.
            ShowWelcome();
        }

        private void ShowWelcome()
        {

            // Ajustar textos de botones del formulario (botones del diseñador)
            var btnAccept = this.Controls.Find("accept", true).FirstOrDefault() as Button;
            if (btnAccept != null) btnAccept.Text = "Aceptar";

            var btnCancel = this.Controls.Find("cancel", true).FirstOrDefault() as Button;
            if (btnCancel != null) btnCancel.Text = "Cancelar";

            // Indicar que aún no se han inicializado los pasos
            currentStep = -1;
            steps = null;
        }

        private void InitializeSteps()
        {
            // Antes de inicializar los pasos, eliminar/ocultar elementos flotantes que provienen del diseñador
            // Queremos mantener solo los botones principales "accept" y "cancel" y contentPanel.
            foreach (var ctrl in this.Controls.OfType<Control>().ToArray())
            {
                if (ctrl == contentPanel) continue;
                if (ctrl.Name == "accept" || ctrl.Name == "cancel") continue;

                // Ocultar botones flotantes no deseados
                if (ctrl is Button)
                {
                    ctrl.Visible = false;
                }

                // Ocultar etiquetas de bienvenida que estuvieran fuera del panel del diseñador (si existen)
                if (ctrl is Label && (ctrl.Name == "label1" || ctrl.Name == "label2" || ctrl.Name == "welcomePara"))
                {
                    ctrl.Visible = false;
                }
            }

            // Crear lista de pasos (UserControls)
            steps = new List<UserControl>();

            // Paso 1: Recopìlación de datos de la empresa (UserControl personalizado)
            var step1 = new newcreationstep1();
            steps.Add(step1);

            // Suscribir dinámicamente a cambios en los TextBox dentro de step1 para controlar el botón "accept"
            Action updateAcceptForStep1 = () =>
            {
                var acceptBtn = this.Controls.Find("accept", true).FirstOrDefault() as Button;
                if (acceptBtn == null) return;

                if (currentStep == 0)
                {
                    var allTextBoxes = GetAllControls(step1).OfType<TextBox>();
                    // Si no hay TextBoxes, dejar habilitado; si hay, requerir que no estén vacíos
                    acceptBtn.Enabled = !allTextBoxes.Any() || allTextBoxes.All(tb => !string.IsNullOrWhiteSpace(tb.Text));
                }
            };

            foreach (var tb in GetAllControls(step1).OfType<TextBox>())
            {
                tb.TextChanged += (s, e) => updateAcceptForStep1();
            }

            // Inicializar estado del botón para el paso 1 (por si ya hay texto)
            updateAcceptForStep1();

            // Paso 2: Recopilación de los sistemas de paquetería que se usarán (existente en el proyecto)
            var step2 = new newcreationstep2();
            steps.Add(step2);

            // No asumimos eventos específicos en step2; el estado del botón se evaluará al mostrar el paso.
            // Paso 3: ...
            var step3 = new newcreationstep3();
            steps.Add(step3);


            // Paso Final: Resumen de todos los datos introducidos y creación del perfil, se guarda en un "Archivo.pakets"
            steps.Add(CreateSummaryStep());

            // Mostrar primer paso
            ShowStep(0);
        }

        private UserControl CreateSummaryStep()
        {
            var uc = new UserControl { Dock = DockStyle.Fill, Padding = new Padding(6) };

            var lbl = new Label
            {
                Text = "4 | Acabando",
                Font = new Font(FontFamily.GenericSansSerif, 16f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(6, 6)
            };

            uc.Controls.Add(lbl);

            // Layout principal: dos columnas, izquierda con lista de campos, derecha con miniaturas
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Location = new Point(6, 36),
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(6, 8, 6, 6)
            };
            // Ajuste de columnas para colocar las miniaturas más hacia el centro:
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f)); // columna izquierda (datos)
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f)); // columna derecha (miniaturas), más ancha para poder desplazar el contenido hacia el centro

            // Panel izquierdo: scroll con TableLayout para pares etiqueta/valor
            var leftPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8, 18, 6, 6) };
            var detailsTable = new TableLayoutPanel
            {
                Name = "detailsTable",
                AutoSize = true,
                Dock = DockStyle.Top,
                ColumnCount = 2,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Margin = new Padding(0)
            };
            detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            detailsTable.Margin = new Padding(0);
            leftPanel.Controls.Add(detailsTable);

            // Panel derecho: miniaturas apiladas; ajustar padding izquierdo pequeño y centrar las miniaturas hacia el centro
            var rightPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8, 18, 12, 6) // padding reducido a la izquierda para acercarlas al centro
            };

            // MINIATURAS MÁS PEQUEÑAS y margen reducido para acercarlas al centro
            var summaryPic1 = new PictureBox
            {
                Name = "summaryPic1",
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(120, 80),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(6, 6, 6, 8)
            };

            var summaryPic2 = new PictureBox
            {
                Name = "summaryPic2",
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(120, 80),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(6, 8, 6, 6)
            };

            // Centrar internamente las miniaturas dentro del panel derecho
            summaryPic1.Anchor = AnchorStyles.Top;
            summaryPic2.Anchor = AnchorStyles.Top;

            rightPanel.Controls.Add(summaryPic1);
            rightPanel.Controls.Add(summaryPic2);

            layout.Controls.Add(leftPanel, 0, 0);
            layout.Controls.Add(rightPanel, 1, 0);

            uc.Controls.Add(layout);

            return uc;
        }

        private void ShowStep(int index)
        {
            if (steps == null || index < 0 || index >= steps.Count) return;

            // Guardar/leer datos entre pasos si es necesario
            if (currentStep == 0 && index == 1)
            {
                var step0 = steps[0];
                var txt = step0.Controls.OfType<TextBox>().FirstOrDefault();
                var value = txt?.Text ?? string.Empty;

                var step1 = steps[1];
                var lblInfo = step1.Controls.Find("lblInfo", true).FirstOrDefault() as Label;
                if (lblInfo != null) lblInfo.Text = $"Valor introducido: {value}";
            }

            // Reemplazar contenido del panel (esto elimina automáticamente la bienvenida que añadimos dentro del panel)
            contentPanel.Controls.Clear();
            var ctrl = steps[index];
            ctrl.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(ctrl);

            currentStep = index;

            // Si es el paso resumen, rellenar con la información recogida
            if (index == steps.Count - 1)
            {
                PopulateSummary(ctrl);
            }

            // Actualizar texto del botón "accept" (Siguiente / Finalizar)
            var btnAccept = this.Controls.Find("accept", true).FirstOrDefault() as Button;
            if (btnAccept != null)
            {
                btnAccept.Text = (currentStep < steps.Count - 1) ? "Siguiente" : "Finalizar";

                // Controlar habilitado por paso sin depender de propiedades/eventos que puedan no existir en los UserControls:
                if (currentStep == 0)
                {
                    var step0 = steps[0];
                    var allTextBoxes = GetAllControls(step0).OfType<TextBox>();
                    btnAccept.Enabled = !allTextBoxes.Any() || allTextBoxes.All(tb => !string.IsNullOrWhiteSpace(tb.Text));
                }
                else if (currentStep == 1)
                {
                    // Intentar detectar un DataGridView dentro del paso 1 y decidir habilitado de forma conservadora.
                    var dgv = GetAllControls(steps[1]).OfType<DataGridView>().FirstOrDefault();
                    if (dgv != null)
                    {
                        // Considerar completado si existe al menos una celda con valor no vacío
                        btnAccept.Enabled = dgv.Rows.Cast<DataGridViewRow>()
                            .SelectMany(r => r.Cells.Cast<DataGridViewCell>())
                            .Any(c => c.Value != null && !string.IsNullOrWhiteSpace(c.Value.ToString()));
                    }
                    else
                    {
                        // Si no hay DataGridView, habilitar por defecto
                        btnAccept.Enabled = true;
                    }
                }
                else
                {
                    btnAccept.Enabled = true;
                }
            }

            // Asegurar que el botón "cancel" tenga texto apropiado
            var btnCancel = this.Controls.Find("cancel", true).FirstOrDefault() as Button;
            if (btnCancel != null) btnCancel.Text = "Cancelar";
        }

        // Rellena la vista resumen con toda la información encontrada en los pasos
        private void PopulateSummary(Control summaryControl)
        {
            var detailsTable = summaryControl.Controls.Find("detailsTable", true).FirstOrDefault() as TableLayoutPanel;
            var pic1 = summaryControl.Controls.Find("summaryPic1", true).FirstOrDefault() as PictureBox;
            var pic2 = summaryControl.Controls.Find("summaryPic2", true).FirstOrDefault() as PictureBox;

            if (detailsTable == null) return;

            // Limpiar tabla de detalles
            detailsTable.SuspendLayout();
            detailsTable.Controls.Clear();
            detailsTable.RowStyles.Clear();
            detailsTable.RowCount = 0;

            // Helper local para añadir fila (etiqueta + valor)
            Action<string, string> addRow = (k, v) =>
            {
                int row = detailsTable.RowCount++;
                detailsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                var lblKey = new Label
                {
                    Text = k,
                    AutoSize = true,
                    Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold),
                    Margin = new Padding(3, 6, 6, 6),
                    Anchor = AnchorStyles.Left | AnchorStyles.Top
                };
                var lblVal = new Label
                {
                    Text = v,
                    AutoSize = true,
                    Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular),
                    Margin = new Padding(3, 6, 3, 6),
                    Anchor = AnchorStyles.Left | AnchorStyles.Top
                };
                detailsTable.Controls.Add(lblKey, 0, row);
                detailsTable.Controls.Add(lblVal, 1, row);
            };

            // Paso 1: Empresa
            if (steps.Count > 0)
            {
                addRow("== Empresa ==", "");
                var kv1 = CollectKeyValuesFromControls(steps[0]);
                foreach (var kv in kv1) addRow(kv.Key + ":", kv.Value);
            }

            // Paso 2: Sistemas / Paquetería
            if (steps.Count > 1)
            {
                addRow("", "");
                addRow("== Sistemas / Paquetería ==", "");
                var s2 = steps[1];
                var tlp = GetAllControls(s2).OfType<TableLayoutPanel>().FirstOrDefault();
                if (tlp != null)
                {
                    int headerRows = tlp.Controls.Find("colHeader_0", true).Any() ? 1 : 0;
                    var textboxes = tlp.Controls.OfType<TextBox>().Where(tb => tlp.GetRow(tb) >= headerRows).ToArray();
                    if (textboxes.Length == 0) addRow("-- Tabla --", "(vacía)");
                    else
                    {
                        foreach (var tb in textboxes)
                        {
                            addRow($"Fila{tlp.GetRow(tb)}-Col{tlp.GetColumn(tb)}:", tb.Text);
                        }
                    }
                }
                else
                {
                    var kv2 = CollectKeyValuesFromControls(s2);
                    foreach (var kv in kv2) addRow(kv.Key + ":", kv.Value);
                }

                var radios = GetAllControls(s2).OfType<RadioButton>();
                foreach (var r in radios) addRow(GetControlLabel(r) ?? r.Name + ":", r.Checked ? "Sí" : "No");
            }

            // Paso 3
            if (steps.Count > 2)
            {
                addRow("", "");
                addRow("== Paso 3 ==", "");
                var kv3 = CollectKeyValuesFromControls(steps[2]);
                foreach (var kv in kv3) addRow(kv.Key + ":", kv.Value);
            }

            detailsTable.ResumeLayout();

            // Copiar imágenes si existen (clonar bitmaps para evitar compartición de recursos)
            if (pic1 != null)
            {
                var img = GetAllControls(steps[0]).OfType<PictureBox>().FirstOrDefault()?.Image;
                pic1.Image = img != null ? new Bitmap(img) : null;
            }
            if (pic2 != null)
            {
                var img2 = GetAllControls(steps[0]).OfType<PictureBox>().Skip(1).FirstOrDefault()?.Image
                           ?? GetAllControls(steps.Count > 2 ? steps[2] : steps[0]).OfType<PictureBox>().FirstOrDefault()?.Image;
                pic2.Image = img2 != null ? new Bitmap(img2) : null;
            }
        }

        // Crear carpeta en Documentos y guardar <Razón_Social>.pakets y copias de hasta 2 imágenes
        private void SavePaketsPackage()
        {
            if (steps == null || steps.Count == 0) throw new InvalidOperationException("Pasos no inicializados.");

            // Obtener datos del paso 1 (empresa)
            var step1 = steps[0];
            var kvsStep1 = CollectKeyValuesFromControls(step1);

            // Intentar averiguar "Razón Social"
            string razon = kvsStep1
                .Select(kv => new { Key = kv.Key ?? "", Value = kv.Value ?? "" })
                .FirstOrDefault(kv => kv.Key.IndexOf("razón", StringComparison.OrdinalIgnoreCase) >= 0
                                    || kv.Key.IndexOf("razon", StringComparison.OrdinalIgnoreCase) >= 0)?.Value;

            if (string.IsNullOrWhiteSpace(razon))
            {
                // fallback: primera entrada no vacía
                razon = kvsStep1.Select(kv => kv.Value).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "PAKETS_Profile";
            }

            // Sanear nombre para carpeta/archivo
            string sanitized = SanitizeFileName(razon);
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // --- NUEVO: asegurar existencia de carpeta central "PAKETS" en Documentos ---
            string paketsRoot = Path.Combine(documents, "PAKETS");
            if (!Directory.Exists(paketsRoot))
            {
                Directory.CreateDirectory(paketsRoot);
            }

            // Carpeta del perfil dentro de Documents\PAKETS\<Razón_Social>
            string folderPath = Path.Combine(paketsRoot, sanitized);
            Directory.CreateDirectory(folderPath);

            // Construir contenido del .pakets (texto simple UTF8)
            var sb = new StringBuilder();
            sb.AppendLine("PAKETS Profile");
            sb.AppendLine("Generado: " + DateTime.Now.ToString("s"));
            sb.AppendLine();

            for (int i = 0; i < steps.Count; i++)
            {
                sb.AppendLine($"== Paso {i + 1} ==");
                var kvs = CollectKeyValuesFromControls(steps[i]);
                foreach (var kv in kvs)
                {
                    sb.AppendLine($"{kv.Key}: {kv.Value}");
                }

                // Si paso 2 contiene tabla con textboxes, añadir sus valores (compatibilidad)
                if (i == 1)
                {
                    var tlp = GetAllControls(steps[i]).OfType<TableLayoutPanel>().FirstOrDefault();
                    if (tlp != null)
                    {
                        int headerRows = tlp.Controls.Find("colHeader_0", true).Any() ? 1 : 0;
                        var textboxes = tlp.Controls.OfType<TextBox>().Where(tb => tlp.GetRow(tb) >= headerRows).ToArray();
                        if (textboxes.Length > 0)
                        {
                            sb.AppendLine("-- Tabla --");
                            foreach (var tb in textboxes)
                            {
                                sb.AppendLine($"Fila{tlp.GetRow(tb)}-Col{tlp.GetColumn(tb)}: {tb.Text}");
                            }
                        }
                    }
                }

                sb.AppendLine();
            }

            // Guardar archivo .pakets dentro de Documents\PAKETS\<Razón_Social>\
            string paketsFile = Path.Combine(folderPath, sanitized + ".pakets");
            File.WriteAllText(paketsFile, sb.ToString(), Encoding.UTF8);

            // Registrar el perfil recién creado en MRU
            try
            {
                RecentProfilesManager.RegisterOpenedProfile(paketsFile);
            }
            catch
            {
                // no bloquear si falla el registro
            }

            // Copiar hasta 2 imágenes encontradas en los pasos (preferir step1 images)
            var images = GetAllControls(step1).OfType<PictureBox>().Select(pb => pb.Image).Where(img => img != null).Cast<Image>().ToList();

            // Si no hay 2 en step1 buscar en otros pasos
            if (images.Count < 2)
            {
                var more = steps.SelectMany(s => GetAllControls(s).OfType<PictureBox>().Select(pb => pb.Image)).Where(img => img != null).Cast<Image>();
                foreach (var im in more)
                {
                    if (images.Count >= 2) break;
                    if (!images.Contains(im)) images.Add(im);
                }
            }

            for (int idx = 0; idx < images.Count && idx < 2; idx++)
            {
                var img = images[idx];
                string imgName = Path.Combine(folderPath, $"image{idx + 1}.png");
                // Guardar clon para evitar bloqueo del Image original
                using (var bmp = new Bitmap(img))
                {
                    bmp.Save(imgName, ImageFormat.Png);
                }
            }
        }

        private string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if (invalid.Contains(c)) sb.Append('_');
                else sb.Append(c);
            }
            var result = sb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(result)) result = "PAKETS_Profile";
            return result;
        }

        // Helper para recorrer recursivamente controles hijos
        private IEnumerable<Control> GetAllControls(Control parent)
        {
            if (parent == null) yield break;
            foreach (Control c in parent.Controls)
            {
                yield return c;
                foreach (var child in GetAllControls(c))
                    yield return child;
            }
        }

        // Reutiliza la función existente del archivo: recoge pares (etiqueta, valor) desde controles dentro de parent
        private List<KeyValuePair<string, string>> CollectKeyValuesFromControls(Control parent)
        {
            var list = new List<KeyValuePair<string, string>>();
            if (parent == null) return list;

            var textboxes = GetAllControls(parent).OfType<TextBox>();
            foreach (var tb in textboxes)
            {
                var label = GetControlLabel(tb) ?? tb.Name;
                list.Add(new KeyValuePair<string, string>(label, tb.Text));
            }

            var combos = GetAllControls(parent).OfType<ComboBox>();
            foreach (var cb in combos)
            {
                var label = GetControlLabel(cb) ?? cb.Name;
                var sel = cb.SelectedItem != null ? cb.SelectedItem.ToString() : cb.Text;
                list.Add(new KeyValuePair<string, string>(label, sel));
            }

            var checkboxes = GetAllControls(parent).OfType<CheckBox>();
            foreach (var ch in checkboxes)
            {
                var label = GetControlLabel(ch) ?? ch.Name;
                list.Add(new KeyValuePair<string, string>(label, ch.Checked ? "Sí" : "No"));
            }

            var radios = GetAllControls(parent).OfType<RadioButton>();
            foreach (var r in radios)
            {
                var label = GetControlLabel(r) ?? r.Name;
                list.Add(new KeyValuePair<string, string>(label, r.Checked ? "Sí" : "No"));
            }

            return list;
        }

        // Intenta encontrar una etiqueta asociada (Label) en el mismo contenedor por proximidad
        private string GetControlLabel(Control ctl)
        {
            if (ctl == null) return null;
            Control search = ctl.Parent;
            for (int depth = 0; depth < 4 && search != null; depth++)
            {
                var candidate = search.Controls.OfType<Label>()
                    .Where(l => Math.Abs(l.Location.Y - ctl.Location.Y) < 14 && l.Location.X < ctl.Location.X + 10)
                    .OrderByDescending(l => l.Location.X)
                    .FirstOrDefault();
                if (candidate != null)
                {
                    var text = candidate.Text;
                    if (!string.IsNullOrWhiteSpace(text)) return text.Trim().TrimEnd(':');
                }
                search = search.Parent;
            }
            return null;
        }

        private void label1_Click(object sender, EventArgs e)
        {
            // El diseñador requiere este manejador. Mantener vacío o añadir la lógica deseada.
        }

        // Agrego el manejador faltante requerido por el diseñador (label2.Click)
        private void label2_Click(object sender, EventArgs e)
        {
            // Handler vacío para evitar error en tiempo de diseño.
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void accept_Click(object sender, EventArgs e)
        {
            // Si los pasos no están inicializados, este botón es "Aceptar" de la bienvenida:
            if (steps == null || steps.Count == 0)
            {
                InitializeSteps();
                return;
            }

            if (currentStep < steps.Count - 1)
            {
                ShowStep(currentStep + 1);
            }
            else
            {
                // Último paso: crear archivo .pakets y cerrar asistente
                try
                {
                    SavePaketsPackage();
                    MessageBox.Show("Perfil guardado correctamente.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al guardar el perfil: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                this.Close();
            }
        }
    }
}
