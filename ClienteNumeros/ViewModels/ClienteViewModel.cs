using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClienteNumeros.Views;
using System.Windows.Controls;
using System.ComponentModel;

namespace ClienteNumeros.ViewModels
{

    public class ClienteViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        RespuestaView respv;
        NicknameView nicknamev;
        public string Nombre { get; set; } = null;
        public int Respuesta { get; set; }

        HttpClient cliente = new HttpClient();
        public ICommand RegistroCommand { get; set; }
        public ICommand RespuestaCommand { get; set; }
        private Control control;
        public Control Control
        {
            get { return control; }
            set
            {
                control = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Control"));
            }
        }
        private string mensaje;
        public string Mensaje
        {
            get { return mensaje; }
            set
            {
                mensaje = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Mensaje"));
            }
        }
        public ClienteViewModel()
        {
            respv = new();
            respv.DataContext = this;
            nicknamev = new() { DataContext = this };
            Control = nicknamev;
            RegistroCommand = new RelayCommand(SendUsername);
            RespuestaCommand = new RelayCommand(SendResponse);
        }

        private async Task EnviarRespuesta()
        {
            if (Respuesta != 0)
            {
                HttpRequestMessage request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"http://localhost:10025/Intentar?username={Nombre}&valor={Respuesta}"),
                };
                var response = await cliente.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Mensaje = JsonConvert.DeserializeObject<string>(json);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Mensaje"));
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Mensaje = JsonConvert.DeserializeObject<string>(json);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Mensaje"));
                }
            }
            else
            {
                Mensaje = "Escribir Resultado";
            }

            await Task.Delay(TimeSpan.FromSeconds(3));
        }
        private async void SendResponse()
        {
            await EnviarRespuesta();
            Mensaje = "";
        }
        private async void SendUsername()
        {
            await Registrarse();
            Mensaje = "";
        }
        public async Task Registrarse()
        {
            if (Nombre!= null)
            {
                HttpRequestMessage request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"http://localhost:10025/Player?username={Nombre}"),
                };
                var response = await cliente.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Control = respv;
                    var json = await response.Content.ReadAsStringAsync();
                    Mensaje = JsonConvert.DeserializeObject<string>(json);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Mensaje"));
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Mensaje = JsonConvert.DeserializeObject<string>(json);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Mensaje"));
                }

            }
            else
            {
                Mensaje = "Este campo no debe estar vacío";
            }
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}
