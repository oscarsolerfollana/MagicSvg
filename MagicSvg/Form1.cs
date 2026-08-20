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
        private Bitmap? _minimalLines;
        private Bitmap? _polygons;
        private List<LineProcessor.Segment>? _segments;
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
            picMinLines.Image    = null;
            picPolygons.Image    = null;

            btnProcess.Enabled          = true;
            btnSave.Enabled             = false;
            btnExportSvg.Enabled        = false;

            string statusMsg =
                $"Imagen cargada: {Path.GetFileName(dlg.FileName)}  " +
                $"({_original.Width} × {_original.Height} px)";

            var suggestion = ParamLearner.Suggest(_original);
            if (suggestion is not null)
            {
                _suspendAutoProcess = true;
                ApplySettings(suggestion);
                _suspendAutoProcess = false;
                statusMsg += $"  · Parámetros sugeridos a partir de {ParamLearner.ExampleCount} ejemplo(s) aprendido(s).";
            }

            lblStatus.Text = statusMsg;

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
                var settings = BuildSettingsFromUi();

                DisposePhases();

                var phases = LineProcessor.ProcessImageDetailed(_original, settings);
                _binary     = phases.Binary;
                _artifacts  = phases.Artifacts;
                _houghRaw   = phases.HoughRaw;
                _classified = phases.Classified;
                _merged     = phases.Merged;
                _result     = phases.Final;
                _segments   = phases.Segments;

                var fusedGraph = SvgExporter.ComputeFusedGraph(_segments);

                var minLineEdges = SvgExporter.ComputeMinimalLineEdges(fusedGraph);
                var collinearLines = SvgExporter.GroupCollinearLines(minLineEdges);
                _minimalLines = SvgExporter.RenderMinimalLinesBitmap(
                    collinearLines, phases.Width, phases.Height, settings.OutputLineThickness);

                var faces = SvgExporter.ExtractFacesFromGraph(fusedGraph);
                _polygons = SvgExporter.RenderFacesBitmap(faces, phases.Width, phases.Height, settings.OutputLineThickness);

                picBinary.Image      = _binary;
                picArtifacts.Image   = _artifacts;
                picHoughRaw.Image    = _houghRaw;
                picClassified.Image  = _classified;
                picMerged.Image      = _merged;
                picResult.Image      = _result;
                picMinLines.Image    = _minimalLines;
                picPolygons.Image    = _polygons;

                btnSave.Enabled      = true;
                btnExportSvg.Enabled = true;
                lblStatus.Text       = "Proceso completado. Puedes guardar el resultado.";
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
            ApplySettings(new LineProcessor.Settings());
            _suspendAutoProcess = false;
            TryAutoProcess();
        }

        // ── Validar y aprender parámetros ───────────────────────────────────
        private void BtnValidate_Click(object? sender, EventArgs e)
        {
            if (_original is null) return;

            ParamLearner.AddExample(_original, BuildSettingsFromUi());

            lblStatus.Text =
                $"Parámetros guardados como ejemplo válido. " +
                $"Ejemplos aprendidos: {ParamLearner.ExampleCount}.";
        }

        private LineProcessor.Settings BuildSettingsFromUi() => new()
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
            ContactTolerance       = (int)numContactTolerance.Value,
            IntersectionTolerance  = (int)numIntersectionTolerance.Value,
            ExtendMaxDistance      = (int)numExtendMaxDistance.Value,
            ClipTolerance          = (int)numClipTolerance.Value,
            CornerTolerance        = (int)numCornerTolerance.Value,
            MinCornerSegmentLength = (int)numMinCornerSegmentLength.Value
        };

        private void ApplySettings(LineProcessor.Settings s)
        {
            numBinaryThreshold.Value        = s.BinaryThreshold;
            numDilationKernelSize.Value     = s.DilationKernelSize;
            numDilationIterations.Value     = s.DilationIterations;
            numMinComponentArea.Value       = s.MinComponentArea;
            numHoughThreshold.Value         = s.HoughThreshold;
            numMinLineLength.Value          = s.MinLineLength;
            numMaxLineGap.Value             = s.MaxLineGap;
            numAngleTolerance.Value         = (decimal)s.AngleSnapTolerance;
            numMergePositionTolerance.Value = s.MergePositionTolerance;
            numSegmentGapTolerance.Value    = s.SegmentGapTolerance;
            numMinOutputLength.Value        = s.MinOutputSegmentLength;
            numLineThickness.Value          = s.OutputLineThickness;
            numContactTolerance.Value       = s.ContactTolerance;
            numIntersectionTolerance.Value  = s.IntersectionTolerance;
            numExtendMaxDistance.Value      = s.ExtendMaxDistance;
            numClipTolerance.Value          = s.ClipTolerance;
            numCornerTolerance.Value        = s.CornerTolerance;
            numMinCornerSegmentLength.Value = s.MinCornerSegmentLength;
        }

        // ── Exportar SVG (polígonos mínimos) ────────────────────────────────
        private void BtnExportSvg_Click(object? sender, EventArgs e)
        {
            if (_result is null || _segments is null) return;

            using var dlg = new SaveFileDialog
            {
                Title      = "Exportar SVG (polígonos)",
                Filter     = "SVG (*.svg)|*.svg|Todos los archivos|*.*",
                DefaultExt = "svg",
                FileName   = "poligonos"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            lblStatus.Text = "Generando SVG…";
            Application.DoEvents();

            try
            {
                var faces = SvgExporter.ExtractFaces(_segments);
                string svg = SvgExporter.RenderPolygonsSvg(faces, _result.Width, _result.Height);
                File.WriteAllText(dlg.FileName, svg, System.Text.Encoding.UTF8);
                lblStatus.Text = $"SVG guardado: {Path.GetFileName(dlg.FileName)}";

                if (faces.Count > 0)
                {
                    var resp = MessageBox.Show(
                        "¿Quieres asignar nombres de habitación a los polígonos?",
                        "Crear habitaciones", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (resp == DialogResult.Yes)
                    {
                        using var roomForm = new RoomEditorForm(faces, _result.Width, _result.Height, _result);
                        roomForm.ShowDialog(this);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al exportar SVG:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error al exportar SVG.";
            }
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
            if (_suspendAutoProcess || _original is null) return;
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
            _result?.Dispose();       _result       = null;
            _binary?.Dispose();        _binary       = null;
            _artifacts?.Dispose();     _artifacts    = null;
            _houghRaw?.Dispose();      _houghRaw     = null;
            _classified?.Dispose();    _classified   = null;
            _merged?.Dispose();        _merged       = null;
            _minimalLines?.Dispose();  _minimalLines = null;
            _polygons?.Dispose();      _polygons     = null;
            _segments = null;
        }
    }
}
