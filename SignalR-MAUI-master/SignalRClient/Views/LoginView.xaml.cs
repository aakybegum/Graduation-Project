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
    private bool awaitingConfirmation = false; // Numara onay� bekleniyor mu?
    private string phoneNumber; // Ge�ici telefon numaras� depolama

    public LoginView()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Uygulaman�n ilk a��ld���nda bir ses y�nergesi �al
        var locales = await TextToSpeech.GetLocalesAsync();
        var locale = locales.FirstOrDefault(l => l.Language == "tr" && l.Country == "TR");
        if (locale != null)
        {
            await TextToSpeech.SpeakAsync("Merhaba de�erli kullan�c�m�z, sesli y�netimi aktifle�tirmek i�in l�tfen telefonun �st k�sm�na bir kez t�klay�n. Telefon numaran�z� girmek i�in 'numaram' diyin ve telefonunuza gelen onay kodunu girin.", new SpeechOptions { Locale = locale });
        }
        else
        {
            await TextToSpeech.SpeakAsync("Merhaba de�erli kullan�c�m�z, sesli y�netimi aktifle�tirmek i�in l�tfen telefonun �st k�sm�na bir kez t�klay�n. Telefon numaran�z� girmek i�in 'numaram' diyin ve telefonunuza gelen onay kodunu girin.");
        }
    }

    public async void LoginButton_Clicked(object sender, EventArgs e)
    {
        // Onay kodu do�rulama ve giri� i�lemi
        string verificationCode = EntryVerificationCode.Text;

        if (string.IsNullOrEmpty(verificationCode))
        {
            await DisplayAlert("Hata", "L�tfen onay kodunu giriniz.", "Tamam");
            return;
        }

        // Onay kodunu do�rulama i�lemini buraya ekleyin
        // E�er onay kodu do�ruysa giri� i�lemini ger�ekle�tirin
        bool isCodeValid = true; // Bu do�rulamay� ger�ek kodla de�i�tirin

        if (isCodeValid)
        {
            // Giri� i�lemi ba�ar�l�
            await DisplayAlert("Giri� Ba�ar�l�", "Ho�geldiniz!", "Tamam");


            /**************************/

            Global.PhoneNumber = EntryPhoneNumber.Text;
            PhoneNumberLabel.Text = EntryPhoneNumber.Text;

            /****************************/

            // MainPage'e y�nlendirme i�lemi
            await Navigation.PushAsync(new MainPage());
        }
        else
        {
            // Giri� i�lemi ba�ar�s�z
            await DisplayAlert("Giri� Ba�ar�s�z", "Onay kodu yanl��. L�tfen tekrar deneyin.", "Tamam");
        }

    }

    private void SendCodeButton_Clicked(object sender, EventArgs e)
    {
        // Kod g�nderme i�lemi
        string phoneNumber = EntryPhoneNumber.Text.Replace(" ", "");

        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length != 11)
        {
            DisplayAlert("Hata", "L�tfen 11 haneli bir telefon numaras� giriniz.", "Tamam");
            return;
        }

        
        DisplayAlert("Kod G�nderildi", "L�tfen telefonunuza g�nderilen onay kodunu giriniz.", "Tamam");

        // Onay kodu giri�ini g�r�n�r yap
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
        // Giri� yap komutu
        if (voiceInput.Equals("Giri� Yap", StringComparison.OrdinalIgnoreCase))
        {
            LoginButton_Clicked(this, EventArgs.Empty);
        }
        // Kay�t ol komutu
        else if (voiceInput.Equals("kay�t", StringComparison.OrdinalIgnoreCase))
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
        // Numara adresi giri�i
        else if (voiceInput.Contains("numaram", StringComparison.OrdinalIgnoreCase))
        {
            // Numara giri�i i�in bir Entry alan�na odaklan
            EntryPhoneNumber.Text = string.Empty;
            EntryPhoneNumber.Focus();
        }
        else if (voiceInput.Replace(" ", "").All(char.IsDigit))
        {
            if (EntryPhoneNumber.IsFocused)
            {
                EntryPhoneNumber.Text += voiceInput.Replace(" ", "");
                phoneNumber = EntryPhoneNumber.Text;

                // Numara do�rulama
                var locales = await TextToSpeech.GetLocalesAsync();
                var locale = locales.FirstOrDefault(l => l.Language == "tr" && l.Country == "TR");
                var message = $"Numaran�z {phoneNumber}. Onayl�yorsan�z 'evet' deyiniz. Numara yanl�� ise telefonu �st k�sm�na t�klay�p 'tekrar dene' deyiniz.";
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
            // Di�er komutlar ve i�lemler
            UpdateUI(voiceInput); // Ekranda g�sterim i�in
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
                    UpdateUI("Ses tan�ma ba�ar�s�z, tekrar deneyin.");
                }
            }
            catch (OperationCanceledException)
            {
                UpdateUI("Ses tan�ma iptal edildi.");
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
                // Kullan�c� numaray� onaylamaz ve "tekrar dene" derse numaray� temizleyip yeniden ba�lat
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