using SharpDX.XInput;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using JoystickServerApp.HID;
using System.Security.Policy;
using System.Net.Sockets;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using HidSharp.Reports;
using SharpDX;
using System.IdentityModel.Metadata;
//using System.Collections.Generic;
//using System.Text;
//using System.Runtime.Remoting.Messaging;

namespace JoystickServerApp
{
    public partial class Form1 : Form
    {
        public static Controller controller;

        public static Session session;
        public static Color leftRGBval;
        public static Color rightRGBval;
        private readonly string url = "opc.tcp://192.168.0.216:4840";
        public static Gamepad GamepadState;
        public static Gamepad previousState;

        //|var|Scout-Econo x06 TV WV Multicore.Application.GVL
        //|var|CODESYS Control for Raspberry Pi MC SL.Application.GVL
        readonly NodeId leftVibration_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.LeftVibration");
        readonly NodeId rightVibration_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.RightVibration");
        readonly NodeId rightLedR_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.RightLedR");
        readonly NodeId rightLedG_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.RightLedG");
        readonly NodeId rightLedB_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.RightLedB");
        readonly NodeId leftLedR_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.LeftLedR");
        readonly NodeId leftLedG_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.LeftLedG");
        readonly NodeId leftLedB_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.LeftLedB");
        readonly NodeId leftStickX_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.LeftStickX");
        readonly NodeId leftStickY_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.LeftStickY");
        readonly NodeId rightStickX_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.RightStickX");
        readonly NodeId rightStickY_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.RightStickY");
        readonly NodeId LeftTrigger_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.LeftTrigger");
        readonly NodeId RightTrigger_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.RightTrigger");
        readonly NodeId buttonA_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonA");
        readonly NodeId buttonB_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonB");
        readonly NodeId buttonX_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonX");
        readonly NodeId buttonY_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonY");
        readonly NodeId buttonLB_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonLB");
        readonly NodeId buttonRB_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonRB");
        readonly NodeId buttonStart_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonStart");
        readonly NodeId buttonView_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonView");
        readonly NodeId buttonDPadUp_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonUp");
        readonly NodeId buttonDPadDown_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonDown");
        readonly NodeId buttonDPadLeft_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonLeft");
        readonly NodeId buttonDPadRight_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.ButtonRight");
        readonly NodeId timer100ms_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.TimeActual");
        readonly NodeId timerPing_ID = new NodeId("ns=4;s=|var|Scout-Econo x06 TV WV Multicore.Application.GVL.PingTime");
        public string ping = "00";
        private System.Threading.Timer OpcOkuTimer;
        private System.Threading.Timer JoystickOkuTimer;
        private System.Threading.Timer OpcYazTimer;
        private bool isReadingOpcData = false;
        private bool isJoystickReading = false;
        private bool isWritingOpcData = false;



        public Form1()
        {
            InitializeComponent();
            InitializeOpcClient();
            InitializeControllerAsync();
            InitializeAsync();
            // Zamanlayıcıları ve olay işleyicilerini başlat
            while (true)
            {
                if (controller?.IsConnected == true)
                {
                    break;
                }
                else
                {
                    MessageBox.Show("Joystick Bağlantısı Kesildi. Program kapatılıyor.");
                    Application.Exit();
                    return;
                }
            }
            

            // Formu göster
            this.Show();
        }

        private void InitializeControllerAsync()
        {
            

            try
            {
                controller = new Controller(UserIndex.One);

                while (!controller.IsConnected)
                {
                    if (controller.IsConnected) break;
                }
               
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Joystick Güncelleme Hatası: {ex.Message}");
            }

            if (controller.IsConnected)
            {
                UpdateJoystickStatus(true);
            }
            else
            {
                UpdateJoystickStatus(false);
            }

        }
        public async void InitializeOpcClient()
        {

            try
            {
                // OPC UA client configuration
                var config = new ApplicationConfiguration
                {
                    ApplicationName = "OPCUAExample",
                    ApplicationType = ApplicationType.Client,
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier
                        {
                            StoreType = "Directory",
                            StorePath = "Certificates",
                            SubjectName = "CN=OPCUAExample"
                        },
                        AutoAcceptUntrustedCertificates = true,
                        RejectSHA1SignedCertificates = false // Gerekirse SHA-1 sertifikalarını kabul edin

                    },
                    ClientConfiguration = new ClientConfiguration
                    {
                        DefaultSessionTimeout = 60000,

                    },

                };

                // Sertifika oluştur ve doğrula


                await config.Validate(ApplicationType.Client);

                // Endpoint ayarları
                var endpointUrl = "opc.tcp://192.168.0.216:4840"; // Sunucu URL'sini kendi endpoint'iniz ile değiştirin
                var endpointDescription = CoreClientUtils.SelectEndpoint(endpointUrl, useSecurity: true, 10000);

                // Sunucuya bağlanmak için Session yarat
                session = await Session.Create(config, new ConfiguredEndpoint(null, endpointDescription), false, "", 60000, null, null);


            }
            catch (Exception ex)
            {
                MessageBox.Show($"OPC UA Bağlantı hatası: {ex.Message}");
            }
            finally
            {
                if (session.Connected) UpdateOpcStatus(true);
                else UpdateOpcStatus(false);
            }


        }


        private void InitializeAsync()
        {
            //Zamanlayıcı ayarları

            OpcOkuTimer = new System.Threading.Timer(async _ => await OpcReadTimer_TickAsync(), null, 510, 20);

            JoystickOkuTimer = new System.Threading.Timer(async _ => await JoystickOkuTimer_TickAsync(), null, 500, 20);
            //JoystickOkuTimer.Tick += async (s, e) => await JoystickOkuTimer_TickAsync();
            //JoystickOkuTimer.Start();

            OpcYazTimer = new System.Threading.Timer(async _ => await OpcYazTimer_TickAsync(), null, 500, 20);
            //OpcYazTimer.Tick += async (s, e) => await OpcYazTimer_TickAsync();
            //OpcYazTimer.Start();
        }


        private async Task OpcReadTimer_TickAsync()
        {
            //if (isReadingOpcData) return;  // Zaten çalışıyorsa işlem yapma
            isReadingOpcData = true;

            try
            {
                await ReadOpcData();  // OPC verilerini arka planda okuma
            }
            catch (Exception ex)
            {
                // UI thread'de çalıştırmak için MessageBox gösterimi
                MessageBox.Show($"OPC veri okuma hatası: {ex.Message}");
            }
            finally
            {
                isReadingOpcData = false;
            }
        }

        private async Task JoystickOkuTimer_TickAsync()
        {
            //if (isJoystickReading) return;  // Zaten çalışıyorsa işlem yapma
            isJoystickReading = true;

            try
            {
                await Task.WhenAll(ReadJoystickData(), UpdateUIWithJoystickData());  // Joystick verilerini ve UI güncellemesini paralel olarak çalıştır
            }
            catch (Exception ex)
            {
                // UI thread'de çalıştırmak için Task.Run kullanarak MessageBox gösterimi
                await Task.Run(() => MessageBox.Show($"Joystick veri okuma hatası: {ex.Message}"));
            }
            finally
            {
                isJoystickReading = false;
            }
        }
        private async Task OpcYazTimer_TickAsync()
        {
            //if (isWritingOpcData) return;  // Zaten çalışıyorsa işlem yapma
            isWritingOpcData = true;

            try
            {
                await WriteOpcData();  // OPC verilerini arka planda yazma
            }
            catch (Exception ex)
            {
                // UI thread'de çalıştırmak için Task.Run kullanarak MessageBox gösterimi
                await Task.Run(() => MessageBox.Show($"OPC veri yazma hatası: {ex.Message}"));
            }
            finally
            {
                isWritingOpcData = false;
            }
        }


        private async Task WriteOpcData()
        {
            try
            {
                var writeCollection = new WriteValueCollection
                {
                    CreateWriteValue(leftStickX_ID, GamepadState.LeftThumbX),
                    CreateWriteValue(leftStickY_ID, GamepadState.LeftThumbY),
                    CreateWriteValue(LeftTrigger_ID, GamepadState.LeftTrigger),
                    CreateWriteValue(rightStickX_ID, GamepadState.RightThumbX),
                    CreateWriteValue(rightStickY_ID, GamepadState.RightThumbY),
                    CreateWriteValue(RightTrigger_ID, GamepadState.RightTrigger),
                    CreateWriteValue(buttonA_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.A)),
                    CreateWriteValue(buttonB_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.B)),
                    CreateWriteValue(buttonX_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.X)),
                    CreateWriteValue(buttonY_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.Y)),
                    CreateWriteValue(buttonStart_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.Start)),
                    CreateWriteValue(buttonView_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.Back)),
                    CreateWriteValue(buttonLB_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder)),
                    CreateWriteValue(buttonRB_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.RightShoulder)),
                    CreateWriteValue(buttonDPadUp_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.DPadUp)),
                    CreateWriteValue(buttonDPadDown_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.DPadDown)),
                    CreateWriteValue(buttonDPadLeft_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.DPadLeft)),
                    CreateWriteValue(buttonDPadRight_ID, GamepadState.Buttons.HasFlag(GamepadButtonFlags.DPadRight)),
                    CreateWriteValue(timer100ms_ID, $"T#{DateTime.Now:HH}h{DateTime.Now:mm}m{DateTime.Now:ss}s{DateTime.Now:fff}ms")
                };

                var result = await Task.Run(() => session.Write(null, writeCollection, out StatusCodeCollection statusCodes, out DiagnosticInfoCollection diagnosticInfos));

                if (!StatusCode.IsGood(result.ServiceResult))
                {
                    //  richTextBoxLogs.AppendText("Değer yazıldı: " + statusCodes[0]);
                }
            }
            catch (Exception ex)
            {
                await Task.Run(() => MessageBox.Show($"OPC UA veri yazma hatası: {ex.Message}"));
            }
        }
        private WriteValue CreateWriteValue(NodeId nodeId, object value)
        {
            return new WriteValue
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value,
                Value = new Opc.Ua.DataValue(new Variant(value))
            };
        }

        private async Task ReadOpcData()
        {
            try
            {
                var nodeIds = new[]
                {
                    leftLedR_ID, leftLedG_ID, leftLedB_ID,
                    rightLedR_ID, rightLedG_ID, rightLedB_ID,
                    leftVibration_ID, rightVibration_ID
                };

                var (values, _) = await session.ReadValuesAsync(nodeIds);

                var leftLedR = values[0].ToString();
                var leftLedG = values[1].ToString();
                var leftLedB = values[2].ToString();
                var rightLedR = values[3].ToString();
                var rightLedG = values[4].ToString();
                var rightLedB = values[5].ToString();
                var leftVib = values[6].ToString();
                var rightVib = values[7].ToString();

                IOUpdate(leftLedR, leftLedG, leftLedB, rightLedR, rightLedG, rightLedB, leftVib, rightVib);

                //leftRGBval = Color.FromArgb(int.Parse(leftLedR), int.Parse(leftLedG), int.Parse(leftLedB));
                //rightRGBval = Color.FromArgb(int.Parse(rightLedR), int.Parse(rightLedG), int.Parse(rightLedB));

                //if (controller.IsConnected)
                //{
                //    // Sol Ledler
                //    var messages = new[]
                //    {
                //        AuraMessage(0x01, leftRGBval, Color.White),
                //        AuraMessage(0x02, leftRGBval, Color.White),
                //        AuraMessage(0x03, rightRGBval, Color.White),
                //        AuraMessage(0x04, rightRGBval, Color.White)
                //    };

                //    foreach (var message in messages)
                //    {
                //        HID.AsusHid.WriteAura(message);
                //    }

                //    controller.SetVibration(new Vibration
                //    {
                //        LeftMotorSpeed = ushort.Parse(leftVib),
                //        RightMotorSpeed = ushort.Parse(rightVib)
                //    });
                //    //richTextBoxLogs.AppendText("sol :" + ushort.Parse(leftVib) + "\n");
                //    //richTextBoxLogs.AppendText("sağ :" + ushort.Parse(rightVib) + "\n");
                //}
            }
            catch (Exception ex)
            {
                await Task.Run(() => MessageBox.Show($"OPC UA veri okuma hatası: {ex.Message}"));
            }
            // return session.ReadValue(timerPing_ID).ToString();
        }

        private void IOUpdate(string leftLedR, string leftLedG, string leftLedB, string rightLedR, string rightLedG, string rightLedB, string leftVib, string rightVib)
        {
            if (!controller.IsConnected) return;

            leftRGBval = Color.FromArgb(int.Parse(leftLedR), int.Parse(leftLedG), int.Parse(leftLedB));
            rightRGBval = Color.FromArgb(int.Parse(rightLedR), int.Parse(rightLedG), int.Parse(rightLedB));

            // Sol Ledler
            var messages = new[]
            {
                AuraMessage(0x01, leftRGBval, Color.White),
                AuraMessage(0x02, leftRGBval, Color.White),
                AuraMessage(0x03, rightRGBval, Color.White),
                AuraMessage(0x04, rightRGBval, Color.White)
            };

            foreach (var message in messages)
            {
                HID.AsusHid.WriteAura(message);
            }

            controller.SetVibration(new Vibration
            {
                LeftMotorSpeed = ushort.Parse(leftVib),
                RightMotorSpeed = ushort.Parse(rightVib)
            });
        }

        private async Task ReadJoystickData()
        {
            if (controller == null || !controller.IsConnected)
            {
                UpdateJoystickStatus(false);
                return;
            }

            try
            {
                var currentState = controller.GetState().Gamepad;
                if (!currentState.Equals(previousState))
                {
                    GamepadState = currentState;
                    previousState = currentState; // Önceki durumu güncelle
                }
            }
            catch (Exception ex)
            {
                await Task.Run(() => MessageBox.Show($"Joystick veri okuma hatası: {ex.Message}"));
                UpdateJoystickStatus(false);
            }
        }

        private async Task UpdateUIWithJoystickData()
        {
            if (!controller.IsConnected) return;

            try
            {
                await Task.Run(UpdateWPF);


            }
            catch (Exception ex)
            {
                await Task.Run(() => MessageBox.Show($"WPF güncelleme hatası: {ex.Message}"));
            }
        }



        private void UpdateWPF()
        {

            textBoxButonlar.Text = GamepadState.Buttons.ToString();
            textBoxSolX.Text = GamepadState.LeftThumbX.ToString();
            textBoxSolY.Text = GamepadState.LeftThumbY.ToString();
            textBoxSağX.Text = GamepadState.RightThumbX.ToString();
            textBoxSağY.Text = GamepadState.RightThumbY.ToString();
            //progressBarSolTetik.Value = GamepadState.LeftTrigger;
            trackBarSolTetik.Value = GamepadState.LeftTrigger;
            //progressBarSagTetik.Value = GamepadState.RightTrigger;
            trackBarSagTetik.Value = GamepadState.RightTrigger;
            //richTextBoxLogs.AppendText("Ping: ");


            UpdateButtonColor(buttonA, GamepadState.Buttons.HasFlag(GamepadButtonFlags.A));
            UpdateButtonColor(buttonB, GamepadState.Buttons.HasFlag(GamepadButtonFlags.B));
            UpdateButtonColor(buttonX, GamepadState.Buttons.HasFlag(GamepadButtonFlags.X));
            UpdateButtonColor(buttonY, GamepadState.Buttons.HasFlag(GamepadButtonFlags.Y));
            UpdateButtonColor(buttonStart, GamepadState.Buttons.HasFlag(GamepadButtonFlags.Start));
            UpdateButtonColor(buttonView, GamepadState.Buttons.HasFlag(GamepadButtonFlags.Back));
            UpdateButtonColor(buttonLB, GamepadState.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder));
            UpdateButtonColor(buttonRB, GamepadState.Buttons.HasFlag(GamepadButtonFlags.RightShoulder));
            UpdateButtonColor(buttonUp, GamepadState.Buttons.HasFlag(GamepadButtonFlags.DPadUp));
            UpdateButtonColor(buttonDown, GamepadState.Buttons.HasFlag(GamepadButtonFlags.DPadDown));
            UpdateButtonColor(buttonLeft, GamepadState.Buttons.HasFlag(GamepadButtonFlags.DPadLeft));
            UpdateButtonColor(buttonRight, GamepadState.Buttons.HasFlag(GamepadButtonFlags.DPadRight));
            leftRGB.BackColor = leftRGBval;
            rightRGB.BackColor = rightRGBval;



        }
        public byte[] AuraMessage(byte zone, Color color, Color color2)
        {

            byte[] msg = new byte[17];
            msg[0] = AsusHid.AURA_ID;
            msg[1] = 0xB3;
            msg[2] = zone; // Zone 
            msg[3] = (byte)0; // Aura Mode
            msg[4] = color.R; // R
            msg[5] = color.G; // G
            msg[6] = color.B; // B
            msg[7] = (byte)1; // aura.speed as u8;
            msg[8] = 0x00; // aura.direction as u8;
            msg[9] = 0x00;
            msg[10] = color2.R; // R
            msg[11] = color2.G; // G
            msg[12] = color2.B; // B
            return msg;
        }




        public void UpdateJoystickStatus(bool isConnected)
        {
            //if (isConnected) MessageBox.Show("Joystick Bağlandı");
            //else MessageBox.Show("Joystick Bağlantısı Kesildi");
            labelJoystickStatus.Text = isConnected ? "Joystick Bağlandı" : "Joystick Bağlantısı yok";
            labelJoystickStatus.ForeColor = isConnected ? Color.Green : Color.Red;

        }
        public void UpdateOpcStatus(bool isConnected)
        {
            labelConnectionStatus.Text = isConnected ? "Bağlandı" : "Bağlantı yok";
            labelConnectionStatus.ForeColor = isConnected ? Color.Green : Color.Red;
        }
        public void UpdateButtonColor(Button button, bool isPressed)
        {
            button.BackColor = isPressed ? Color.Green : Color.Gray;
        }
    }
}
