namespace MagicSvg
{
    public partial class Form1 : Form
    {
        private Bitmap? _original;
        private Bitmap? _binary;
        private Bitmap? _artifacts;
        private Bitmap? _houghRaw;
        private Bitmap? _classified;
        private Bitmap? _merged;
        private Bitmap? _result;
        private bool    _suspendAutoProcess;

        public Form1()
        {
            InitializeComponent();
        }

        // ── Abrir imagen ────────────────────────────────────────────────────
        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Abrir imagen",
                Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|Todos los archivos|*.*"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            DisposePhases();

            _original = new Bitmap(dlg.FileName);
            picOriginal.Image    = _original;
            picBinary.Image      = null;
            picArtifacts.Image   = null;
            picHoughRaw.Image    = null;
            picClassified.Image  = null;
            picMerged.Image      = null;
            picResult.Image      = null;

            btnProcess.Enabled = true;
            btnSave.Enabled    = false;
            lblStatus.Text =
                $"Imagen cargada: {Path.GetFileName(dlg.FileName)}  " +
                $"({_original.Width} × {_original.Height} px)";

            ProcessImageOnLoad();
        }

        // ── Procesar ────────────────────────────────────────────────────────
        private void BtnProcess_Click(object? sender, EventArgs e)
        {
            if (_original is null) return;

            btnProcess.Enabled = false;
            btnSave.Enabled    = false;
            lblStatus.Text     = "Procesando…";
            Application.DoEvents();

            try
            {
                var settings = new LineProcessor.Settings
                {
                    BinaryThreshold        = (int)numBinaryThreshold.Value,
                    DilationKernelSize     = (int)numDilationKernelSize.Value,
                    DilationIterations     = (int)numDilationIterations.Value,
                    MinComponentArea       = (int)numMinComponentArea.Value,
                    HoughThreshold         = (int)numHoughThreshold.Value,
                    MinLineLength          = (int)numMinLineLength.Value,
                    MaxLineGap             = (int)numMaxLineGap.Value,
                    AngleSnapTolerance     = (double)numAngleTolerance.Value,
                    MergePositionTolerance = (int)numMergePositionTolerance.Value,
                    SegmentGapTolerance    = (int)numSegmentGapTolerance.Value,
                    MinOutputSegmentLength = (int)numMinOutputLength.Value,
                    OutputLineThickness    = (int)numLineThickness.Value,
                    SnapTolerance          = (int)numSnapTolerance.Value
                };

                DisposePhases();

                var phases = LineProcessor.ProcessImageDetailed(_original, settings);
                _binary     = phases.Binary;
                _artifacts  = phases.Artifacts;
                _houghRaw   = phases.HoughRaw;
                _classified = phases.Classified;
                _merged     = phases.Merged;
                _result     = phases.Final;

                picBinary.Image      = _binary;
                picArtifacts.Image   = _artifacts;
                picHoughRaw.Image    = _houghRaw;
                picClassified.Image  = _classified;
                picMerged.Image      = _merged;
                picResult.Image      = _result;

                btnSave.Enabled = true;
                lblStatus.Text  = "Proceso completado. Puedes guardar el resultado.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al procesar la imagen:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error al procesar.";
            }
            finally
            {
                btnProcess.Enabled = true;
            }
        }

        // ── Restablecer parámetros ──────────────────────────────────────────
        private void BtnReset_Click(object? sender, EventArgs e)
        {
            _suspendAutoProcess = true;
            var d = new LineProcessor.Settings();
            numBinaryThreshold.Value        = d.BinaryThreshold;
            numDilationKernelSize.Value     = d.DilationKernelSize;
            numDilationIterations.Value     = d.DilationIterations;
            numMinComponentArea.Value       = d.MinComponentArea;
            numHoughThreshold.Value         = d.HoughThreshold;
            numMinLineLength.Value          = d.MinLineLength;
            numMaxLineGap.Value             = d.MaxLineGap;
            numAngleTolerance.Value         = (decimal)d.AngleSnapTolerance;
            numMergePositionTolerance.Value = d.MergePositionTolerance;
            numSegmentGapTolerance.Value    = d.SegmentGapTolerance;
            numMinOutputLength.Value        = d.MinOutputSegmentLength;
            numLineThickness.Value          = d.OutputLineThickness;
            numSnapTolerance.Value          = d.SnapTolerance;
            _suspendAutoProcess = false;
            TryAutoProcess();
        }

        // ── Guardar resultado ───────────────────────────────────────────────
        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (_result is null) return;

            using var dlg = new SaveFileDialog
            {
                Title       = "Guardar resultado",
                Filter      = "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg|BMP (*.bmp)|*.bmp",
                DefaultExt  = "png",
                FileName    = "lineas_rectas"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            var fmt = dlg.FilterIndex switch
            {
                2 => System.Drawing.Imaging.ImageFormat.Jpeg,
                3 => System.Drawing.Imaging.ImageFormat.Bmp,
                _ => System.Drawing.Imaging.ImageFormat.Png
            };

            _result.Save(dlg.FileName, fmt);
            lblStatus.Text = $"Guardado: {Path.GetFileName(dlg.FileName)}";
        }

        // ── Auto-proceso ────────────────────────────────────────────────────────
        private void TryAutoProcess()
        {
            if (_suspendAutoProcess || !chkAutoProcess.Checked || _original is null) return;
            BtnProcess_Click(null, EventArgs.Empty);
        }

        private void ProcessImageOnLoad()
        {
            if (_original is null) return;
            BtnProcess_Click(null, EventArgs.Empty);
        }

        private void Num_ValueChanged(object? sender, EventArgs e) => TryAutoProcess();

        // ── Helpers ─────────────────────────────────────────────────────────
        private void DisposePhases()
        {
            _result?.Dispose();     _result     = null;
            _binary?.Dispose();     _binary     = null;
            _artifacts?.Dispose();  _artifacts  = null;
            _houghRaw?.Dispose();   _houghRaw   = null;
            _classified?.Dispose(); _classified = null;
            _merged?.Dispose();     _merged     = null;
        }
    }
}
