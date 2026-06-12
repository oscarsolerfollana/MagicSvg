namespace MagicSvg
{
    /// <summary>
    /// Pequeño diálogo modal para introducir el nombre de una habitación.
    /// </summary>
    public class RoomNameDialog : Form
    {
        private readonly TextBox _txtName;

        public string RoomName => _txtName.Text.Trim();

        public RoomNameDialog(string title, string initialValue, string labelText = "Nombre de la habitación:")
        {
            Text            = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MinimizeBox     = false;
            MaximizeBox     = false;
            ShowInTaskbar   = false;
            ClientSize      = new System.Drawing.Size(320, 110);

            var lbl = new Label
            {
                Text      = labelText,
                AutoSize  = false,
                Location  = new System.Drawing.Point(12, 12),
                Size      = new System.Drawing.Size(296, 20)
            };

            _txtName = new TextBox
            {
                Text     = initialValue,
                Location = new System.Drawing.Point(12, 36),
                Size     = new System.Drawing.Size(296, 24)
            };

            var btnOk = new Button
            {
                Text         = "Aceptar",
                DialogResult = DialogResult.OK,
                Location     = new System.Drawing.Point(132, 70),
                Size         = new System.Drawing.Size(85, 28)
            };

            var btnCancel = new Button
            {
                Text         = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location     = new System.Drawing.Point(223, 70),
                Size         = new System.Drawing.Size(85, 28)
            };

            Controls.Add(lbl);
            Controls.Add(_txtName);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            _txtName.SelectAll();
        }
    }
}
