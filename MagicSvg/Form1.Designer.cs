namespace MagicSvg
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // Toolbar
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton btnOpen;
        private System.Windows.Forms.ToolStripButton btnProcess;
        private System.Windows.Forms.ToolStripButton btnSave;
        private System.Windows.Forms.ToolStripButton btnExportSvg;

        // Imágenes (cuadrícula 3×2)
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.TableLayoutPanel tableImages;

        private System.Windows.Forms.Panel panelOriginal;
        private System.Windows.Forms.Panel panelBinary;
        private System.Windows.Forms.Panel panelArtifacts;
        private System.Windows.Forms.Panel panelHoughRaw;
        private System.Windows.Forms.Panel panelClassified;
        private System.Windows.Forms.Panel panelMerged;
        private System.Windows.Forms.Panel panelResult;

        private System.Windows.Forms.Label lblOriginal;
        private System.Windows.Forms.Label lblBinary;
        private System.Windows.Forms.Label lblArtifacts;
        private System.Windows.Forms.Label lblHoughRaw;
        private System.Windows.Forms.Label lblClassified;
        private System.Windows.Forms.Label lblMerged;
        private System.Windows.Forms.Label lblResult;

        private System.Windows.Forms.PictureBox picOriginal;
        private System.Windows.Forms.PictureBox picBinary;
        private System.Windows.Forms.PictureBox picArtifacts;
        private System.Windows.Forms.PictureBox picHoughRaw;
        private System.Windows.Forms.PictureBox picClassified;
        private System.Windows.Forms.PictureBox picMerged;
        private System.Windows.Forms.PictureBox picResult;

        // Parámetros
        private System.Windows.Forms.NumericUpDown numBinaryThreshold;
        private System.Windows.Forms.NumericUpDown numDilationKernelSize;
        private System.Windows.Forms.NumericUpDown numDilationIterations;
        private System.Windows.Forms.NumericUpDown numMinComponentArea;
        private System.Windows.Forms.NumericUpDown numHoughThreshold;
        private System.Windows.Forms.NumericUpDown numMinLineLength;
        private System.Windows.Forms.NumericUpDown numMaxLineGap;
        private System.Windows.Forms.NumericUpDown numAngleTolerance;
        private System.Windows.Forms.NumericUpDown numMergePositionTolerance;
        private System.Windows.Forms.NumericUpDown numSegmentGapTolerance;
        private System.Windows.Forms.NumericUpDown numMinOutputLength;
        private System.Windows.Forms.NumericUpDown numLineThickness;
        private System.Windows.Forms.NumericUpDown numContactTolerance;
        private System.Windows.Forms.NumericUpDown numIntersectionTolerance;
        private System.Windows.Forms.NumericUpDown numExtendMaxDistance;
        private System.Windows.Forms.NumericUpDown numClipTolerance;
        private System.Windows.Forms.NumericUpDown numCornerTolerance;
        private System.Windows.Forms.NumericUpDown numMinCornerSegmentLength;
        private System.Windows.Forms.Button btnReset;

        // Status
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            toolStrip    = new System.Windows.Forms.ToolStrip();
            btnOpen      = new System.Windows.Forms.ToolStripButton();
            btnProcess   = new System.Windows.Forms.ToolStripButton();
            btnSave      = new System.Windows.Forms.ToolStripButton();
            btnExportSvg = new System.Windows.Forms.ToolStripButton();
            splitMain   = new System.Windows.Forms.SplitContainer();
            tableImages = new System.Windows.Forms.TableLayoutPanel();

            panelOriginal   = new System.Windows.Forms.Panel();
            panelBinary     = new System.Windows.Forms.Panel();
            panelArtifacts  = new System.Windows.Forms.Panel();
            panelHoughRaw   = new System.Windows.Forms.Panel();
            panelClassified = new System.Windows.Forms.Panel();
            panelMerged     = new System.Windows.Forms.Panel();
            panelResult     = new System.Windows.Forms.Panel();

            lblOriginal   = new System.Windows.Forms.Label();
            lblBinary     = new System.Windows.Forms.Label();
            lblArtifacts  = new System.Windows.Forms.Label();
            lblHoughRaw   = new System.Windows.Forms.Label();
            lblClassified = new System.Windows.Forms.Label();
            lblMerged     = new System.Windows.Forms.Label();
            lblResult     = new System.Windows.Forms.Label();

            picOriginal   = new System.Windows.Forms.PictureBox();
            picBinary     = new System.Windows.Forms.PictureBox();
            picArtifacts  = new System.Windows.Forms.PictureBox();
            picHoughRaw   = new System.Windows.Forms.PictureBox();
            picClassified = new System.Windows.Forms.PictureBox();
            picMerged     = new System.Windows.Forms.PictureBox();
            picResult     = new System.Windows.Forms.PictureBox();

            numBinaryThreshold        = CreateNum( 0,  255, 200);
            numDilationKernelSize     = CreateNum( 1,   21,   3);
            numDilationIterations     = CreateNum( 0,   10,   1);
            numMinComponentArea       = CreateNum( 0, 50000, 500);
            numHoughThreshold         = CreateNum(10,  200,  40);
            numMinLineLength          = CreateNum( 5,  500,  20);
            numMaxLineGap             = CreateNum( 0,  200,  15);
            numAngleTolerance         = CreateNum( 1,   44,  10);
            numMergePositionTolerance = CreateNum( 1,  200,  30);
            numSegmentGapTolerance    = CreateNum( 0,  300,  35);
            numMinOutputLength        = CreateNum( 0,  500,  30);
            numLineThickness          = CreateNum( 1,   10,   2);
            numContactTolerance       = CreateNum( 0,   50,   8);
            numIntersectionTolerance  = CreateNum( 0,  100,  24);
            numExtendMaxDistance      = CreateNum( 0, 1000, 200);
            numClipTolerance          = CreateNum( 0,   500,   8);
            numCornerTolerance        = CreateNum( 0,  300,  80);
            numMinCornerSegmentLength = CreateNum( 0,   50,   8);
            btnReset       = new System.Windows.Forms.Button();
            statusStrip    = new System.Windows.Forms.StatusStrip();
            lblStatus      = new System.Windows.Forms.ToolStripStatusLabel();

            numBinaryThreshold.ValueChanged        += Num_ValueChanged;
            numDilationKernelSize.ValueChanged     += Num_ValueChanged;
            numDilationIterations.ValueChanged     += Num_ValueChanged;
            numMinComponentArea.ValueChanged       += Num_ValueChanged;
            numHoughThreshold.ValueChanged         += Num_ValueChanged;
            numMinLineLength.ValueChanged          += Num_ValueChanged;
            numMaxLineGap.ValueChanged             += Num_ValueChanged;
            numAngleTolerance.ValueChanged         += Num_ValueChanged;
            numMergePositionTolerance.ValueChanged += Num_ValueChanged;
            numSegmentGapTolerance.ValueChanged    += Num_ValueChanged;
            numMinOutputLength.ValueChanged        += Num_ValueChanged;
            numLineThickness.ValueChanged          += Num_ValueChanged;
            numContactTolerance.ValueChanged       += Num_ValueChanged;
            numIntersectionTolerance.ValueChanged  += Num_ValueChanged;
            numExtendMaxDistance.ValueChanged      += Num_ValueChanged;
            numClipTolerance.ValueChanged          += Num_ValueChanged;
            numCornerTolerance.ValueChanged        += Num_ValueChanged;
            numMinCornerSegmentLength.ValueChanged += Num_ValueChanged;

            splitMain.SuspendLayout();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            tableImages.SuspendLayout();
            SuspendLayout();

            // ── ToolStrip ───────────────────────────────────────────────────
            toolStrip.Dock      = System.Windows.Forms.DockStyle.Top;
            toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                btnOpen, btnProcess,
                new System.Windows.Forms.ToolStripSeparator(),
                btnSave,
                new System.Windows.Forms.ToolStripSeparator(),
                btnExportSvg
            });

            btnOpen.Text         = "📂  Abrir imagen";
            btnOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnOpen.Click       += BtnOpen_Click;

            btnProcess.Text         = "⚙  Procesar";
            btnProcess.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnProcess.Enabled      = false;
            btnProcess.Click       += BtnProcess_Click;

            btnSave.Text         = "💾  Guardar resultado";
            btnSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnSave.Enabled      = false;
            btnSave.Click       += BtnSave_Click;

            btnExportSvg.Text         = "⬡  Exportar SVG (polígonos)";
            btnExportSvg.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnExportSvg.Enabled      = false;
            btnExportSvg.Click       += BtnExportSvg_Click;

            // ── SplitContainer
            splitMain.Dock          = System.Windows.Forms.DockStyle.Fill;
            splitMain.Orientation   = System.Windows.Forms.Orientation.Vertical;
            splitMain.FixedPanel    = System.Windows.Forms.FixedPanel.Panel2;
            splitMain.SplitterWidth = 4;

            // ── TableLayoutPanel 4×2 ───────────────────────────────────────
            tableImages.Dock        = System.Windows.Forms.DockStyle.Fill;
            tableImages.ColumnCount = 4;
            tableImages.RowCount    = 2;
            tableImages.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent, 25F));
            tableImages.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent, 25F));
            tableImages.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent, 25F));
            tableImages.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent, 25F));
            tableImages.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Percent, 50F));
            tableImages.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Percent, 50F));
            tableImages.Padding = new System.Windows.Forms.Padding(4);

            // ── Fuente para etiquetas ──────────────────────────────────────
            var imgFont = new System.Drawing.Font(
                "Segoe UI", 9F, System.Drawing.FontStyle.Bold);

            // ── Crear los 7 paneles ────────────────────────────────────────
            SetupImagePanel(panelOriginal,   picOriginal,   lblOriginal,
                "Original",                       imgFont);
            SetupImagePanel(panelBinary,     picBinary,     lblBinary,
                "① Binarización",                      imgFont);
            SetupImagePanel(panelArtifacts,  picArtifacts,  lblArtifacts,
                "② Eliminación de artefactos  (rojo=descartado)", imgFont);
            SetupImagePanel(panelHoughRaw,   picHoughRaw,   lblHoughRaw,
                "③ Transformada de Hough",                  imgFont);
            SetupImagePanel(panelClassified, picClassified, lblClassified,
                "④ Clasificación  (azul=0°  rojo=90°  colores=diagonales)", imgFont);
            SetupImagePanel(panelMerged,     picMerged,     lblMerged,
                "⑤ Unificación de paralelas",                   imgFont);
            SetupImagePanel(panelResult,     picResult,     lblResult,
                "⑥ Extensión/recorte",               imgFont);

            // Fila 0: Original | Binario | Artefactos | Hough crudo
            tableImages.Controls.Add(panelOriginal,   0, 0);
            tableImages.Controls.Add(panelBinary,     1, 0);
            tableImages.Controls.Add(panelArtifacts,  2, 0);
            tableImages.Controls.Add(panelHoughRaw,   3, 0);
            // Fila 1: Clasificados | Fusionados | Resultado
            tableImages.Controls.Add(panelClassified, 0, 1);
            tableImages.Controls.Add(panelMerged,     1, 1);
            tableImages.Controls.Add(panelResult,     2, 1);

            // ── Botón reset ────────────────────────────────────────────────
            btnReset.Text   = "Restablecer valores";
            btnReset.Height = 28;
            btnReset.Click += BtnReset_Click;

            // ── Ensamblar SplitContainer ───────────────────────────────────
            splitMain.Panel1.Controls.Add(tableImages);
            splitMain.Panel2.AutoScroll = true;
            splitMain.Panel2.Controls.Add(BuildSettingsPanel());

            // ── StatusStrip ────────────────────────────────────────────────
            lblStatus.Text   = "Listo. Abre una imagen para comenzar.";
            lblStatus.Spring = true;
            statusStrip.Items.Add(lblStatus);

            // ── Form ───────────────────────────────────────────────────────
            Controls.Add(splitMain);
            Controls.Add(toolStrip);
            Controls.Add(statusStrip);

            tableImages.ResumeLayout(false);
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            splitMain.ResumeLayout(false);

            ClientSize    = new System.Drawing.Size(1440, 820);
            MinimumSize   = new System.Drawing.Size(1000, 580);
            Text          = "MagicSvg – Rectificador de líneas";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            ResumeLayout(false);
            PerformLayout();
            splitMain.Panel1MinSize    = 600;
            splitMain.Panel2MinSize    = 250;
            splitMain.SplitterDistance = 1140;
        }

        // ── Helper: construye un panel de imagen ──────────────────────────────

        private static void SetupImagePanel(
            System.Windows.Forms.Panel        panel,
            System.Windows.Forms.PictureBox   pic,
            System.Windows.Forms.Label        lbl,
            string                            title,
            System.Drawing.Font               font)
        {
            lbl.Text      = title;
            lbl.Dock      = System.Windows.Forms.DockStyle.Top;
            lbl.Height    = 22;
            lbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lbl.Font      = font;

            ((System.ComponentModel.ISupportInitialize)pic).BeginInit();
            pic.SizeMode    = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pic.Dock        = System.Windows.Forms.DockStyle.Fill;
            pic.BackColor   = System.Drawing.Color.FromArgb(224, 224, 224);
            pic.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            ((System.ComponentModel.ISupportInitialize)pic).EndInit();

            panel.Dock    = System.Windows.Forms.DockStyle.Fill;
            panel.Padding = new System.Windows.Forms.Padding(3);
            panel.Controls.Add(pic);
            panel.Controls.Add(lbl);
        }

        // ── Construcción del panel de parámetros ──────────────────────────────

        private System.Windows.Forms.TableLayoutPanel BuildSettingsPanel()
        {
            var nFont = new System.Drawing.Font("Segoe UI", 8.5F);
            var bFont = new System.Drawing.Font(
                "Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            var sFont = new System.Drawing.Font(
                "Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);

            var tbl = new System.Windows.Forms.TableLayoutPanel
            {
                ColumnCount  = 2,
                RowCount     = 0,
                AutoSize     = true,
                AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink,
                Dock         = System.Windows.Forms.DockStyle.Top,
                Padding      = new System.Windows.Forms.Padding(8, 6, 8, 8)
            };
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent, 100F));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Absolute, 68F));

            int r = SAddRow(tbl, 34);
            var title = new System.Windows.Forms.Label
            {
                Text      = "⚙  Parámetros",
                Font      = bFont,
                Dock      = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                AutoSize  = false
            };
            tbl.Controls.Add(title, 0, r);
            tbl.SetColumnSpan(title, 2);

            SAddSection(tbl, sFont, "Binarización");
            SAddParam(tbl, nFont, "Umbral (0-255):",          numBinaryThreshold);
            SAddParam(tbl, nFont, "Kernel dilatación (px):",  numDilationKernelSize);
            SAddParam(tbl, nFont, "Iteraciones dilatación:",  numDilationIterations);

            SAddSection(tbl, sFont, "Eliminación de artefactos");
            SAddParam(tbl, nFont, "Área mín. componente (px²):", numMinComponentArea);

            SAddSection(tbl, sFont, "Detección (Hough)");
            SAddParam(tbl, nFont, "Umbral votos:",           numHoughThreshold);
            SAddParam(tbl, nFont, "Long. mín. det. (px):",   numMinLineLength);
            SAddParam(tbl, nFont, "Hueco máx. Hough (px):",  numMaxLineGap);

            SAddSection(tbl, sFont, "Clasificación");
            SAddParam(tbl, nFont, "Tolerancia ángulo (°):",  numAngleTolerance);

            SAddSection(tbl, sFont, "Unificación de paralelas");
            SAddParam(tbl, nFont, "Tolerancia posición (px):", numMergePositionTolerance);
            SAddParam(tbl, nFont, "Hueco segmentos (px):",     numSegmentGapTolerance);
            SAddParam(tbl, nFont, "Long. mín. salida (px):",   numMinOutputLength);

            SAddSection(tbl, sFont, "Extensión/recorte");
            SAddParam(tbl, nFont, "Tolerancia contacto (px):",       numContactTolerance);
            SAddParam(tbl, nFont, "Tolerancia intersección (px):",   numIntersectionTolerance);
            SAddParam(tbl, nFont, "Distancia máx. extensión (px):",  numExtendMaxDistance);
            SAddParam(tbl, nFont, "Tolerancia recorte (px):",        numClipTolerance);
            SAddParam(tbl, nFont, "Tolerancia esquina (px):",        numCornerTolerance);
            SAddParam(tbl, nFont, "Long. mín. esquina (px):",        numMinCornerSegmentLength);

            SAddSection(tbl, sFont, "Resultado");
            SAddParam(tbl, nFont, "Grosor de líneas (px):",  numLineThickness);

            r = SAddRow(tbl, 36);
            btnReset.Dock   = System.Windows.Forms.DockStyle.Fill;
            btnReset.Margin = new System.Windows.Forms.Padding(0, 6, 0, 4);
            tbl.Controls.Add(btnReset, 0, r);
            tbl.SetColumnSpan(btnReset, 2);

            return tbl;
        }

        private static int SAddRow(
            System.Windows.Forms.TableLayoutPanel tbl, int height)
        {
            int idx = tbl.RowCount;
            tbl.RowStyles.Add(new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Absolute, (float)height));
            tbl.RowCount++;
            return idx;
        }

        private static void SAddSection(
            System.Windows.Forms.TableLayoutPanel tbl,
            System.Drawing.Font font, string text)
        {
            int r = SAddRow(tbl, 30);
            var lbl = new System.Windows.Forms.Label
            {
                Text      = text,
                Font      = font,
                Dock      = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.BottomLeft,
                ForeColor = System.Drawing.SystemColors.ControlDarkDark,
                Padding   = new System.Windows.Forms.Padding(0, 8, 0, 0),
                AutoSize  = false
            };
            tbl.Controls.Add(lbl, 0, r);
            tbl.SetColumnSpan(lbl, 2);
        }

        private static void SAddParam(
            System.Windows.Forms.TableLayoutPanel tbl,
            System.Drawing.Font font, string text,
            System.Windows.Forms.NumericUpDown num)
        {
            int r = SAddRow(tbl, 26);
            var lbl = new System.Windows.Forms.Label
            {
                Text      = text,
                Font      = font,
                Dock      = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                AutoSize  = false
            };
            num.Dock = System.Windows.Forms.DockStyle.Fill;
            num.Font = font;
            tbl.Controls.Add(lbl, 0, r);
            tbl.Controls.Add(num, 1, r);
        }

        private static System.Windows.Forms.NumericUpDown CreateNum(
            decimal min, decimal max, decimal value)
        {
            return new System.Windows.Forms.NumericUpDown
            {
                Minimum = min, Maximum = max, Value = value, Increment = 1
            };
        }

        #endregion
    }
}
