using CommunityToolkit.Maui.Media;
using System.Globalization;
using System.Threading;
using Microsoft.Maui.Media;
using System.Security.Cryptography.X509Certificates;
using static SignalRClient.App;


namespace SignalRClient.Views;

public partial class LoginView : ContentPage
{
    private CancellationTokenSource cancellationTokenSource;
    private bool isListeningForCommands = false;
    private bool awaitingConfirmation = false; // Numara onayý bekleniyor mu?
    private string phoneNumber; // Geçici telefon numarasý depolama

    public LoginView()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Uygulamanýn ilk açýldýðýnda bir ses yönergesi çal
        var locales = await TextToSpeech.GetLocalesAsync();
        var locale = locales.FirstOrDefault(l => l.Language == "tr" && l.Country == "TR");
        if (locale != null)
        {
            await TextToSpeech.SpeakAsync("Merhaba deðerli kullanýcýmýz, sesli yönetimi aktifleþtirmek için lütfen telefonun üst kýsmýna bir kez týklayýn. Telefon numaranýzý girmek için 'numaram' diyin ve telefonunuza gelen onay kodunu girin.", new SpeechOptions { Locale = locale });
        }
        else
        {
            await TextToSpeech.SpeakAsync("Merhaba deðerli kullanýcýmýz, sesli yönetimi aktifleþtirmek için lütfen telefonun üst kýsmýna bir kez týklayýn. Telefon numaranýzý girmek için 'numaram' diyin ve telefonunuza gelen onay kodunu girin.");
        }
    }

    public async void LoginButton_Clicked(object sender, EventArgs e)
    {
        // Onay kodu doðrulama ve giriþ iþlemi
        string verificationCode = EntryVerificationCode.Text;

        if (string.IsNullOrEmpty(verificationCode))
        {
            await DisplayAlert("Hata", "Lütfen onay kodunu giriniz.", "Tamam");
            return;
        }

        // Onay kodunu doðrulama iþlemini buraya ekleyin
        // Eðer onay kodu doðruysa giriþ iþlemini gerçekleþtirin
        bool isCodeValid = true; // Bu doðrulamayý gerçek kodla deðiþtirin

        if (isCodeValid)
        {
            // Giriþ iþlemi baþarýlý
            await DisplayAlert("Giriþ Baþarýlý", "Hoþgeldiniz!", "Tamam");


            /**************************/

            Global.PhoneNumber = EntryPhoneNumber.Text;
            PhoneNumberLabel.Text = EntryPhoneNumber.Text;

            /****************************/

            // MainPage'e yönlendirme iþlemi
            await Navigation.PushAsync(new MainPage());
        }
        else
        {
            // Giriþ iþlemi baþarýsýz
            await DisplayAlert("Giriþ Baþarýsýz", "Onay kodu yanlýþ. Lütfen tekrar deneyin.", "Tamam");
        }

    }

    private void SendCodeButton_Clicked(object sender, EventArgs e)
    {
        // Kod gönderme iþlemi
        string phoneNumber = EntryPhoneNumber.Text.Replace(" ", "");

        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length != 11)
        {
            DisplayAlert("Hata", "Lütfen 11 haneli bir telefon numarasý giriniz.", "Tamam");
            return;
        }

        
        DisplayAlert("Kod Gönderildi", "Lütfen telefonunuza gönderilen onay kodunu giriniz.", "Tamam");

        // Onay kodu giriþini görünür yap
        VerificationCodeRoundRectangle.IsVisible = true;
        VerificationCodeFrame.IsVisible = true;
        EntryVerificationCode.IsVisible = true;
        LoginButton.IsEnabled = true;
    }

    public async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterView());
    }

    private async Task ProcessVoiceCommands(string voiceInput)
    {
        // Giriþ yap komutu
        if (voiceInput.Equals("Giriþ Yap", StringComparison.OrdinalIgnoreCase))
        {
            LoginButton_Clicked(this, EventArgs.Empty);
        }
        // Kayýt ol komutu
        else if (voiceInput.Equals("kayýt", StringComparison.OrdinalIgnoreCase))
        {
            RegisterButton_Clicked(this, EventArgs.Empty);
        }
        // Evet komutu
        else if (awaitingConfirmation && voiceInput.Equals("evet", StringComparison.OrdinalIgnoreCase))
        {
            awaitingConfirmation = false;
            EntryPhoneNumber.Text = phoneNumber;
            SendCodeButton_Clicked(this, EventArgs.Empty);
        }
        // Tekrar dene komutu
        else if (awaitingConfirmation && voiceInput.Equals("tekrar dene", StringComparison.OrdinalIgnoreCase))
        {
            awaitingConfirmation = false;
            EntryPhoneNumber.Text = string.Empty;
            await StartListeningForCommands();
        }
        // Numara adresi giriþi
        else if (voiceInput.Contains("numaram", StringComparison.OrdinalIgnoreCase))
        {
            // Numara giriþi için bir Entry alanýna odaklan
            EntryPhoneNumber.Text = string.Empty;
            EntryPhoneNumber.Focus();
        }
        else if (voiceInput.Replace(" ", "").All(char.IsDigit))
        {
            if (EntryPhoneNumber.IsFocused)
            {
                EntryPhoneNumber.Text += voiceInput.Replace(" ", "");
                phoneNumber = EntryPhoneNumber.Text;

                // Numara doðrulama
                var locales = await TextToSpeech.GetLocalesAsync();
                var locale = locales.FirstOrDefault(l => l.Language == "tr" && l.Country == "TR");
                var message = $"Numaranýz {phoneNumber}. Onaylýyorsanýz 'evet' deyiniz. Numara yanlýþ ise telefonu üst kýsmýna týklayýp 'tekrar dene' deyiniz.";
                if (locale != null)
                {
                    await TextToSpeech.SpeakAsync(message, new SpeechOptions { Locale = locale });
                }
                else
                {
                    await TextToSpeech.SpeakAsync(message);
                }

                awaitingConfirmation = true;
            }
        }
        else
        {
            // Diðer komutlar ve iþlemler
            UpdateUI(voiceInput); // Ekranda gösterim için
        }
    }

    private async Task StartListeningForCommands()
    {
        var isGranted = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (isGranted != PermissionStatus.Granted)
        {
            isGranted = await Permissions.RequestAsync<Permissions.Microphone>();
        }

        if (isGranted == PermissionStatus.Granted)
        {
            var cultureInfo = new CultureInfo("tr-TR");
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            try
            {
                var recognitionResult = await SpeechToText.Default.ListenAsync(
                    cultureInfo,
                    new Progress<string>(partialText => UpdateUI(partialText)),
                    cancellationToken);

                if (recognitionResult != null && recognitionResult.IsSuccessful)
                {
                    await ProcessVoiceCommands(recognitionResult.Text);
                }
                else
                {
                    UpdateUI("Ses tanýma baþarýsýz, tekrar deneyin.");
                }
            }
            catch (OperationCanceledException)
            {
                UpdateUI("Ses tanýma iptal edildi.");
            }
            isListeningForCommands = false;
        }
        else
        {
            UpdateUI("Mikrofon izni verilmedi.");
        }
    }

    private async void btnListenCommands_Clicked(object sender, EventArgs e)
    {
        if (!isListeningForCommands)
        {
            isListeningForCommands = true;
            await StartListeningForCommands();
        }
        else
        {
            if (awaitingConfirmation)
            {
                // Kullanýcý numarayý onaylamaz ve "tekrar dene" derse numarayý temizleyip yeniden baþlat
                EntryPhoneNumber.Text = string.Empty;
                awaitingConfirmation = false;
                await StartListeningForCommands();
            }
            isListeningForCommands = false;
            cancellationTokenSource?.Cancel();
        }
    }

    private void UpdateUI(string message)
    {
        this.Dispatcher.DispatchAsync(() =>
        {
            if (awaitingConfirmation)
            {
                if (message.Equals("evet", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessVoiceCommands("evet");
                }
                else if (message.Equals("tekrar dene", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessVoiceCommands("tekrar dene");
                }
            }
            else
            {
                transcriptionLabel.Text = message;
            }
        });
    }
}