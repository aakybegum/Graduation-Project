using SignalRClient.Views;

namespace SignalRClient
{
    public partial class App : Application
    {
        
        
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new LoginView());


        }
        public static class Global
        {
            public static string PhoneNumber { get; set; }
        }
    }
}