using System.Windows;

namespace HranitelPro
{
    public partial class VisitorDialog : Window
    {
        public Visitor Visitor { get; private set; } = new Visitor(); // ← Инициализация по умолчанию

        public VisitorDialog()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Visitor = new Visitor
            {
                LastName = LastNameBox.Text,
                FirstName = FirstNameBox.Text,
                MiddleName = MiddleNameBox.Text,
                Phone = PhoneBox.Text,
                Email = EmailBox.Text,
                PassportSeries = PassportSeriesBox.Text,
                PassportNumber = PassportNumberBox.Text
            };

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}