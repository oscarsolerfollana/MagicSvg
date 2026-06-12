namespace MagicSvg
{
    /// <summary>
    /// Ventana que muestra el SVG de polígonos generado y permite al usuario
    /// seleccionar cada polígono y asignarle un nombre de habitación (guardado
    /// como atributo <c>data-room</c>), además de añadir textos libres que se
    /// pueden arrastrar por el lienzo.
    /// </summary>
    public class RoomEditorForm : Form
    {
        private readonly List<List<Point>> _faces;
        private readonly int               _imgWidth;
        private readonly int               _imgHeight;
        private readonly Bitmap?           _background;
        private readonly Dictionary<int, string> _roomNames = new();
        private readonly List<SvgTextItem> _texts = new();

        private readonly Panel  _canvas;
        private readonly Button _btnSave;
        private readonly Button _btnAddText;
        private readonly Label  _lblStatus;

        private double _scale   = 1.0;
        private double _offsetX;
        private double _offsetY;
        private int    _hoverIndex = -1;

        private bool _addTextMode;
        private int  _draggingTextIndex = -1;
        private bool _dragMoved;
        private bool _suppressClick;

        public RoomEditorForm(List<List<Point>> faces, int imgWidth, int imgHeight, Bitmap? background)
        {
            _faces      = faces;
            _imgWidth   = imgWidth;
            _imgHeight  = imgHeight;
            _background = background;

            Text          = "Asignar habitaciones";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize    = new System.Drawing.Size(900, 700);
            MinimumSize   = new System.Drawing.Size(500, 400);

            _canvas = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White
            };
            _canvas.Paint      += Canvas_Paint;
            _canvas.MouseDown       += Canvas_MouseDown;
            _canvas.MouseMove       += Canvas_MouseMove;
            _canvas.MouseUp         += Canvas_MouseUp;
            _canvas.MouseClick      += Canvas_MouseClick;
            _canvas.MouseDoubleClick += Canvas_MouseDoubleClick;
            _canvas.Resize     += (_, _) => _canvas.Invalidate();

            _btnSave = new Button
            {
                Text   = "💾  Guardar SVG con habitaciones",
                Dock   = DockStyle.Right,
                Width  = 220
            };
            _btnSave.Click += BtnSave_Click;

            _btnAddText = new Button
            {
                Text   = "🔤  Añadir texto",
                Dock   = DockStyle.Right,
                Width  = 130
            };
            _btnAddText.Click += BtnAddText_Click;

            _lblStatus = new Label
            {
                Text      = "Haz clic sobre un polígono para asignarle un nombre de habitación.",
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 0, 0)
            };

            var bottom = new Panel
            {
                Dock   = DockStyle.Bottom,
                Height = 36
            };
            bottom.Controls.Add(_lblStatus);
            bottom.Controls.Add(_btnAddText);
            bottom.Controls.Add(_btnSave);

            Controls.Add(_canvas);
            Controls.Add(bottom);
        }

        // ── Dibujo ───────────────────────────────────────────────────────────

        private void Canvas_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (_imgWidth <= 0 || _imgHeight <= 0) return;

            _scale = Math.Min(
                _canvas.Width  / (double)_imgWidth,
                _canvas.Height / (double)_imgHeight);
            if (_scale <= 0) _scale = 1.0;

            _offsetX = (_canvas.Width  - _imgWidth  * _scale) / 2.0;
            _offsetY = (_canvas.Height - _imgHeight * _scale) / 2.0;

            if (_background is not null)
            {
                var destRect = new RectangleF(
                    (float)_offsetX, (float)_offsetY,
                    (float)(_imgWidth * _scale), (float)(_imgHeight * _scale));
                g.DrawImage(_background, destRect);
            }

            for (int i = 0; i < _faces.Count; i++)
            {
                var pts = ToScreen(_faces[i]);

                bool hasRoom = _roomNames.TryGetValue(i, out var room) && !string.IsNullOrWhiteSpace(room);
                bool hover    = i == _hoverIndex;

                if (hasRoom || hover)
                {
                    Color fill = hasRoom ? Color.FromArgb(70, 0, 160, 0) : Color.FromArgb(50, 0, 120, 255);
                    using var brush = new SolidBrush(fill);
                    g.FillPolygon(brush, pts);
                }

                using var pen = new Pen(hover ? Color.DodgerBlue : Color.Black, hover ? 2f : 1f);
                g.DrawPolygon(pen, pts);

                if (hasRoom)
                {
                    var centroid = Centroid(pts);
                    using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    var size = g.MeasureString(room, font);
                    var textPos = new PointF(centroid.X - size.Width / 2f, centroid.Y - size.Height / 2f);

                    using var bg = new SolidBrush(Color.FromArgb(200, Color.White));
                    g.FillRectangle(bg, textPos.X - 2, textPos.Y - 1, size.Width + 4, size.Height + 2);
                    using var textBrush = new SolidBrush(Color.Black);
                    g.DrawString(room, font, textBrush, textPos);
                }
            }

            for (int i = 0; i < _texts.Count; i++)
            {
                var item = _texts[i];
                if (string.IsNullOrEmpty(item.Text)) continue;

                using var font = ScaledFont(item.FontSize);
                var center = ToScreenPoint(item.X, item.Y);
                var size   = g.MeasureString(item.Text, font);
                var rect   = new RectangleF(center.X - size.Width / 2f, center.Y - size.Height / 2f, size.Width, size.Height);

                bool dragging = i == _draggingTextIndex;
                using var bg = new SolidBrush(Color.FromArgb(220, Color.LightYellow));
                g.FillRectangle(bg, rect.X - 3, rect.Y - 2, rect.Width + 6, rect.Height + 4);

                using var pen = new Pen(dragging ? Color.DodgerBlue : Color.Gray, dragging ? 2f : 1f);
                g.DrawRectangle(pen, rect.X - 3, rect.Y - 2, rect.Width + 6, rect.Height + 4);

                using var textBrush = new SolidBrush(Color.Black);
                g.DrawString(item.Text, font, textBrush, rect.Location);
            }
        }

        /// <summary>Crea una fuente cuyo tamaño en pantalla refleja el tamaño en
        /// coordenadas de imagen, escalado según el zoom actual del lienzo.</summary>
        private Font ScaledFont(double fontSize)
        {
            float size = (float)Math.Max(1.0, fontSize * _scale);
            return new Font("Segoe UI", size);
        }

        private PointF[] ToScreen(List<Point> face)
        {
            var pts = new PointF[face.Count];
            for (int i = 0; i < face.Count; i++)
                pts[i] = ToScreenPoint(face[i].X, face[i].Y);
            return pts;
        }

        private PointF ToScreenPoint(double x, double y) =>
            new((float)(_offsetX + x * _scale), (float)(_offsetY + y * _scale));

        private (double x, double y) ToImagePoint(int screenX, int screenY) =>
            ((screenX - _offsetX) / _scale, (screenY - _offsetY) / _scale);

        private static PointF Centroid(PointF[] pts)
        {
            float cx = 0, cy = 0;
            foreach (var p in pts) { cx += p.X; cy += p.Y; }
            return new PointF(cx / pts.Length, cy / pts.Length);
        }

        private static (double x, double y) CentroidImg(List<Point> face)
        {
            double cx = 0, cy = 0;
            foreach (var p in face) { cx += p.X; cy += p.Y; }
            return (cx / face.Count, cy / face.Count);
        }

        // ── Interacción: habitaciones y texto ───────────────────────────────

        private void Canvas_MouseDown(object? sender, MouseEventArgs e)
        {
            if (_addTextMode) return;

            int idx = FindTextAt(e.X, e.Y);
            if (idx < 0) return;

            _draggingTextIndex = idx;
            _dragMoved         = false;
        }

        private void Canvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_draggingTextIndex >= 0)
            {
                var (x, y) = ToImagePoint(e.X, e.Y);
                _texts[_draggingTextIndex].X = x;
                _texts[_draggingTextIndex].Y = y;
                _dragMoved = true;
                _canvas.Invalidate();
                return;
            }

            if (!_addTextMode)
            {
                _canvas.Cursor = FindTextAt(e.X, e.Y) >= 0 ? Cursors.SizeAll : Cursors.Default;

                int idx = FindFaceAt(e.X, e.Y);
                if (idx != _hoverIndex)
                {
                    _hoverIndex = idx;
                    _canvas.Invalidate();
                }
            }
        }

        private void Canvas_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_draggingTextIndex < 0) return;

            if (_dragMoved) _suppressClick = true;
            _draggingTextIndex = -1;
            _dragMoved         = false;
            _canvas.Invalidate();
        }

        private void Canvas_MouseClick(object? sender, MouseEventArgs e)
        {
            if (_suppressClick) { _suppressClick = false; return; }

            if (_addTextMode)
            {
                AddTextAt(e.X, e.Y);
                return;
            }

            int idx = FindFaceAt(e.X, e.Y);
            if (idx < 0) return;

            _roomNames.TryGetValue(idx, out var current);
            using var dlg = new RoomNameDialog("Habitación", current ?? "");
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var name = dlg.RoomName;
            if (string.IsNullOrWhiteSpace(name))
                _roomNames.Remove(idx);
            else
                _roomNames[idx] = name;

            _lblStatus.Text = $"Habitaciones asignadas: {_roomNames.Count} de {_faces.Count} polígonos.";
            _canvas.Invalidate();
        }

        private void Canvas_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (_addTextMode) return;

            int idx = FindTextAt(e.X, e.Y);
            if (idx < 0) return;

            _suppressClick = true;
            EditTextAt(idx);
        }

        // ── Texto libre ──────────────────────────────────────────────────────

        private void BtnAddText_Click(object? sender, EventArgs e)
        {
            _addTextMode  = !_addTextMode;
            _canvas.Cursor = _addTextMode ? Cursors.Cross : Cursors.Default;
            _btnAddText.Text = _addTextMode ? "🔤  Haz clic para colocar…" : "🔤  Añadir texto";
            _lblStatus.Text  = _addTextMode
                ? "Haz clic en el lienzo para colocar el texto."
                : "Haz clic sobre un polígono para asignarle un nombre de habitación.";
        }

        private void AddTextAt(int screenX, int screenY)
        {
            using var dlg = new TextItemDialog("Añadir texto", "", 14);
            bool ok = dlg.ShowDialog(this) == DialogResult.OK;

            _addTextMode  = false;
            _canvas.Cursor = Cursors.Default;
            _btnAddText.Text = "🔤  Añadir texto";
            _lblStatus.Text  = "Haz clic sobre un polígono para asignarle un nombre de habitación.";

            if (!ok || string.IsNullOrWhiteSpace(dlg.TextValue)) return;

            double x, y;
            int faceIdx = FindFaceAt(screenX, screenY);
            if (faceIdx >= 0)
                (x, y) = CentroidImg(_faces[faceIdx]);
            else
                (x, y) = ToImagePoint(screenX, screenY);

            _texts.Add(new SvgTextItem { X = x, Y = y, Text = dlg.TextValue, FontSize = dlg.FontSize });
            _canvas.Invalidate();
        }

        private void EditTextAt(int index)
        {
            var item = _texts[index];
            using var dlg = new TextItemDialog("Editar texto", item.Text, item.FontSize);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            if (string.IsNullOrWhiteSpace(dlg.TextValue))
            {
                _texts.RemoveAt(index);
            }
            else
            {
                item.Text     = dlg.TextValue;
                item.FontSize = dlg.FontSize;
            }

            _canvas.Invalidate();
        }

        /// <summary>Devuelve el índice del texto bajo el punto de pantalla dado, o -1.</summary>
        private int FindTextAt(int screenX, int screenY)
        {
            using var g = _canvas.CreateGraphics();

            for (int i = _texts.Count - 1; i >= 0; i--)
            {
                var item   = _texts[i];
                using var font = ScaledFont(item.FontSize);
                var center = ToScreenPoint(item.X, item.Y);
                var size   = g.MeasureString(item.Text, font);
                var rect   = new RectangleF(
                    center.X - size.Width / 2f - 3, center.Y - size.Height / 2f - 2,
                    size.Width + 6, size.Height + 4);

                if (rect.Contains(screenX, screenY)) return i;
            }

            return -1;
        }

        /// <summary>
        /// Devuelve el índice de la cara más pequeña que contiene el punto dado
        /// (coordenadas de pantalla), o -1 si no hay ninguna.
        /// </summary>
        private int FindFaceAt(int screenX, int screenY)
        {
            if (_scale <= 0) return -1;

            var (imgX, imgY) = ToImagePoint(screenX, screenY);

            int    best     = -1;
            double bestArea = double.MaxValue;

            for (int i = 0; i < _faces.Count; i++)
            {
                if (!PointInPolygon(_faces[i], imgX, imgY)) continue;

                double area = PolygonArea(_faces[i]);
                if (area < bestArea)
                {
                    bestArea = area;
                    best     = i;
                }
            }

            return best;
        }

        private static bool PointInPolygon(List<Point> poly, double x, double y)
        {
            bool inside = false;
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                double xi = poly[i].X, yi = poly[i].Y;
                double xj = poly[j].X, yj = poly[j].Y;

                if (yi == yj) continue;
                if ((y < yi) == (y < yj)) continue;

                double xCross = xi + (y - yi) / (yj - yi) * (xj - xi);
                if (x < xCross) inside = !inside;
            }
            return inside;
        }

        private static double PolygonArea(List<Point> poly)
        {
            double area = 0;
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += (double)poly[i].X * poly[j].Y - (double)poly[j].X * poly[i].Y;
            }
            return Math.Abs(area) / 2.0;
        }

        // ── Guardar ──────────────────────────────────────────────────────────

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title      = "Guardar SVG con habitaciones",
                Filter     = "SVG (*.svg)|*.svg|Todos los archivos|*.*",
                DefaultExt = "svg",
                FileName   = "habitaciones"
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            string svg = SvgExporter.RenderPolygonsSvg(_faces, _imgWidth, _imgHeight, _roomNames, _texts);
            File.WriteAllText(dlg.FileName, svg, System.Text.Encoding.UTF8);

            _lblStatus.Text = $"Guardado: {Path.GetFileName(dlg.FileName)}";
        }
    }
}
