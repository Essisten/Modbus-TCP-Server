using System;
using System.Collections.Generic;
using System.Linq;
using Modbus_TCP_Server;
using System.Windows;
using System.Text.RegularExpressions;
using System.Net;
using EasyModbus;

namespace ModbusTCP_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool started;
        private ModbusServer server;
        private List<Device> devices;
        int counter = 0;    //только для дебага
        public MainWindow()
        {
            InitializeComponent();
            started = false;
            devices = new List<Device>();
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (started)
            {
                //Остановка сервера
                LaunchButton.Content = "Запустить";
                server.StopListening();
                server = null;
            }
            else
            {
                //Запуск сервера
                //Введённые данные можно проверять на валидность как try/catch, так и при помощи regex
                Regex IP_regex = new Regex("^(?:\\d{1,3}\\.){3}\\d{1,3}$");
                if (!IP_regex.IsMatch(IP_textbox.Text))
                {
                    MessageBox.Show("Введён неправильный формат IP", "Неверный IP", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!int.TryParse(Port_textbox.Text, out int port) || port < 0 || port > 65535)
                {
                    MessageBox.Show("Порт должен быть в диапозоне от 0 до 65535", "Неверный порт", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                IPAddress ip = IPAddress.Parse(IP_textbox.Text);
                if (server == null)
                    server = new ModbusServer();
                server.LocalIPAddress = ip;
                server.Port = port;
                server.CoilsChanged += Server_CoilsChanged;
                server.HoldingRegistersChanged += Server_HoldingRegistersChanged;
                server.NumberOfConnectedClientsChanged += Server_NumberOfConnectedClientsChanged;
                
                server.Listen();
                Sheet.ItemsSource = devices;
                LaunchButton.Content = "Остановить";
            }
            started = !started;
            UpdateSheet();
        }
        private void UpdateSheet()
        {
            devices.Clear();
            //Console.WriteLine(counter++);
            if (started)
            {
                for (int i = 0; i < server.coils.localArray.Length; i++)
                {
                    if (server.coils.localArray[i] || server.discreteInputs.localArray[i] ||
                        server.holdingRegisters.localArray[i] != 0 || server.inputRegisters.localArray[i] != 0)
                    {
                        devices.Add(new Device(i, server.coils.localArray[i], server.discreteInputs.localArray[i],
                                    server.holdingRegisters.localArray[i], server.inputRegisters.localArray[i]));
                    }
                }
            }
            Dispatcher.Invoke(() =>
            {
                Sheet.Items.Refresh();
                if (!started)
                    Connections.Content = $"Подключений: 0";
                else
                    Connections.Content = $"Подключений: {server.NumberOfConnections}";
            });
        }

        private void Server_CoilsChanged(int coil, int numberOfCoils)
        {
            UpdateSheet();
        }

        private void Server_HoldingRegistersChanged(int register, int numberOfRegisters)
        {
            UpdateSheet();
        }

        private void Server_NumberOfConnectedClientsChanged()
        {
            UpdateSheet();
            //Тут должно быть ограничение на количество подключений, но я не знаю как это сделать
        }

        private void Cell_combobox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void Sheet_SelectedCellsChanged(object sender, System.Windows.Controls.SelectedCellsChangedEventArgs e)
        {

        }
    }
}