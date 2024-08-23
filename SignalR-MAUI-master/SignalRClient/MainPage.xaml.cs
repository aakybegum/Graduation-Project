using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Media;
using Microsoft.Maui.ApplicationModel;

using static SignalRClient.App;
using Microsoft.AspNetCore.SignalR;

namespace SignalRClient
{
    public partial class MainPage : ContentPage
    {
        private readonly HubConnection hubConnection;
        private CancellationTokenSource cancellationTokenSource;
        private bool isListeningForCommands = false;
        private string phoneNumber;

        public MainPage()
        {
            InitializeComponent();
            phoneNumber = Global.PhoneNumber;
            ShowPhoneNumber();
            string baseUrl = DeviceInfo.Current.Platform == DevicePlatform.Android ? "http://10.0.2.2" : "http://localhost";
            hubConnection = new HubConnectionBuilder()
                .WithUrl($"https://signalrserver20240609015647.azurewebsites.net/chatHub", options =>
                {
                    options.Headers.Add("PhoneNumber", phoneNumber); // Telefon numarasını başlık olarak ekle
                })
                .Build();

            hubConnection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await ConnectToHubAsync();
            };

            hubConnection.On<string, string>("ReceiveMessage", async (user, message) =>
            {
                await this.Dispatcher.DispatchAsync(() =>
                {
                    AddMessageToChat(user, message);
                });
                if (user != txtUsername.Text)
                {
                    var locales = await TextToSpeech.GetLocalesAsync();
                    var locale = locales.FirstOrDefault(l => l.Language == "tr" && l.Country == "TR");

                    if (locale != null)
                    {
                        await TextToSpeech.SpeakAsync(user + " diyorki " + message, new SpeechOptions { Locale = locale });
                    }
                    else
                    {
                        await TextToSpeech.SpeakAsync(user + " diyorki " + message);
                    }
                }
            });

            _ = ConnectToHubAsync();
            cancellationTokenSource = new CancellationTokenSource();
        }

        private void ShowPhoneNumber()
        {
            this.Dispatcher.Dispatch(() =>
            {
                lblPhoneNumber.Text = $"Your Phone Number: {phoneNumber}";
            });
        }

        private async Task ConnectToHubAsync()
        {
            try
            {
                await hubConnection.StartAsync();
                UpdateStatusLabel("Connected to hub.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to Hub: {ex.Message}");
                UpdateStatusLabel("Error connecting to hub.");
            }
        }

        private async Task StartListening()
        {
            var isGranted = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (isGranted != PermissionStatus.Granted)
            {
                isGranted = await Permissions.RequestAsync<Permissions.Microphone>();
            }

            if (isGranted == PermissionStatus.Granted)
            {
                var cultureInfo = new CultureInfo("tr-TR");
                cancellationTokenSource?.Cancel();
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
                        UpdateUI("Tekrar dene.");
                    }
                }
                catch (OperationCanceledException)
                {
                    UpdateUI("Ses tanıma iptal edildi.");
                }
                catch (Exception ex)
                {
                    UpdateUI($"Hata: {ex.Message}");
                }
            }
            else
            {
                UpdateUI("Mikrofon izni verilmedi.");
            }
        }

        private async Task ProcessVoiceCommands(string voiceInput)
        {
            if (voiceInput.Equals("herkese gönder", StringComparison.OrdinalIgnoreCase))
            {
                btnSend_Clicked(this, EventArgs.Empty);
            }
            else if (voiceInput.Equals("gönder", StringComparison.OrdinalIgnoreCase))
            {
                btnSendToUser_Clicked(this, EventArgs.Empty);
            }
            else if (voiceInput.Equals("kayıt", StringComparison.OrdinalIgnoreCase))
            {
                btnRecord_Clicked(this, EventArgs.Empty);
                txtMessage.Text = string.Empty;
                txtMessage.Focus();
				
			}
            else if (voiceInput.Contains("numara", StringComparison.OrdinalIgnoreCase))
            {
                // Numara girişi için bir Entry alanına odaklan
                txtRecipientPhoneNumber.Text = string.Empty;
                txtRecipientPhoneNumber.Focus();
            }
            else if (txtRecipientPhoneNumber.IsFocused && voiceInput.Replace(" ", "").All(char.IsDigit))
            {
                // Numara girildiğinde boşlukları kaldır
                var phoneNumber = voiceInput.Replace(" ", "");
                txtRecipientPhoneNumber.Text = phoneNumber;

                var locales = await TextToSpeech.GetLocalesAsync();
                var locale = locales.FirstOrDefault(l => l.Language == "tr" && l.Country == "TR");

                if (locale != null)
                {
                    await TextToSpeech.SpeakAsync($"Telefon numarası {phoneNumber} olarak girildi.", new SpeechOptions { Locale = locale });
                }
            }
            else if (voiceInput.Contains("ismim", StringComparison.OrdinalIgnoreCase))
            {
                //İsim girişi için bir Entry alanına odaklan
                txtUsername.Text = string.Empty;
                txtUsername.Focus();
            }
            else if (txtUsername.IsFocused && !voiceInput.Any(char.IsDigit))
            {
                // İsim girildiğinde
                txtUsername.Text = voiceInput;

                var locales = await TextToSpeech.GetLocalesAsync();
                var locale = locales.FirstOrDefault(l => l.Language == "tr" && l.Country == "TR");

                if (locale != null)
                {
                    await TextToSpeech.SpeakAsync($"İsim {voiceInput} olarak girildi.", new SpeechOptions { Locale = locale });
                }
            }

			else if (txtMessage.IsFocused)
			{
				txtMessage.Text = voiceInput;
				var locales = await TextToSpeech.GetLocalesAsync();
				var locale = locales.FirstOrDefault(l => l.Language == "tr" && l.Country == "TR");

				if (locale != null)
				{
					await TextToSpeech.SpeakAsync($"Mesaj {voiceInput} olarak yazıldı.", new SpeechOptions { Locale = locale });
				}
			}
		}

        private void UpdateStatusLabel(string message)
        {
            this.Dispatcher.DispatchAsync(() =>
            {
                statusLabel.Text = message;
            });
        }

        private async void btnSend_Clicked(object sender, EventArgs e)
        {
            await SendAndSpeakAsync();
        }

        private async Task SendAndSpeakAsync()
        {
            try
            {
                if (hubConnection.State == HubConnectionState.Connected)
                {
                    await hubConnection.InvokeAsync("SendMessageToAll", txtUsername.Text, txtMessage.Text);

                    // ****AddMessageToChat metodunu burada çağırma******
                    AddMessageToChat(txtUsername.Text, txtMessage.Text);

                    var locales = await TextToSpeech.GetLocalesAsync();
                    var locale = locales.FirstOrDefault(l => l.Language == "tr" && l.Country == "TR");

                    if (locale != null)
                    {
                        await TextToSpeech.SpeakAsync(txtMessage.Text, new SpeechOptions { Locale = locale });
                    }
                    else
                    {
                        await TextToSpeech.SpeakAsync(txtMessage.Text);
                    }

                    txtMessage.Text = string.Empty;
                }
                else
                {
                    UpdateStatusLabel("Not connected to hub. Please try again.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        private async void btnSendToUser_Clicked(object sender, EventArgs e)
        {
            var recipientPhoneNumber = txtRecipientPhoneNumber.Text;
            var message = txtMessage.Text;

            if (!string.IsNullOrEmpty(recipientPhoneNumber) && !string.IsNullOrEmpty(message))
            {
                if (hubConnection.State == HubConnectionState.Connected)
                {
                    await hubConnection.InvokeAsync("SendMessageToUserByPhoneNumber", recipientPhoneNumber, txtUsername.Text, message);
                    txtMessage.Text = string.Empty;
					AddMessageToChat(txtUsername.Text, message);
				}
                else
                {
                    UpdateStatusLabel("Not connected to hub. Please try again.");
                }
            }

        }

        private async void btnRecord_Clicked(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            var cultureInfo = new CultureInfo("tr-TR");
            var cancellationToken = cancellationTokenSource.Token;

            try
            {
                var recognitionResult = await SpeechToText.Default.ListenAsync(
                    cultureInfo,
                    new Progress<string>(partialText => UpdateUI(partialText)),
                    cancellationToken);

                if (recognitionResult != null && recognitionResult.IsSuccessful)
                {
                    var message = recognitionResult.Text;
                    transcriptionLabel.Text = message;

                    if (hubConnection.State == HubConnectionState.Connected)
                    {
                        string recipientPhoneNumber = txtRecipientPhoneNumber.Text; // Alıcı telefon numarası
                        string senderUsername = txtUsername.Text; // Gönderenin kullanıcı adı
                        

                        await hubConnection.InvokeAsync("SendMessageToUserByPhoneNumber", recipientPhoneNumber, senderUsername, message);
                        AddMessageToChat(senderUsername, message);

						txtMessage.Text = string.Empty; //Mesaj yazdırma kısmı
                        txtMessage.Focus();

					}
                }
                else
                {
                    UpdateUI("Tekrar dene.");
                }
            }
            catch (HubException hubEx)
            {
                UpdateUI($"Hub Hatası: {hubEx.Message}");
            }
            catch (Exception ex)
            {
                UpdateUI($"Hata: {ex.Message}");
            }
        }

        private void UpdateUI(string message)
        {
            this.Dispatcher.DispatchAsync(() =>
            {
                transcriptionLabel.Text = message;
            });
        }

        private async void btnListenCommands_Clicked(object sender, EventArgs e)
        {
            if (!isListeningForCommands)
            {
                isListeningForCommands = true;
                UpdateStatusLabel("Sesli komutlar için dinleniyor...");
                await StartListeningForCommands();
            }
            else
            {
                isListeningForCommands = false;
                UpdateStatusLabel("Sesli komut dinleme durduruldu.");
                cancellationTokenSource?.Cancel();
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
                        UpdateUI("Ses tanıma başarısız, tekrar deneyin.");
                    }
                }
                catch (OperationCanceledException)
                {
                    UpdateUI("Ses tanıma iptal edildi.");
                }
                catch (Exception ex)
                {
                    UpdateUI($"Ses tanıma sırasında hata oluştu: {ex.Message}");
                }
                isListeningForCommands = false;
            }
            else
            {
                UpdateUI("Mikrofon izni verilmedi.");
            }
        }

        private void AddMessageToChat(string user, string message)
        {
            this.Dispatcher.Dispatch(() =>
            {
                var messageFrame = new Frame
                {
                    BackgroundColor = user == txtUsername.Text ? Color.FromArgb("#DCF8C6") : Color.FromArgb("#FFFFFF"),
                    CornerRadius = 10,
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 5),
                    HorizontalOptions = user == txtUsername.Text ? LayoutOptions.End : LayoutOptions.Start
                };

                var messageLabel = new Label
                {
                    Text = $"{user}: {message}",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#333")
                };

                messageFrame.Content = messageLabel;
                lblChat.Children.Add(messageFrame);
            });
        }
    }
}
