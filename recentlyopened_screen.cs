using System;
using System.Drawing;
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
            // comportamiento existente
        }
    }
}
