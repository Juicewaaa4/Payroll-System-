using System.Windows.Controls;

namespace PayrollSystem.Views
{
    public partial class PayrollProcessingView : UserControl
    {
        public PayrollProcessingView()
        {
            InitializeComponent();
        }

        private void NumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.TextChanged -= NumberTextBox_TextChanged;

                string raw = textBox.Text.Replace(",", "");
                if (decimal.TryParse(raw, out decimal result))
                {
                    int caretIndex = textBox.CaretIndex;
                    int lengthBefore = textBox.Text.Length;

                    if (!raw.Contains("."))
                    {
                        textBox.Text = string.Format("{0:N0}", result);
                        int diff = textBox.Text.Length - lengthBefore;
                        textBox.CaretIndex = System.Math.Max(0, caretIndex + diff);
                    }
                }

                textBox.TextChanged += NumberTextBox_TextChanged;
            }
        }
    }
}
