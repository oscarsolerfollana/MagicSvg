namespace MagicSvg
{
    /// <summary>
    /// Diálogo modal para introducir/editar un texto libre y su tamaño de fuente.
    /// </summary>
    public class TextItemDialog : Form
    {
        private readonly TextBox _txtText;
        private readonly NumericUpDown _numSize;

        public string TextValue => _txtText.Text.Trim();
        public double FontSize  => (double)_numSize.Value;

        public TextItemDialog(string title, string initialText, double initialSize)
        {
            Text            = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MinimizeBox     = false;
            MaximizeBox     = false;
            ShowInTaskbar   = false;
            ClientSize      = new System.Drawing.Size(320, 150);

            var lblText = new Label
            {
                Text      = "Texto:",
                AutoSize  = false,
                Location  = new System.Drawing.Point(12, 12),
                Size      = new System.Drawing.Size(296, 20)
            };

            _txtText = new TextBox
            {
                Text     = initialText,
                Location = new System.Drawing.Point(12, 34),
                Size     = new System.Drawing.Size(296, 24)
            };

            var lblSize = new Label
            {
                Text      = "Tamaño de fuente (px):",
                AutoSize  = false,
                Location  = new System.Drawing.Point(12, 68),
                Size      = new System.Drawing.Size(180, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _numSize = new NumericUpDown
            {
                Minimum  = 6,
                Maximum  = 200,
                Value    = (decimal)Math.Clamp(initialSize, 6, 200),
                Location = new System.Drawing.Point(198, 66),
                Size     = new System.Drawing.Size(110, 24)
            };

            var btnOk = new Button
            {
                Text         = "Aceptar",
                DialogResult = DialogResult.OK,
                Location     = new System.Drawing.Point(132, 110),
                Size         = new System.Drawing.Size(85, 28)
            };

            var btnCancel = new Button
            {
                Text         = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location     = new System.Drawing.Point(223, 110),
                Size         = new System.Drawing.Size(85, 28)
            };

            Controls.Add(lblText);
            Controls.Add(_txtText);
            Controls.Add(lblSize);
            Controls.Add(_numSize);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            _txtText.SelectAll();
        }
    }
}
