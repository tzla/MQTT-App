using MQTTnet.Client; //MQTT messaging libraries
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.ManagedClient;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System; //windows libraries
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks; //system libraries
using Windows.Devices.Gpio; //Pin Out Library
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;
using Windows.Foundation;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Core;//UI libraries
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.System.Threading;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.System.Profile;
using Windows.UI.Popups;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace MQTTnet.TestApp.UniversalWindows //main program
{
    public sealed partial class MainPage
    {
        EasClientDeviceInformation CurrentDeviceInfor = new EasClientDeviceInformation();

        bool startcheck, backcheck = false;
        bool rescheck, backres = false;
        bool stopcheck, stopback = false;
        bool hasStarted = false;

        private readonly ConcurrentQueue<MqttNetLogMessage> _traceMessages =
            new ConcurrentQueue<MqttNetLogMessage>();//initiate mqtt
        private IMqttClient _mqttClient; //mqtt client-retrieves messages
        private IMqttServer _mqttServer; //mqtt broker-distributes messages

        private const int LED_PIN = 17; //Control Pin Out 17
        private GpioPin pin; //Pin for pinout
        private GpioPinValue pinValue; //high or low for digital control

        bool connacking = false;
        bool first = false;

        public WiFiAdapter firstAdapter;
        public ConnectionProfile connectionProfile = NetworkInformation.GetInternetConnectionProfile();
        public WiFiNetworkReport report;
        public String titsy;

        int x = 0;
        int ccc = 0;
        int trips = 0;
        int tripper = 0; //counters

        bool[] ResetArray = new bool[56]; //for resetting exactly once
        bool[] TurnoffArray = new bool[56]; //sensor on or off
        bool[] TriggerArray = new bool[56]; //which sensor triggered
        bool Disabled = false; //disables the program output

        double[] RawData = new double[56]; //latest updated readings
        double[,] BigData = new double[10, 56]; //last 10 readings
        double[] AvgData = new double[56]; //average of last 10
        double[] OffData = new double[56]; //difference of latest from average
        double[] PerData = new double[56]; //percent difference
        double[] SumData = new double[56];//sum of last 10

        Ellipse[] VirtualLEDS = new Ellipse[56]; //indicator 'lights'
        TextBlock[] labels = new TextBlock[56];
        TextBlock[] rawss = new TextBlock[56];
        
        TextBlock[] pers = new TextBlock[56];//data boxes

        String TriggerList1 = "";
        String TriggerList2 = "";
        String TriggerList3 = "";
        String TriggerList4 = "";

        TextBlock TriggerBox1 = new TextBlock();
        TextBlock TriggerBox2 = new TextBlock();
        TextBlock TriggerBox3 = new TextBlock();
        TextBlock TriggerBox4 = new TextBlock();
        
        bool SweatySack = false;
        bool Sacked = false;
        bool[] Triggering = new bool[56];


        //sets the colors for the indicators
        //green higher, red lower, gray the same, yellow-0/null reading present
        //black is disabled, gradient for triggered sensor  
        SolidColorBrush Greeners
            = new SolidColorBrush(Windows.UI.Colors.LimeGreen);
        SolidColorBrush Reders
           = new SolidColorBrush(Windows.UI.Colors.Red);
        SolidColorBrush Bluers
           = new SolidColorBrush(Windows.UI.Colors.Blue);
        SolidColorBrush Grays
            = new SolidColorBrush(Windows.UI.Colors.LightGray);
        SolidColorBrush Yellows
            = new SolidColorBrush(Windows.UI.Colors.Yellow);
        SolidColorBrush BlakCannon
            = new SolidColorBrush(Windows.UI.Colors.Black);
        LinearGradientBrush FlakCannon =
             new LinearGradientBrush();
        //these select the LED color intensity based on reading
        SolidColorBrush[] Reds = new SolidColorBrush[56];
        SolidColorBrush[] Greens = new SolidColorBrush[56];
        SolidColorBrush[] Blues = new SolidColorBrush[56];
        SolidColorBrush clearcoat = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
        SolidColorBrush clearercoat = new SolidColorBrush(Windows.UI.Color.FromArgb(25, 0, 0, 0));

        ImageBrush startedd = new ImageBrush();
        ImageBrush resumer = new ImageBrush();
        ImageBrush disabler = new ImageBrush();

        double mod;// = 1.4;//1.4; //8
        double trigger;// = 2.5;//2.5; //10
        string machiner;// = "SensorSh";//"SensorSh"; //Proto
        public MainPage()
        {
            machiner = CurrentDeviceInfor.FriendlyName;

            if (machiner == "Proto")
            {
                mod = 8;
                trigger = 10;
            }
            else
            {
                mod = 1.4;
                trigger = 2.5;
            }
            
            InitializeComponent();//Initializes the GUI for added objects
            InitGPIO();//Initializes the GPIO settings
            Gradienter();//Initializes the gradient brush
            try { Connack(); } catch { }
           
            ThisMach.Text = machiner;
            startedd.ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/StartedButton.png"));
            resumer.ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/ResumeButton.png"));
            disabler.ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/PauseButton.png"));

            for (int f = 0; f < 56; f++)
            {
                Windows.Foundation.Point assss = new Point(0, 0);
                TurnoffArray[f] = false;
                ResetArray[f] = false;
                VirtualLEDS[f] = new Ellipse();
                TriggerArray[f] = false;
                labels[f] = new TextBlock();
                rawss[f] = new TextBlock();
                pers[f] = new TextBlock();

                Reds[f] = new SolidColorBrush(Windows.UI.Colors.Red);
                Blues[f] = new SolidColorBrush(Windows.UI.Colors.Blue);
                Greens[f] = new SolidColorBrush(Windows.UI.Colors.Green);

                labels[f].Text = (f + 1).ToString();
                Triggering[f] = false;
                rawss[f].Text = "R" + (f + 1).ToString();
                
                //babys[f].Text = "B" + (f + 1).ToString();
                //avgs[f].Text = "A" + (f + 1).ToString();
                //offs[f].Text = "O" + (f + 1).ToString();
                pers[f].Text = "P" + (f + 1).ToString();

                //lasts[f].Margin = = lasts[f].Width= lasts[f].FontSize
                // diffs[f].Margin =diffs[f].Width =diffs[f].FontSize =
                labels[f].Margin = rawss[f].Margin 
                = pers[f].Margin = new Thickness(0);

                labels[f].Width = rawss[f].Width = pers[f].Width = 28;


                rawss[f].FontSize = 14;
                pers[f].FontSize = 14;
                

                VirtualLEDS[f].Width = 20;
                VirtualLEDS[f].Height = 28;
                VirtualLEDS[f].Margin = new Thickness(4);
                //Thickness leftfield = new Thickness();
                //leftfield.Left = 5;
               // VirtualLEDS[0].Margin = leftfield;
                VirtualLEDS[f].Fill = clearercoat;
                


                if (f < 28)
                {
                    LEDS.Children.Add(VirtualLEDS[f]);
                    midge.Children.Add(labels[f]);
                    lblrow.Children.Add(rawss[f]);
                    
                    
                    perrow.Children.Add(pers[f]);
                    
                }
                if (f > 27)
                {
                    //rawss[f].Text = "0";
                    lblrowB.Children.Add(rawss[f]);
                    perrowB.Children.Add(pers[f]);
                    LEDZ.Children.Add(VirtualLEDS[f]);
                    numrowBot.Children.Add(labels[f]);
                }
                LEDZ.Height = LEDS.Height = 45;
                panssy.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 0, 255, 0));
                }
            //LEDS.Height = 80;
            Rings.IsActive = false;
            // When you create a XAML element in code, you have to add
            // it to the XAML visual tree. This example assumes you have
            // a panel named 'layoutRoot' in your XAML file, like this:
            // <Grid x:Name="layoutRoot>

            // MqttNetGlobalLogger.LogMessagePublished += OnTraceMessagePublished;
        }
        private void Gradienter()
        {
            FlakCannon.StartPoint = new Point(0, 0);
            FlakCannon.EndPoint = new Point(1, 1);
            GradientStop gs1 = new GradientStop();
            GradientStop gs2 = new GradientStop();
            gs1.Color = Windows.UI.Colors.IndianRed;
            gs2.Color = Windows.UI.Colors.Red;
            gs1.Offset = 0.2;
            gs2.Offset = 1;
            FlakCannon.GradientStops.Add(gs1);
            FlakCannon.GradientStops.Add(gs2);
            
        }
        
        /// <summary>
        /// This class initiates the GPIO pin output for relay control
        /// <param name="gpio"> Parameter description for s goes here.</param>
        /// </summary>
        public async void Connack()
        {
            //raw.Text = hoe.ToString();
            //hoe++;
            if (!first)
            {
                var result = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
                firstAdapter = await WiFiAdapter.FromIdAsync(result[0].Id);
                first = true;
            }
            if (!connacking)
            {
                
                try
                {
                    connacking = true;
                    try
                    {
                        await firstAdapter.ScanAsync();
                    }
                    catch { }
                    report = firstAdapter.NetworkReport;

                    foreach (var network in report.AvailableNetworks)
                    {
                        if (network.Bssid == "00:1e:2a:0c:6a:9a")
                        {
                            WiFiReconnectionKind reKind = WiFiReconnectionKind.Automatic;
                            PasswordCredential credential = new PasswordCredential();
                            credential.Password = "drinkbeer";
                            WiFiConnectionResult results = await firstAdapter.ConnectAsync(
                                network, reKind, credential);

                        }
                    }
                }
                catch { }
                connacking = false;

                
            }
        }
        private void InitGPIO()
        {
            try
            {
                GpioController gpio = GpioController.GetDefault();//gpio object
                pin = gpio.OpenPin(LED_PIN); //sets pin
                pinValue = GpioPinValue.Low; //initially off
                pin.Write(pinValue); //writes initial value
                pin.SetDriveMode(GpioPinDriveMode.Output);//set pin for output
            }
            catch { }
        }
        private async void OnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            //var item = $"Timestamp: {DateTime.Now:O} | Topic: {eventArgs.ApplicationMessage.Topic} | Payload: {Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload)} | QoS: {eventArgs.ApplicationMessage.QualityOfServiceLevel}";
            //var doo = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);
            x++;
            
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var connector = NetworkInformation.GetInternetConnectionProfile();
                if (connector == null)
                {
                    //Connack();
                    
                    raw.Text = "fucker";
                }
                var etem = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);
                string[] atam = new string[14];
                atam = etem.Split(',');
                int indexx = 0;
                //
                //naps.Text = String.Join("", etem);
                double[] exe = new double[7];
                for (int v = 0; v < 7; v++)
                {
                    exe[v] = Convert.ToSingle(atam[13 - 2 * v]);
                }

                if (Convert.ToSingle(atam[0]) == 7)
                {
                    //diff1.FontSize = 10;
                    ccc++;
                    raw.Text = ccc.ToString();
                    ///TimerLog.Text = ccc.ToString();
                   
                }
                int me = Convert.ToInt16(atam[0]);
                indexx = me-7;
                for (int v = 0; v < 7; v++)
                {
                    RawData[v+indexx] = Convert.ToDouble(atam[13 - (v) * 2]);
                    
                    rawss[v+indexx].Text = exe[v].ToString();
                }
                
                for (int zz = indexx; zz < indexx + 7; zz++)
                {
                    //raw.Text = zz.ToString();
                    bool zeros = GetMyData(zz);
                    if (!TurnoffArray[zz] && !TriggerArray[zz])
                    {
                        if (OffData[zz] > 0)
                        {
                            if (PerData[zz] < mod)
                            {
                                byte colorer = new byte();
                                colorer = (byte)((Math.Abs(PerData[zz]) / mod * 205) + 50);
                                //naps.Text = (colorer).ToString();
                                Greens[zz].Color = Windows.UI.Color.FromArgb(colorer, 0, 255, 0);
                                VirtualLEDS[zz].Fill = Greens[zz];
                            }
                            else { VirtualLEDS[zz].Fill = Greeners; }
                            //UpdateLayout();
                        }
                        if (OffData[zz] < 0)
                        {
                            if (Math.Abs(PerData[zz]) < mod)
                            {
                                byte colorer = new byte();
                                colorer = (byte)((Math.Abs(PerData[zz]) / mod * 205) + 50);
                                //naps.Text = (colorer).ToString();
                                Reds[zz].Color = Windows.UI.Color.FromArgb(colorer, 255, 0, 0);
                                Blues[zz].Color = Windows.UI.Color.FromArgb(colorer, 0, 0, 255);
                                VirtualLEDS[zz].Fill = Blues[zz];

                            }
                            else { VirtualLEDS[zz].Fill = Bluers; }

                        }
                        if (OffData[zz] == 0) { VirtualLEDS[zz].Fill = Grays; }
                        if (zeros) { VirtualLEDS[zz].Fill = Yellows; }
                        if (PerData[zz] > trigger && !zeros && !ResetArray[zz] && !Disabled)
                        {
                            TriggerCode(zz);

                        }

                    }
                    if (TurnoffArray[zz])
                    {
                        VirtualLEDS[zz].Fill = clearcoat;
                        this.UpdateLayout();
                        
                    }
                    if (ResetArray[zz])
                    {
                        try
                        {
                            pinValue = GpioPinValue.Low;
                            pin.Write(pinValue);
                        }
                        catch{ }
                        ResetArray[zz] = false;
                        VirtualLEDS[zz].Fill = Grays;
                        VirtualLEDS[zz].StrokeThickness = 0;
                        bitcher.Fill = Grays;
                        TriggerArray[zz] = false;
                    }
                   
                    //led1.Fill = new SolidColorBrush(Windows.UI.Colors.Green);
                    //diff1.Foreground = new SolidColorBrush(Windows.UI.Colors.Green);
                    //led1.Fill
                    for (int v = 0; v < 56; v++)
                    {
                        
                        pers[v].Text = PerData[v].ToString();
                        //UpdateLayout();

                    }


                }
                UpdateLayout();
                bool ballsy = Array.Exists(Triggering, element => element == true);
                if (SweatySack == false && ballsy && Sacked)
                {
                    Sacked = false;
                    TriggerPopUp();
                }
                Sacked = false;
            });
        }
        private void TriggerCode(int zz)
        {
            try
            {
                pinValue = GpioPinValue.High;
                pin.Write(pinValue);
            }
            catch { }
            bitcher.Fill = Greeners;
            tripper = zz;
            VirtualLEDS[zz].Fill = FlakCannon;
            VirtualLEDS[zz].Stroke = BlakCannon;
            VirtualLEDS[zz].StrokeThickness = 2;
            panssy.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 0, 0, 255));
            
            trips++;
            traps.Text = trips.ToString();
            TriggerArray[zz] = true;
            TriggerPopUp();

            // boxxed = false;
        }
        private bool GetMyData(int zz)
        {
            double baby = 0;
            bool zeros = false;
            for (int bigger = 9; bigger > 0; bigger--)
            {
                BigData[bigger, zz] = BigData[bigger - 1, zz];
                baby += BigData[bigger - 1, zz];
                if (BigData[bigger, zz] == 0)
                { zeros = true; }

            }
            BigData[0, zz] = RawData[zz];

            baby += RawData[zz];
            SumData[zz] = baby;
            AvgData[zz] = Math.Round(baby / 10);
            OffData[zz] = RawData[zz] - AvgData[zz];
            PerData[zz] = Math.Abs(Math.Round(OffData[zz] / AvgData[zz] * 100, 1));
            if (RawData[zz] == 0) { zeros = true; }

            return zeros;
        }
        private async void Disconnect(object sender, RoutedEventArgs e)
        {
            try
            {
                await _mqttClient.DisconnectAsync();
            }
            catch (Exception exception)
            {
                //Trace.Text += exception + Environment.NewLine;
                int aa = 0;
            }
        }
        private void Clear(object sender, RoutedEventArgs e)
        {
            //Trace.Text = string.Empty;
        }
        private void Resets(object sender, RoutedEventArgs e)
        {
            if (SweatySack == false)
            {
                SweatySack = true;
                reseter();
            }
        }
        private async void StartServer(object sender, RoutedEventArgs e)
        {
            if (!hasStarted)
            {
                hasStarted = true;
                TimeSpan period = TimeSpan.FromSeconds(1);
                ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        titsy = NetScans();

                        if (titsy == "None")
                        {
                            Rings.IsActive = true;
                            try { if (!connacking) { try { Connack(); } catch (Exception eee) { } } }
                            catch (Exception ee) { }

                        }
                        else
                        {
                            Rings.IsActive = false;
                        }
                    });

                }, period);

                if (_mqttServer != null)
                {
                    return;
                }
                
                startedd.ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/StartedButton2.png"));
                Slips.Fill = startedd;
                JsonServerStorage storage = null;
                storage = new JsonServerStorage();
                storage.Clear();

                _mqttServer = new MqttFactory().CreateMqttServer();

                var options = new MqttServerOptions();
                options.DefaultEndpointOptions.Port = 1883;
                options.Storage = storage;

                await _mqttServer.StartAsync(options);
                var tlsOptions = new MqttClientTlsOptions
                {
                    UseTls = false,
                    IgnoreCertificateChainErrors = true,
                    IgnoreCertificateRevocationErrors = true,
                    AllowUntrustedCertificates = true
                };

                var options2 = new MqttClientOptions { ClientId = "" };


                options2.ChannelOptions = new MqttClientTcpOptions
                {
                    Server = machiner,
                    Port = 1883,
                    TlsOptions = tlsOptions
                };

                if (options2.ChannelOptions == null)
                {
                    throw new InvalidOperationException();
                }

                /*options.Credentials = new MqttClientCredentials
                {
                    Username = User.Text,
                    Password = Password.Text
                };*/

                options2.CleanSession = true;
                options2.KeepAlivePeriod = TimeSpan.FromSeconds(5);

                try
                {
                    if (_mqttClient != null)
                    {
                        await _mqttClient.DisconnectAsync();
                        _mqttClient.ApplicationMessageReceived -= OnApplicationMessageReceived;
                    }

                    var factory = new MqttFactory();
                    _mqttClient = factory.CreateMqttClient();
                    _mqttClient.ApplicationMessageReceived += OnApplicationMessageReceived;

                    await _mqttClient.ConnectAsync(options2);
                }
                catch (Exception exception)
                {
                    //Trace.Text += exception + Environment.NewLine;
                }

                if (_mqttClient == null)
                {
                    return;
                }



                Slips.Fill = startedd;
                var qos = MqttQualityOfServiceLevel.ExactlyOnce;
                await _mqttClient.SubscribeAsync(new TopicFilter("luxProto2", qos));
            }
        }
        private String NetScans()
        {
            ConnectionProfile connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            NetworkConnectivityLevel concon = new NetworkConnectivityLevel();
            try
            {
                concon = connectionProfile.GetNetworkConnectivityLevel();
            }
            catch
            {

            }
            return concon.ToString();
        }
        private async void reseter()
        {
            ContentDialog ResetDialog = new ContentDialog
            {
                Title = "Reset?????",
                Content = "Are you sure you want to reset?",
                PrimaryButtonText = "NO",
                SecondaryButtonText = "YES"

            };
            ContentDialogResult result = await ResetDialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
            {
                for (int hh = 0; hh < 56; hh++)
                {
                    /*try
                    { firstAdapter.Disconnect(); }
                    catch { }
                    ResetArray[hh] = true;*/
                    for (int rr = 0; rr < 10; rr++) { BigData[rr, hh] = 0; }
                }
                if (!Disabled)
                {
                    panssy.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 0, 255, 0));
                    
                }
                SweatySack = false;
                Sacked = true;
            }


            else
            {
                SweatySack = false;
                Sacked = true;
            }
        }
        private void Disables(object sender, RoutedEventArgs e)
        {
            if (!Disabled)
            {
                Disabled = true;
                Stops.Fill = resumer;
               
                panssy.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 0, 0));
                
                bitcher.Fill = Reders;
            }
            else
            {
                Disabled = false;
                
                Stops.Fill = disabler;
                panssy.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 0, 255, 0));
                
                bitcher.Fill = Grays;
            }
            this.UpdateLayout();
        }
        
        private void Offers(object sender, RoutedEventArgs e)
        {
            if (SweatySack == false)
            {
                SweatySack = true;
                Offfer();
            }
        }
        private async void Offfer()
        {
            ContentDialog SelectDialog = new ContentDialog
            {
                Title = "Choose Part",
                Content = "Choose Part #",
                PrimaryButtonText = "NO",
                SecondaryButtonText = "YES",
                MaxWidth = this.ActualWidth,
                Width = this.ActualWidth

            };
            var painal = new StackPanel();

            Button Eighteen = new Button();
            Eighteen.Content = Eighteen.Name = "18";
            Eighteen.Click += Selector;

            Button Eighty = new Button();
            Eighty.Content = Eighty.Name = "80";
            Eighty.Click += Selector;

            Button None = new Button();
            None.Content = "None";
            None.Name = "0";
            None.Click += Selector;

            Eighteen.Margin = Eighty.Margin = None.Margin = new Thickness(6);
            Eighteen.FontSize = Eighty.FontSize = None.FontSize = 16;
            painal.Children.Add(Eighteen);
            painal.Children.Add(Eighty);
            painal.Children.Add(None);
            SelectDialog.Content = painal;

            void Selector(object sender2, RoutedEventArgs e2)
            {
                Button obj = sender2 as Button;
                string _name = obj.Name;
                int sel = Convert.ToInt16(_name);
                Selectop(sel);
                SelectDialog.Title = "Choose Part: " + _name;
            }


            //ResatDialog.UpdateLayout();
            //ResatDialog.Width = 800;
            ContentDialogResult results = await SelectDialog.ShowAsync();
            //ResatDialog.MaxWidth = this.ActualWidth;
            //ResatDialog.Width = this.ActualWidth;
            //ResatDialog.UpdateLayout();
            if (results == ContentDialogResult.Secondary)
            {
                SweatySack = false;
                Sacked = true;
                // resat = true;
                
            }
            else
            {
                Offfer2();
                // The user clicked the CLoseButton, pressed ESC, Gamepad B, or the system back button.
                // Do nothing.
            }

            
        }
        private void Selectop(int select)
        {

            if (select == 18)
            { 
                TurnoffArray[0] = TurnoffArray[1] = TurnoffArray[2] = true;
                TurnoffArray[27] = TurnoffArray[26] = TurnoffArray[25] = TurnoffArray[24]  = true;
            }
            else if (select == 80)
            {
                TurnoffArray[0] = TurnoffArray[1] = TurnoffArray[2] = TurnoffArray[3] = TurnoffArray[4] =  true;
                TurnoffArray[27] = TurnoffArray[26] = TurnoffArray[25] = TurnoffArray[24] = TurnoffArray[23] = TurnoffArray[22] = TurnoffArray[21] = true;
            }
            else
            {
                for(int l =0; l<56; l++)
                {
                    TurnoffArray[l] = false;
                }
            }
        }
        private async void Offfer2()
        {
            ContentDialog ResatDialog = new ContentDialog
            {
                Title = "Reset?????",
                Content = "Are you sure you want to reset?",
                PrimaryButtonText = "NO",
                SecondaryButtonText = "YES",
                MaxWidth = this.ActualWidth


            };
            ResatDialog.Width = this.ActualWidth;
            Button[] hoes = new Button[56];
            CheckBox[] Tricks = new CheckBox[56];
            TextBlock fap = new TextBlock();
            var panal = new StackPanel();
            var panalT = new StackPanel();
            var panalB = new StackPanel();
            var panalB2 = new StackPanel();
            var panalB3 = new StackPanel();

            panalT.Orientation = panalB.Orientation = Orientation.Horizontal;
            panalB2.Orientation = panalB3.Orientation = Orientation.Horizontal;
            for (int yy = 0; yy < 14; yy++)
            {
                hoes[yy] = new Button();
                hoes[yy + 14] = new Button();
                hoes[yy + 28] = new Button();
                hoes[yy + 42] = new Button();

                Tricks[yy] = new CheckBox();
                Tricks[yy+14] = new CheckBox();
                Tricks[yy+28] = new CheckBox();
                Tricks[yy+42] = new CheckBox();

                hoes[yy].Name = yy.ToString() + ",ok";
                hoes[yy + 14].Name = (yy + 14).ToString() + ",ok";
                hoes[yy + 28].Name = (yy + 28).ToString() + ",ok";
                hoes[yy + 42].Name = (yy + 42).ToString() + ",ok";

                Tricks[yy].Name = yy.ToString() + ",ok";
                Tricks[yy + 14].Name = (yy + 14).ToString() + ",ok";
                Tricks[yy + 28].Name = (yy + 28).ToString() + ",ok";
                Tricks[yy + 42].Name = (yy + 42).ToString() + ",ok";

                hoes[yy].Content = (yy + 1).ToString();
                hoes[yy + 14].Content = (yy + 15).ToString();
                hoes[yy + 28].Content = (yy + 29).ToString();
                hoes[yy + 42].Content = (yy + 43).ToString();

                Tricks[yy].Content = (yy + 1).ToString();
                Tricks[yy + 14].Content = (yy + 15).ToString();
                Tricks[yy + 28].Content = (yy + 29).ToString();
                Tricks[yy + 42].Content = (yy + 43).ToString();

                hoes[yy].Width = hoes[yy + 14].Width = 30;
                hoes[yy + 28].Width = hoes[yy + 42].Width = 30;
                hoes[yy].Height = hoes[yy + 14].Height = 30;
                hoes[yy + 28].Height = hoes[yy + 42].Height = 30;

                hoes[yy].Margin = hoes[yy + 14].Margin = new Thickness(2);
                hoes[yy + 28].Margin = hoes[yy + 42].Margin = new Thickness(2);

                hoes[yy].FontSize = hoes[yy + 14].FontSize = 8;
                hoes[yy + 28].FontSize = hoes[yy + 42].FontSize = 8;

                if (!TurnoffArray[yy]) { hoes[yy].Background = Greeners; }
                else { hoes[yy].Background = Reders; }
                if (!TurnoffArray[yy + 14]) { hoes[yy + 14].Background = Greeners; }
                else { hoes[yy + 14].Background = Reders; }
                if (!TurnoffArray[yy + 28]) { hoes[yy + 28].Background = Greeners; }
                else { hoes[yy + 28].Background = Reders; }
                if (!TurnoffArray[yy + 42]) { hoes[yy + 42].Background = Greeners; }
                else { hoes[yy + 42].Background = Reders; }
                hoes[yy].Click += OffOp;
                hoes[yy + 14].Click += OffOp;
                hoes[yy + 28].Click += OffOp;
                hoes[yy + 42].Click += OffOp;
                panalT.Children.Add(hoes[yy]);
                panalB.Children.Add(hoes[yy + 14]);
                panalB2.Children.Add(hoes[yy + 28]);
                panalB3.Children.Add(hoes[yy + 42]);
                //panalT.Children.Add(Tricks[yy]);
                //panalB.Children.Add(Tricks[yy + 14]);
                //panalB2.Children.Add(Tricks[yy + 28]);
                //panalB3.Children.Add(Tricks[yy + 42]);


                //hoes[yy].Click += clicker;
            }
            panal.Children.Add(panalT);
            panal.Children.Add(panalB);
            var gap = new StackPanel();
            panal.Children.Add(gap);
            panal.Children.Add(panalB2);
            panal.Children.Add(panalB3);
            panal.Children.Add(fap);
            ResatDialog.Content = panal;
            ContentDialogResult result = await ResatDialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
            {
                // resat = true;
               
                SweatySack = false;
                Sacked = true;
            }
            else
            {
                SweatySack = false;
                Sacked = true;
                // The user clicked the CLoseButton, pressed ESC, Gamepad B, or the system back button.
                // Do nothing.
            }

            void OffOp(object sender, RoutedEventArgs e)
            {
                Button obj = sender as Button;
                string _name = obj.Name;
                string[] rect = _name.Split(',');
                int posi = Convert.ToInt16(rect[0]);
                if ((SolidColorBrush)hoes[posi].Background == Greeners)
                {
                    hoes[posi].Background = Reders;
                    TurnoffArray[posi] = true;
                }
                else
                {
                    hoes[posi].Background = Greeners;
                    TurnoffArray[posi] = false;
                }
                fap.Text = rect[0];
            }
        }
        private async void TriggerPopUp()
        {

            if (SweatySack == false)
            {
                SweatySack = true;
                

                ContentDialog TriggerDialog = new ContentDialog
                {
                    Title = "Sensor Triggered",
                    Content = "Sensor Triggered",
                    PrimaryButtonText = "Ok"
                };
                StackPanel TriggerPanel = new StackPanel();
                for (int tt = 0; tt < TriggerArray.Length; tt++)
                {
                    if (TriggerArray[tt] & !Triggering[tt])
                    {
                        if (tt < 14) { TriggerList1 = TriggerList1 + (tt + 1).ToString() + ","; }
                        if (tt > 13 && tt < 28) { TriggerList2 = TriggerList2 + (tt + 1).ToString() + ","; }
                        if (tt > 27 && tt < 42) { TriggerList3 = TriggerList3 + (tt + 1).ToString() + ","; }
                        if (tt > 41) { TriggerList4 = TriggerList4 + (tt + 1).ToString() + ","; }
                        Triggering[tt] = true; 
                    }
                }
                TriggerBox1.Text = TriggerList1;
                TriggerBox2.Text = TriggerList2;
                TriggerBox3.Text = TriggerList3;
                TriggerBox4.Text = TriggerList4;
                TriggerPanel.Children.Add(TriggerBox1); TriggerPanel.Children.Add(TriggerBox2);
                TriggerPanel.Children.Add(TriggerBox3); TriggerPanel.Children.Add(TriggerBox4);
                TriggerDialog.Content = TriggerPanel;
                ContentDialogResult result = await TriggerDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    TriggerList1 = TriggerList2 = TriggerList3 = TriggerList4 = "";
                    TriggerPanel = null;
                    TriggerDialog = null;
                    for (int hh = 0; hh < 56; hh++)
                    {
                        ResetArray[hh] = true;
                        for (int rr = 0; rr < 10; rr++) { BigData[rr, hh] = 0; }
                    }
                    if (!Disabled)
                    {
                        panssy.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 0, 255, 0));
                       
                    }
                    TriggerList1 = TriggerList2 = TriggerList3 = TriggerList4 = "";
                    //TriggerDialog = null;
                    //TriggerPanel = null;

                    SweatySack = false;
                    for (int jjj = 0; jjj < 56; jjj++)
                    {
                        Triggering[jjj] = false;
                    }
                }
               
            }
            else
            {
                for (int tt = 0; tt < TriggerArray.Length; tt++)
                {
                    if (TriggerArray[tt] && !Triggering[tt] )
                    {
                        Triggering[tt] = true;
                        if (tt < 14) { TriggerList1 = TriggerList1 + (tt+1).ToString() + ","; }
                        if (tt > 13 && tt < 28) { TriggerList2 = TriggerList2 + (tt + 1).ToString() + ","; }
                        if (tt > 27 && tt < 42) { TriggerList3 = TriggerList3 + (tt + 1).ToString() + ","; }
                        if (tt > 41) { TriggerList4 = TriggerList4 + (tt + 1).ToString() + ","; }
                    }
                }
                TriggerBox1.Text = TriggerList1;
                TriggerBox2.Text = TriggerList2;
                TriggerBox3.Text = TriggerList3;
                TriggerBox4.Text = TriggerList4;
            }
        }
        private void SlipsP(object sender, PointerRoutedEventArgs e)
        {
            if ((!startcheck || !backcheck) && !hasStarted)
            {
                startcheck = true;
                int inner2 = 0;
                TimeSpan delay = TimeSpan.FromMilliseconds(2);

                ThreadPoolTimer PeriodicTimer2 = null;
                StartServer(sender, e);
                ThisMach.Visibility = Visibility.Collapsed;
                PeriodicTimer2 = ThreadPoolTimer.CreatePeriodicTimer(
                (source) =>
                {
                    //
                    // TODO: Work
                    //
                    
                    Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {

                        Slips.StrokeThickness = inner2;
                        inner2 += 2;
                        //ass.Content = "Button " + y.ToString();
                        this.UpdateLayout();



                    });
                    if (inner2 >= 24)
                    {
                        //Slips.StrokeThickness = 60;
                        backcheck = false;
                        PeriodicTimer2.Cancel();

                    }
                }, delay);
                //PeriodicTimer = null;


            }

        }

        private void ResP(object sender, PointerRoutedEventArgs e)
        {
            if (!rescheck || !backres)
            {
                rescheck = true;
                int inner2 = 0;
                Resets(sender, e);
                TimeSpan delay = TimeSpan.FromMilliseconds(2);

                ThreadPoolTimer PeriodicTimer2 = null;
                PeriodicTimer2 = ThreadPoolTimer.CreatePeriodicTimer(
                (source) =>
                {

                    Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                     {

                         Resetss.StrokeThickness = inner2;
                         inner2 += 2;
                        //ass.Content = "Button " + y.ToString();
                        this.UpdateLayout();
                     });
                    if (inner2 >= 24)
                    {
                        //Slips.StrokeThickness = 60;
                        backres = false;
                        PeriodicTimer2.Cancel();

                    }
                }, delay);
                //PeriodicTimer = null;


            }
        }

        private void StopP(object sender, PointerRoutedEventArgs e)
        {
            if (!stopcheck || !stopback)
            {
                stopcheck = true;
                int inner2 = 0;
                Disables(sender, e);
                TimeSpan delay = TimeSpan.FromMilliseconds(2);

                ThreadPoolTimer PeriodicTimer2 = null;
                PeriodicTimer2 = ThreadPoolTimer.CreatePeriodicTimer(
                (source) =>
                {
                    Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {

                        Stops.StrokeThickness = inner2;
                        inner2 += 2;
                        this.UpdateLayout();
                    });
                    if (inner2 >= 24)
                    {
                        //Slips.StrokeThickness = 60;
                        stopback = false;
                        PeriodicTimer2.Cancel();

                    }
                }, delay);
                //PeriodicTimer = null;
            }
        }

        private void StopR(object sender, PointerRoutedEventArgs e)
        {
            if (!stopback)
            {
                stopback = true;
                int inner = 24;
                TimeSpan delay = TimeSpan.FromMilliseconds(2);
                ThreadPoolTimer PeriodicTimer3 = null;
                PeriodicTimer3 = ThreadPoolTimer.CreatePeriodicTimer(
                (source) =>
                {
                    //
                    // TODO: Work
                    //

                    //
                    // Update the UI thread by using the UI core dispatcher.
                    //
                    Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {

                        Stops.StrokeThickness = inner;
                        inner -= 2;
                        //ass.Content = "Button " + y.ToString();
                        this.UpdateLayout();



                    });
                    if (inner <= 0)
                    {
                        //Slips.StrokeThickness = 0;
                        stopcheck = false;
                        PeriodicTimer3.Cancel();

                    }
                }, delay);
                //PeriodicTimer = null;


            }
        }

        private void ResR(object sender, PointerRoutedEventArgs e)
        {
            if (!backres)
            {
                backres = true;
                int inner = 24;
                TimeSpan delay = TimeSpan.FromMilliseconds(2);
                ThreadPoolTimer PeriodicTimer3 = null;
                PeriodicTimer3 = ThreadPoolTimer.CreatePeriodicTimer(
                (source) =>
                {
                    //
                    // TODO: Work
                    //

                    //
                    // Update the UI thread by using the UI core dispatcher.
                    //
                    Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {

                        Resetss.StrokeThickness = inner;
                        inner -= 2;
                        //ass.Content = "Button " + y.ToString();
                        this.UpdateLayout();



                    });
                    if (inner <= 0)
                    {
                        //Slips.StrokeThickness = 0;
                        rescheck = false;
                        PeriodicTimer3.Cancel();

                    }
                }, delay);
                //PeriodicTimer = null;


            }
        }

        private void SlipsR(object sender, PointerRoutedEventArgs e)
        {
            if (!backcheck)
            {
                backcheck = true;
                int inner = 24;
                TimeSpan delay = TimeSpan.FromMilliseconds(2);
                ThreadPoolTimer PeriodicTimer3 = null;
                PeriodicTimer3 = ThreadPoolTimer.CreatePeriodicTimer(
                (source) =>
                {
                    //
                    // TODO: Work
                    //

                    //
                    // Update the UI thread by using the UI core dispatcher.
                    //
                    Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {

                        Slips.StrokeThickness = inner;
                        inner -= 2;
                        //ass.Content = "Button " + y.ToString();
                        this.UpdateLayout();



                    });
                    if (inner <= 0)
                    {
                        //Slips.StrokeThickness = 0;
                        startcheck = false;
                        PeriodicTimer3.Cancel();

                    }
                }, delay);
                //PeriodicTimer = null;


            }
        }

    }
}
