using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PAKETS
{
    public partial class main_enterprise_dashboard : UserControl
    {
        // DataGridView que contendrá la "tabla" de archivos
        private DataGridView filesDataGridView;

        public main_enterprise_dashboard()
        {
            InitializeComponent();

            // Crear la tabla programáticamente y añadirla al control
            CreateFilesTable();

            // Si el control ya se carga en tiempo de diseño, también intentamos establecer el nombre.
            // La carga definitiva se realiza en el evento Load, pero permitimos también actualización aquí.
            // (No bloqueará si no existe la carpeta)
            TryLoadCompanyNameFromDocumentsPakets();
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Columnas sugeridas (puedes ajustar textos y tipos según necesidad)
            filesDataGridView.Columns.Add("colName", "Nombre");
            filesDataGridView.Columns.Add("colDate", "Fecha");
            filesDataGridView.Columns.Add("colUser", "Usuario");
            filesDataGridView.Columns.Add("colSize", "Tamaño");

            // Estética mínima
            filesDataGridView.RowHeadersVisible = false;
            filesDataGridView.EnableHeadersVisualStyles = false;
            filesDataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            filesDataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            filesDataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Ejemplo de fila de muestra (opcional). Elimina o comenta si no quieres datos por defecto.
            filesDataGridView.Rows.Add("DocumentoEjemplo.pdf", DateTime.Now.ToString("dd/MM/yyyy HH:mm"), "usuario_demo", "1.2 MB");

            // Evento cuando cambie la selección (puedes enlazar con la lógica existente)
            filesDataGridView.SelectionChanged += FilesDataGridView_SelectionChanged;

            // Calcular Y para situarla por debajo del header/label existente del diseñador.
            // Cambiado: priorizar label4 (texto "Seleccione algun archivo...") para asegurar que la tabla quede por debajo del label.
            int topY = 72;
            try
            {
                // PRIORIDAD: label4 (mensaje instructivo), luego panel3 (línea), luego label2 (empresa), por último default.
                if (this.label4 != null && !this.label4.IsDisposed)
                {
                    topY = this.label4.Bottom + 6; // dejar 6px de separación tras el label
                }
                else if (this.panel3 != null && !this.panel3.IsDisposed)
                {
                    topY = this.panel3.Bottom + 8; // alternativa: línea separadora
                }
                else if (this.label2 != null && !this.label2.IsDisposed)
                {
                    topY = this.label2.Bottom + 10; // si sólo hay el nombre de empresa
                }
            }
            catch
            {
                topY = 72;
            }

            // Posicionar y dimensionar inicialmente usando el top calculado
            filesDataGridView.Location = new Point(8, topY);
            filesDataGridView.Size = new Size(Math.Max(100, this.ClientSize.Width - 16), Math.Max(60, this.ClientSize.Height - topY - 8));

            // Añadir al control; al usar Anchor se mantendrá debajo del header y botones
            this.Controls.Add(filesDataGridView);

            // Asegurar que la tabla quede detrás de los controles del encabezado, si estos ya existen en el diseñador
            foreach (Control c in this.Controls)
            {
                // Si detecta controles con nombres habituales de header, los trae al frente
                if (c.Name.IndexOf("header", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c.Name.IndexOf("top", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c == this.panel3 || c == this.label2 || c == this.label1 || c == this.label3 || c == this.label4)
                {
                    c.BringToFront();
                }
            }

            // Gestionar redimensionado inicial (por si el control ya tiene tamaño final)
            filesDataGridView.Size = new Size(this.ClientSize.Width - 16, this.ClientSize.Height - filesDataGridView.Location.Y - 8);

            // Asegurar que si el contenedor cambia de tamaño la tabla se reposicione correctamente respecto a label4/panel3/label2.
            this.Resize += (s, e) =>
            {
                try
                {
                    int newTop = topY;
                    if (this.label4 != null && !this.label4.IsDisposed)
                        newTop = this.label4.Bottom + 6;
                    else if (this.panel3 != null && !this.panel3.IsDisposed)
                        newTop = this.panel3.Bottom + 8;
                    else if (this.label2 != null && !this.label2.IsDisposed)
                        newTop = this.label2.Bottom + 10;

                    filesDataGridView.Location = new Point(8, newTop);
                    filesDataGridView.Size = new Size(Math.Max(100, this.ClientSize.Width - 16), Math.Max(60, this.ClientSize.Height - newTop - 8));
                }
                catch { }
            };
        }

        private void FilesDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            // Aquí puedes obtener la fila seleccionada y actualizar la UI según necesites.
            // Ejemplo:
            // if (filesDataGridView.SelectedRows.Count > 0)
            // {
            //     var nombre = filesDataGridView.SelectedRows[0].Cells["colName"].Value?.ToString();
            //     // actualizar labels o cargar vista previa...
            // }
        }

        /// <summary>
        /// Intenta localizar el archivo *.pakets dentro de "Mis Documentos\PAKETS" y establecer
        /// el label con el nombre de la carpeta que contiene ese archivo (que corresponde a "Razón Social").
        /// Selecciona el archivo .pakets más reciente si hay varios. También actualiza el label "USUARIO".
        /// </summary>
        private void TryLoadCompanyNameFromDocumentsPakets()
        {
            try
            {
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string paketsRoot = Path.Combine(documents, "PAKETS");

                if (!Directory.Exists(paketsRoot))
                {
                    // No existe la carpeta PAKETS en Documentos: mantener texto por defecto
                    return;
                }

                // Buscar archivos .pakets en subcarpetas (recursivo)
                var paketsFiles = Directory.EnumerateFiles(paketsRoot, "*.pakets", SearchOption.AllDirectories);

                // Tomar el más reciente por fecha de modificación
                string selected = paketsFiles
                    .OrderByDescending(p => File.GetLastWriteTimeUtc(p))
                    .FirstOrDefault();

                if (selected != null)
                {
                    UpdateCompanyNameFromPaketsFile(selected);
                    UpdateUserNameFromPaketsFile(selected);
                }
            }
            catch
            {
                // Ignorar errores y mantener el texto por defecto en los labels.
            }
        }

        /// <summary>
        /// Establece el texto del label con la "Razón Social" obtenida a partir del archivo .pakets.
        /// Actualiza con el nombre de la carpeta padre (según especificación del usuario).
        /// También disponible para que otros componentes llamen cuando abran explícitamente un archivo .pakets.
        /// </summary>
        /// <param name="paketsFilePath">Ruta completa al archivo .pakets</param>
        public void UpdateCompanyNameFromPaketsFile(string paketsFilePath)
        {
            if (string.IsNullOrWhiteSpace(paketsFilePath))
                return;

            try
            {
                if (!File.Exists(paketsFilePath))
                    return;

                // Según la especificación, la carpeta que contiene archivo.pakets tiene el nombre de la Razón Social
                var parent = Directory.GetParent(paketsFilePath);
                if (parent != null)
                {
                    string razonSocial = parent.Name;

                    // Actualizar label de forma segura (en el hilo UI)
                    if (this.label2 != null && !this.label2.IsDisposed)
                    {
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke(new Action(() => this.label2.Text = razonSocial));
                        }
                        else
                        {
                            this.label2.Text = razonSocial;
                        }
                    }
                }
            }
            catch
            {
                // Silenciar errores y no alterar estado si ocurre algo inesperado
            }
        }

        /// <summary>
        /// Intenta extraer el nombre de usuario del contenido del archivo .pakets y actualiza el label "USUARIO".
        /// Si no se encuentra un valor dentro del archivo, usa Environment.UserName como fallback.
        /// </summary>
        /// <param name="paketsFilePath">Ruta completa al archivo .pakets</param>
        public void UpdateUserNameFromPaketsFile(string paketsFilePath)
        {
            if (string.IsNullOrWhiteSpace(paketsFilePath))
                return;

            try
            {
                if (!File.Exists(paketsFilePath))
                    return;

                string content = File.ReadAllText(paketsFilePath, Encoding.UTF8);
                string usuario = ParseUsuarioFromContent(content);

                if (string.IsNullOrWhiteSpace(usuario))
                {
                    // Fallback a usuario del sistema si no se encontró en el archivo
                    usuario = Environment.UserName;
                }

                // Actualizar label1 de forma segura (en el hilo UI)
                if (this.label1 != null && !this.label1.IsDisposed)
                {
                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke(new Action(() => this.label1.Text = usuario));
                    }
                    else
                    {
                        this.label1.Text = usuario;
                    }
                }
            }
            catch
            {
                // No hacer nada en caso de error; dejar el label por defecto
            }
        }

        /// <summary>
        /// Intentos prudentes de extracción del campo "usuario" en distintos formatos:
        /// - JSON: "usuario": "valor" o "user": "valor"
        /// - Líneas con formato clave=valor o clave: valor (usuario, user, username)
        /// Devuelve null si no encuentra nada razonable.
        /// </summary>
        private string ParseUsuarioFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                // 1) Buscar JSON común: "usuario": "valor" o "user": "valor"
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

                // 2) Buscar líneas tipo "usuario = valor" o "usuario: valor"
                var linePattern = new Regex(@"(?mi)^\s*(?:usuario|user|username)\s*[:=]\s*(.+)$", RegexOptions.IgnoreCase);
                var lm = linePattern.Match(content);
                if (lm.Success && lm.Groups.Count > 1)
                {
                    var val = lm.Groups[1].Value.Trim();
                    // Eliminar comillas y comas finales
                    val = val.Trim().Trim('"', '\'', ',');
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }

                // 3) Buscar en contenido general: usuario=valor (no en inicio de línea)
                var anyPattern = new Regex(@"(?i)(?:usuario|user|username)\s*[:=]\s*([^\s,;]+)");
                var am = anyPattern.Match(content);
                if (am.Success && am.Groups.Count > 1)
                {
                    return am.Groups[1].Value.Trim().Trim('"', '\'').Trim();
                }
            }
            catch
            {
                // Ignorar fallos de parsing
            }

            return null;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void main_enterprise_dashboard_Load(object sender, EventArgs e)
        {
            // Al cargarse el control intentamos establecer la Razón Social y el Usuario
            TryLoadCompanyNameFromDocumentsPakets();
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
