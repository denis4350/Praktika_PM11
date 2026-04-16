using System.Windows;

namespace HranitelPro
{
    public partial class ChoiceWindow : Window
    {
        public string VisitType { get; private set; } = string.Empty;
        public int CurrentUserId { get; set; }

        public ChoiceWindow()
        {
            InitializeComponent();
        }

        private void PersonalVisit_Click(object sender, RoutedEventArgs e)
        {
            VisitType = "personal";
            this.DialogResult = true;
            this.Close();
        }

        private void GroupVisit_Click(object sender, RoutedEventArgs e)
        {
            VisitType = "group";
            this.DialogResult = true;
            this.Close();
        }
    }
}