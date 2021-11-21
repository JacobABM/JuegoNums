using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using Newtonsoft.Json;
using System.Threading;
using ServidorNumeros.Views;
using ServidorNumeros.Models;

namespace ServidorNumeros.ViewModels
{
    public class ServidorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        System.Timers.Timer t = new System.Timers.Timer();
        PartidaView partidaView;
        JuegoView juegoView;
        ResultadosView resultadosView;
        static object stopp = true;
        HttpListener server = new();

        public List<string> Jugadores
        {
            get;
            set;
        }

        public bool? RespuestasR
        {
            get;
            set;
        }

        public ObservableCollection<Player> TotalRespuesta { get; set; } = new();

        public byte Unum { get; set; }
        public byte Dnum { get; set; }
        public int NumeroFinal { get; set; }
        public Stopwatch Tiempot { get; set; } = new();

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

        public IEnumerable<Player> Resultados => TotalRespuesta.OrderBy(x => x.Resultado == false).ThenBy(x => x.Tiempo);
        private int segundos = 25;

        public int ConteoSegundos
        {
            get { return segundos; }
            set
            {
                segundos = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ConteoSegundos"));
            }
        }

        public ICommand IniciarCommand { get; set; }
        Dispatcher dispatcher;

        public ServidorViewModel()
        {
            partidaView = new PartidaView()
            { DataContext = this };
            juegoView = new JuegoView()
            { DataContext = this };
            resultadosView = new ResultadosView()
            { DataContext = this };

            IniciarCommand = new RelayCommand(IPartida);
            Control = partidaView;
            RespuestasR = false;
            Jugadores = new();
            dispatcher = Dispatcher.CurrentDispatcher;
            new Thread(Start).Start();
            t.Elapsed += T_Elapsed;
        }

        public void Start()
        {
            if (!server.IsListening)
            {
                server.Prefixes.Add("http://*:10025/");
                server.Start();
                Receive();
            }
        }

        private void IPartida()
        {
            ConteoSegundos = 25;
            Random r = new();
            Control = juegoView;
            Unum = (byte)r.Next(1, 11);
            Dnum = (byte)r.Next(1, 11);
            NumeroFinal = Unum + Dnum;
            RespuestasR = true;
            Tiempot.Reset();
            Tiempot.Start();
            t.Interval = 1000;
            t.Start();
            Refreshs();
        }

        void Receive()
        {
            while (server.IsListening)
            {
                var context = server.GetContext();

                if (context.Request.Url.AbsolutePath == "/Player" && context.Request.HttpMethod == "POST")
                {
                    if (context.Request.QueryString["username"] != null)
                    {
                        if (Jugadores.Contains(context.Request.QueryString["username"]))
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                            var mensaje = JsonConvert.SerializeObject("El nombre que intenta usar ya ha sido utilizado.");
                            byte[] buffer = Encoding.UTF8.GetBytes(mensaje);
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);

                        }
                        else
                        {
                            Jugadores.Add(context.Request.QueryString["username"]);
                            context.Response.StatusCode = 200;
                            var mensaje = JsonConvert.SerializeObject("Nombre de usuario registrado.");
                            byte[] buffer = Encoding.UTF8.GetBytes(mensaje);
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        var mensaje = JsonConvert.SerializeObject("Necesita un nombre de usuario para jugar.");
                        byte[] buffer = Encoding.UTF8.GetBytes(mensaje);
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    }

                    context.Response.Close();

                }
                else if (context.Request.Url.AbsolutePath == "/Intentar" && context.Request.HttpMethod == "POST")
                {
                    if (RespuestasR == true)
                    {
                        var nombre = context.Request.QueryString["username"];
                        var valor = context.Request.QueryString["valor"];

                        if (valor != null)
                        {
                            if (!TotalRespuesta.Any(x => x.Nombre == nombre))
                            {
                                if (Jugadores.Contains(nombre))
                                {

                                    dispatcher.Invoke(() =>
                                    {
                                        lock (stopp)
                                        {
                                            TotalRespuesta.Add(new Player
                                            {
                                                Respuesta = int.Parse(valor),
                                                Nombre = nombre,
                                                Tiempo = Tiempot.Elapsed.Seconds,
                                                Resultado = int.Parse(context.Request.QueryString["valor"]) == NumeroFinal
                                            });
                                        }
                                    });
                                    context.Response.StatusCode = 200;
                                    var mensaje = JsonConvert.SerializeObject("Respuesta enviada.");
                                    byte[] buffer = Encoding.UTF8.GetBytes(mensaje);
                                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);

                                }
                                else
                                {
                                    var mensaje = JsonConvert.SerializeObject("Aún no se ha registrado.");
                                    byte[] buffer = Encoding.UTF8.GetBytes(mensaje);
                                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                                    context.Response.StatusCode = 409;
                                }
                            }
                            else
                            {
                                var mensaje = JsonConvert.SerializeObject("Respuesta enviada, No puede volver a responder.");
                                byte[] buffer = Encoding.UTF8.GetBytes(mensaje);
                                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                                context.Response.StatusCode = 409;
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            var mensaje = JsonConvert.SerializeObject("Escriba su respuesta.");
                            byte[] buffer = Encoding.UTF8.GetBytes(mensaje);
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        }

                    }
                    else
                    {
                        context.Response.StatusCode = 409;
                        var mensaje = JsonConvert.SerializeObject("No ha sido detectado.");
                        byte[] buffer = Encoding.UTF8.GetBytes(mensaje);
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    }

                    context.Response.Close();
                }
                else
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
        }

        private void T_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ConteoSegundos -= 1;
            Refreshs("ConteoSegundos");
            if (ConteoSegundos <= 0)
            {
                dispatcher.Invoke(() =>
                {
                    RespuestasR = false;
                    t.Stop();
                    Refreshs(nameof(RespuestasR));
                    Refreshs(nameof(Resultados));
                    TotalRespuesta = new();
                    Control = resultadosView;
                });
            }
        }


        void Refreshs(string propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }
    }
}
